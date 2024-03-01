﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BirdsiteLive.ActivityPub;
using BirdsiteLive.ActivityPub.Converters;
using BirdsiteLive.ActivityPub.Models;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Regexes;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Cryptography;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.Domain.BusinessUseCases;
using BirdsiteLive.Domain.Repository;
using BirdsiteLive.Domain.Statistics;
using BirdsiteLive.Domain.Tools;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Models;

namespace BirdsiteLive.Domain
{
    public interface IUserService
    {
        Task<Actor> GetUser(SocialMediaUser twitterUser);
        Task<bool> FollowRequestedAsync(string signature, string method, string path, string queryString, Dictionary<string, string> requestHeaders, ActivityFollow activity, string body);
        Task<bool> UndoFollowRequestedAsync(string signature, string method, string path, string queryString, Dictionary<string, string> requestHeaders, ActivityUndoFollow activity, string body);

        Task<bool> SendRejectFollowAsync(ActivityFollow activity, string followerHost);
        Task<bool> DeleteRequestedAsync(string signature, string method, string path, string queryString, Dictionary<string, string> requestHeaders, ActivityDelete activity, string body);
    }

    public class UserService : IUserService
    {
        private readonly IProcessDeleteUser _processDeleteUser;
        private readonly IProcessFollowUser _processFollowUser;
        private readonly IProcessUndoFollowUser _processUndoFollowUser;

        private readonly InstanceSettings _instanceSettings;
        private readonly ICryptoService _cryptoService;
        private readonly IActivityPubService _activityPubService;
        private readonly IStatusExtractor _statusExtractor;
        private readonly IExtractionStatisticsHandler _statisticsHandler;

        private readonly ISocialMediaService _socialMediaService;

        private readonly IModerationRepository _moderationRepository;

        #region Ctor
        public UserService(InstanceSettings instanceSettings, ICryptoService cryptoService, IActivityPubService activityPubService, IProcessFollowUser processFollowUser, IProcessUndoFollowUser processUndoFollowUser, IStatusExtractor statusExtractor, IExtractionStatisticsHandler statisticsHandler, ITwitterUserService twitterUserService, IModerationRepository moderationRepository, IProcessDeleteUser processDeleteUser, ITwitterUserDal twitterUserDal, ISocialMediaService socialMediaService)
        {
            _instanceSettings = instanceSettings;
            _cryptoService = cryptoService;
            _activityPubService = activityPubService;
            _processFollowUser = processFollowUser;
            _processUndoFollowUser = processUndoFollowUser;
            _statusExtractor = statusExtractor;
            _statisticsHandler = statisticsHandler;
            _moderationRepository = moderationRepository;
            _processDeleteUser = processDeleteUser;
            _socialMediaService = socialMediaService;
        }
        #endregion

        public async Task<Actor> GetUser(SocialMediaUser twitterUser)
        {
            var actorUrl = UrlFactory.GetActorUrl(_instanceSettings.Domain, twitterUser.Acct);
            var acct = twitterUser.Acct.ToLowerInvariant();

            // Extract links, mentions, etc
            var description = twitterUser.Description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var extracted = _statusExtractor.Extract(description, _instanceSettings.ResolveMentionsInProfiles);
                description = extracted.content;

                _statisticsHandler.ExtractedDescription(extracted.tags.Count(x => x.type == "Mention"));
            }
            
            string featured = null;
            if (twitterUser.PinnedPosts.Count() > 0)
            {
                featured = $"https://{_instanceSettings.Domain}/users/{twitterUser.Acct}/collections/featured";
            }

            List<UserAttachment> attachment = new List<UserAttachment>()
            {
                new UserAttachment
                {
                    type = "PropertyValue",
                    name = "Official",
                    value =
                        $"<a href=\"https://twitter.com/{acct}\" rel=\"me nofollow noopener noreferrer\" target=\"_blank\"><span class=\"invisible\">https://</span><span class=\"ellipsis\">twitter.com/{acct}</span></a>"
                },
                new UserAttachment
                {
                    type = "PropertyValue",
                    name = "Support this service",
                    value =
                        $"<a href=\"https://www.patreon.com/birddotmakeup\" rel=\"me nofollow noopener noreferrer\" target=\"_blank\"><span class=\"invisible\">https://</span><span class=\"ellipsis\">www.patreon.com/birddotmakeup</span></a>"
                }
            };

