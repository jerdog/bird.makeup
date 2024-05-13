using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.Common.Exceptions;
using BirdsiteLive.Instagram.Models;
using dotMakeup.Instagram.Models;
using Microsoft.Extensions.Caching.Memory;

namespace dotMakeup.Instagram;

public class InstagramService : ISocialMediaService
{
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly InstanceSettings _settings;
        private readonly ISettingsDal _settingsDal;
        
        private readonly MemoryCache _userCache;
        private readonly MemoryCache _postCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(1)//Size amount
            //Priority on removing when reaching size limit (memory pressure)
            .SetPriority(CacheItemPriority.Low)
            // Keep in cache for this time, reset time if accessed.
            .SetSlidingExpiration(TimeSpan.FromHours(16))
            // Remove from cache after this time, regardless of sliding expiration
            .SetAbsoluteExpiration(TimeSpan.FromHours(24));

        private readonly MemoryCacheEntryOptions _cacheEntryOptionsError = new MemoryCacheEntryOptions()
            .SetSize(1)//Size amount
            //Priority on removing when reaching size limit (memory pressure)
            .SetPriority(CacheItemPriority.Low)
            // Keep in cache for this time, reset time if accessed.
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            // Remove from cache after this time, regardless of sliding expiration
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

        #region Ctor
        public InstagramService(IInstagramUserDal userDal, IHttpClientFactory httpClientFactory, InstanceSettings settings, ISettingsDal settingsDal)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _settingsDal = settingsDal;
            UserDal = userDal;
            
            _userCache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = settings.UserCacheCapacity
            });
            _postCache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = settings.TweetCacheCapacity
            });
        }
        #endregion


        public string ServiceName { get; } = "Instagram";
        public Regex ValidUsername { get;  } = new Regex(@"^[a-zA-Z0-9_\.]{1,30}$");
        public Regex UserMention { get;  } = new Regex(@"(^|.?[ \n\.]+)@([a-zA-Z0-9_\.]+)(?=\s|$|[\[\]<>,;:'\.’!?/—\|-]|(. ))");
        public SocialMediaUserDal UserDal { get; }
        public async Task<SocialMediaUser?> GetUserAsync(string username)
        {
            var accounts = await _settingsDal.Get("ig_allow_list");
            if (accounts is null)
                throw new UserNotFoundException();
            if (!accounts.Value.EnumerateArray().Any(user => user.GetString() == username))
                throw new UserNotFoundException();
            
            if (!_userCache.TryGetValue(username, out InstagramUser user))
            {
                var client = _httpClientFactory.CreateClient();
                string requestUrl;
                requestUrl = _settings.SidecarURL + "/instagram/user/" + username;
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var httpResponse = await client.SendAsync(request);

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    _userCache.Set(username, user, _cacheEntryOptionsError);
                    throw new UserNotFoundException();
                }

                var c = await httpResponse.Content.ReadAsStringAsync();
                var userDocument = JsonDocument.Parse(c);


                try
                {
                    user = new InstagramUser()
                    {
                        Description = userDocument.RootElement.GetProperty("bio").GetString(),
                        Acct = username,
                        ProfileImageUrl = userDocument.RootElement.GetProperty("profilePic").GetString(),
                        Name = userDocument.RootElement.GetProperty("name").GetString(),
                        PinnedPosts = new List<long>(),
                        ProfileUrl = "www.instagram.com/" + username,
                    };

                }
                catch (KeyNotFoundException _)
                {
                    _userCache.Set(username, user, _cacheEntryOptionsError);
                    throw new UserNotFoundException();
                }
                _userCache.Set(username, user, _cacheEntryOptions);
            }

            return user;
        }

        public async Task<SocialMediaPost?> GetPostAsync(string id)
        {
            if (!_postCache.TryGetValue(id, out InstagramPost post))
            {
                var client = _httpClientFactory.CreateClient();
                string requestUrl;
                requestUrl = _settings.SidecarURL + "/instagram/post/" + id;
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var httpResponse = await client.SendAsync(request);

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    _postCache.Set(id, post, _cacheEntryOptionsError);
                    return null;
                }
                var c = await httpResponse.Content.ReadAsStringAsync();
                var postDoc = JsonDocument.Parse(c);
                List<ExtractedMedia> media = new List<ExtractedMedia>();
                foreach (JsonElement m in postDoc.RootElement.GetProperty("media").EnumerateArray())
                {
                    bool isVideo = m.GetProperty("is_video").GetBoolean();
                    if (!isVideo)
                    {
                        media.Add(new ExtractedMedia()
                        {
                            Url = m.GetProperty("url").GetString(),
                            MediaType = "image/jpeg"

                        });
                        
                    }

                }
                post = new InstagramPost()
                    {
                        Id = id,
                        MessageContent = postDoc.RootElement.GetProperty("caption").GetString(),
                        Author = new InstagramUser()
                        {
                            Acct = postDoc.RootElement.GetProperty("user").GetString(),
                        },
                        
                        Media = media.ToArray(),
                    };
                _postCache.Set(id, post, _cacheEntryOptions);
                return post;
            }

            
            return post;
        }
        
}