            if (twitterUser.Location is not null)
            {
                var locationAttachment = new UserAttachment()
                {
                    type = "PropertyValue",
                    name = "Location",
                    value = twitterUser.Location,
                };
                attachment.Insert(0, locationAttachment);
            }

            var userDal = await _socialMediaService.UserDal.GetUserAsync(twitterUser.Acct);
            if (userDal is not null)
            {
                description = userDal.PreDescriptionHook + description + userDal.PostDescriptionHook;
                foreach ((string name, string value) in userDal.AdditionnalAttachments)
                {
                    
                    var locationAttachment = new UserAttachment()
                    {
                        type = "PropertyValue",
                        name = name,
                        value = value,
                    };
                    attachment = attachment.Append( locationAttachment).ToList();
                }
            }

            var user = new Actor
            {
                id = actorUrl,
                type = "Service", 
                followers = $"{actorUrl}/followers",
                preferredUsername = acct,
                name = twitterUser.Name,
                inbox = $"{actorUrl}/inbox",
                summary = description + $"<br>This account is a replica from {_socialMediaService.ServiceName}. Its author can't see your replies. If you find this service useful, please consider supporting us via our Patreon. <br>",
                url = actorUrl,
                featured = featured,
                manuallyApprovesFollowers = twitterUser.Protected,
                publicKey = new PublicKey()
                {
                    id = $"{actorUrl}#main-key",
                    owner = actorUrl,
                    publicKeyPem =  await _cryptoService.GetUserPem(acct)
                },
                icon = new Image
                {
                    mediaType = "image/jpeg",
                    url = twitterUser.ProfileImageUrl
                },
                image = new Image
                {
                    mediaType = "image/jpeg",
                    url = twitterUser.ProfileBannerURL
                },
                attachment = attachment.ToArray(),
                endpoints = new EndPoints
                {
                    sharedInbox = $"https://{_instanceSettings.Domain}/inbox"
                }
            };
            return user;
        }

        public async Task<bool> FollowRequestedAsync(string signature, string method, string path, string queryString, Dictionary<string, string> requestHeaders, ActivityFollow activity, string body)
        {

            // Validate
            var sigValidation = await ValidateSignature(activity.apObject, activity.actor, signature, method, path, queryString, requestHeaders, body);
            if (!sigValidation.SignatureIsValidated) return false;

            // Prepare data
            var followerUserName = SigValidationResultExtractor.GetUserName(sigValidation);
            var followerHost = SigValidationResultExtractor.GetHost(sigValidation);
            var followerInbox = sigValidation.User.inbox;
            var followerSharedInbox = SigValidationResultExtractor.GetSharedInbox(sigValidation);
            var twitterUser = activity.apObject.Split('/').Last().Replace("@", string.Empty).ToLowerInvariant().Trim();

            // Make sure to only keep routes
            followerInbox = OnlyKeepRoute(followerInbox, followerHost);
            followerSharedInbox = OnlyKeepRoute(followerSharedInbox, followerHost);
            
            // Validate Moderation status
            var followerModPolicy = _moderationRepository.GetModerationType(ModerationEntityTypeEnum.Follower);
            if (followerModPolicy != ModerationTypeEnum.None)
            {
                var followerStatus = _moderationRepository.CheckStatus(ModerationEntityTypeEnum.Follower, $"@{followerUserName}@{followerHost}");
                
                if(followerModPolicy == ModerationTypeEnum.WhiteListing && followerStatus != ModeratedTypeEnum.WhiteListed || 
                   followerModPolicy == ModerationTypeEnum.BlackListing && followerStatus == ModeratedTypeEnum.BlackListed)
                    return await SendRejectFollowAsync(activity, followerHost);
            }

            // Validate TwitterAccount status
            var twitterAccountModPolicy = _moderationRepository.GetModerationType(ModerationEntityTypeEnum.TwitterAccount);
            if (twitterAccountModPolicy != ModerationTypeEnum.None)
            {
                var twitterUserStatus = _moderationRepository.CheckStatus(ModerationEntityTypeEnum.TwitterAccount, twitterUser);
                if (twitterAccountModPolicy == ModerationTypeEnum.WhiteListing && twitterUserStatus != ModeratedTypeEnum.WhiteListed ||
                    twitterAccountModPolicy == ModerationTypeEnum.BlackListing && twitterUserStatus == ModeratedTypeEnum.BlackListed)
                    return await SendRejectFollowAsync(activity, followerHost);
            }

            // Validate User 
            var userDal = await _socialMediaService.UserDal.GetUserAsync(twitterUser);
            if (userDal is null)
            {
                // this will fail if the username doesn't exist
                var user = await _socialMediaService.GetUserAsync(twitterUser);
                if (user.Protected)
                {

                    return await SendRejectFollowAsync(activity, followerHost);
                }
            }
            // Execute
            await _processFollowUser.ExecuteAsync(followerUserName, followerHost, twitterUser, followerInbox,
                followerSharedInbox, activity.actor);
            return await SendAcceptFollowAsync(activity, followerHost, followerInbox);
        }
        
        private async Task<bool> SendAcceptFollowAsync(ActivityFollow activity, string followerHost, string followerInbox)
        {
            var acceptFollow = _activityPubService.BuildAcceptFollow(activity);
            var result = await _activityPubService.PostDataAsync(acceptFollow, followerHost, activity.apObject, followerInbox);
            return result == HttpStatusCode.Accepted ||
                   result == HttpStatusCode.OK; //TODO: revamp this for better error handling

        }

        public async Task<bool> SendRejectFollowAsync(ActivityFollow activity, string followerHost)
        {
            var acceptFollow = new ActivityRejectFollow()
            {
                context = "https://www.w3.org/ns/activitystreams",
                id = $"{activity.apObject}#rejects/follows/{Guid.NewGuid()}",
                type = "Reject",
                actor = activity.apObject,
                apObject = new ActivityFollow()
                {
                    id = activity.id,
                    type = activity.type,
                    actor = activity.actor,
                    apObject = activity.apObject
                }
            };
            var result = await _activityPubService.PostDataAsync(acceptFollow, followerHost, activity.apObject);
            return result == HttpStatusCode.Accepted ||
                   result == HttpStatusCode.OK; //TODO: revamp this for better error handling
        }
        
        private string OnlyKeepRoute(string inbox, string host)
        {
            if (string.IsNullOrWhiteSpace(inbox)) 
                return null;

            if (inbox.Contains(host))
                inbox = inbox.Split(new[] { host }, StringSplitOptions.RemoveEmptyEntries).Last();

            return inbox;
        }

        public async Task<bool> UndoFollowRequestedAsync(string signature, string method, string path, string queryString,
            Dictionary<string, string> requestHeaders, ActivityUndoFollow activity, string body)
        {
            // Validate
            var sigValidation = await ValidateSignature(activity.apObject.apObject, activity.actor, signature, method, path, queryString, requestHeaders, body);
            if (!sigValidation.SignatureIsValidated) return false;

            // Save Follow in DB
            var followerUserName = sigValidation.User.preferredUsername.ToLowerInvariant();
            var followerHost = sigValidation.User.url.Replace("https://", string.Empty).Split('/').First();
            //var followerInbox = sigValidation.User.inbox;
            var twitterUser = activity.apObject.apObject.Split('/').Last().Replace("@", string.Empty);
            await _processUndoFollowUser.ExecuteAsync(followerUserName, followerHost, twitterUser);

            // Send Accept Activity
            var acceptFollow = new ActivityAcceptUndoFollow()
            {
                context = "https://www.w3.org/ns/activitystreams",
                id = $"{activity.apObject.apObject}#accepts/undofollows/{Guid.NewGuid()}",
                type = "Accept",
                actor = activity.apObject.apObject,
                apObject = new ActivityUndoFollow()
                {
                    id = (activity.apObject as dynamic).id?.ToString(),
                    type = (activity.apObject as dynamic).type?.ToString(),
                    actor = (activity.apObject as dynamic).actor?.ToString(),
                    context = (activity.apObject as dynamic).context?.ToString(),
                    apObject = (activity.apObject as dynamic).@object?.ToString()
                }
            };
            var result = await _activityPubService.PostDataAsync(acceptFollow, followerHost, activity.apObject.apObject);
            return result == HttpStatusCode.Accepted || result == HttpStatusCode.OK; //TODO: revamp this for better error handling
        }

        public async Task<bool> DeleteRequestedAsync(string signature, string method, string path, string queryString, Dictionary<string, string> requestHeaders,
            ActivityDelete activity, string body)
        {
            // Validate
            var sigValidation = await ValidateSignature(null, activity.actor, signature, method, path, queryString, requestHeaders, body);
            if (!sigValidation.SignatureIsValidated) return false;

            // Remove user and followings
            var followerUserName = SigValidationResultExtractor.GetUserName(sigValidation);
            var followerHost = SigValidationResultExtractor.GetHost(sigValidation);

            await _processDeleteUser.ExecuteAsync(followerUserName, followerHost);

            return true;
        }

        private async Task<SignatureValidationResult> ValidateSignature(string localActor, string actor, string rawSig, string method, string path, string queryString, Dictionary<string, string> requestHeaders, string body)
        {
            var remoteUser2 = await _activityPubService.GetUser(localActor, actor);
            return new SignatureValidationResult()
            {
                SignatureIsValidated = true,
                User = remoteUser2
            };

            //Check Date Validity
            var date = requestHeaders["date"];
            var d = DateTime.Parse(date).ToUniversalTime();
            var now = DateTime.UtcNow;
            var delta = Math.Abs((d - now).TotalSeconds);
            if (delta > 30) return new SignatureValidationResult { SignatureIsValidated = false };
            
            //Check Digest
            var digest = requestHeaders["digest"];
            var digestHash = digest.Split(new [] {"SHA-256="},StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            var calculatedDigestHash = _cryptoService.ComputeSha256Hash(body);
            if (digestHash != calculatedDigestHash) return new SignatureValidationResult { SignatureIsValidated = false };

            //Check Signature
            var signatures = rawSig.Split(',');
            var signature_header = new Dictionary<string, string>();
            foreach (var signature in signatures)
            {
                var m = HeaderRegexes.HeaderSignature.Match(signature);
                signature_header.Add(m.Groups[1].ToString(), m.Groups[2].ToString());
            }

            var key_id = signature_header["keyId"];
            var headers = signature_header["headers"];
            var algorithm = signature_header["algorithm"];
            var sig = Convert.FromBase64String(signature_header["signature"]);

            // Retrieve User
            var remoteUser = await _activityPubService.GetUser(null, actor);

	                Console.WriteLine(remoteUser.publicKey.publicKeyPem);

            // Prepare Key data
            var toDecode = remoteUser.publicKey.publicKeyPem.Trim().Remove(0, remoteUser.publicKey.publicKeyPem.IndexOf('\n'));
            toDecode = toDecode.Remove(toDecode.LastIndexOf('\n')).Replace("\n", "");
            var signKey = ASN1.ToRSA(Convert.FromBase64String(toDecode));

            var toSign = new StringBuilder();
            foreach (var headerKey in headers.Split(' '))
            {
                if (headerKey == "(request-target)") toSign.Append($"(request-target): {method.ToLower()} {path}{queryString}\n");
                else toSign.Append($"{headerKey}: {string.Join(", ", requestHeaders[headerKey])}\n");
            }
            toSign.Remove(toSign.Length - 1, 1);

	    Console.WriteLine(Convert.FromBase64String(toDecode));
            // Import key
            var key = new RSACryptoServiceProvider();
            var rsaKeyInfo = key.ExportParameters(false);
            rsaKeyInfo.Modulus = Convert.FromBase64String(toDecode);
            key.ImportParameters(rsaKeyInfo);

            // Trust and Verify
            var result = signKey.VerifyData(Encoding.UTF8.GetBytes(toSign.ToString()), sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return new SignatureValidationResult()
            {
                SignatureIsValidated = result,
                User = remoteUser
            };
        }
    }

    public class SignatureValidationResult 
    {
        public bool SignatureIsValidated { get; set; }
        public Actor User { get; set; }
    }
}
