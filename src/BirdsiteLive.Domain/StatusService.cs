using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BirdsiteLive.ActivityPub;
using BirdsiteLive.ActivityPub.Converters;
using BirdsiteLive.ActivityPub.Models;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Domain.Repository;
using BirdsiteLive.Domain.Statistics;
using BirdsiteLive.Domain.Tools;
using BirdsiteLive.Twitter.Models;

namespace BirdsiteLive.Domain
{
    public interface IStatusService
    {
        Note GetStatus(string username, ExtractedTweet tweet);
        Note GetStatus(string username, SocialMediaPost post);
        ActivityCreateNote GetActivity(string username, ExtractedTweet tweet);
    }

    public class StatusService : IStatusService
    {
        private readonly InstanceSettings _instanceSettings;
        private readonly IStatusExtractor _statusExtractor;
        private readonly IExtractionStatisticsHandler _statisticsHandler;

        #region Ctor
        public StatusService(InstanceSettings instanceSettings, IStatusExtractor statusExtractor, IExtractionStatisticsHandler statisticsHandler)
        {
            _instanceSettings = instanceSettings;
            _statusExtractor = statusExtractor;
            _statisticsHandler = statisticsHandler;
        }
        #endregion

        public Note GetStatus(string username, SocialMediaPost post)
        {
            var actorUrl = UrlFactory.GetActorUrl(_instanceSettings.Domain, username);
            var noteUrl = UrlFactory.GetNoteUrl(_instanceSettings.Domain, username, post.Id.ToString());
            String announceId = null;
            if (post.IsRetweet)
            {
                actorUrl = UrlFactory.GetActorUrl(_instanceSettings.Domain, post.OriginalAuthor.Acct);
                noteUrl = UrlFactory.GetNoteUrl(_instanceSettings.Domain, post.OriginalAuthor.Acct, post.RetweetId.ToString());
                announceId  = UrlFactory.GetNoteUrl(_instanceSettings.Domain, username, post.Id.ToString());
            }

            var to = $"{actorUrl}/followers";

            var cc = new string[0];
            
            string summary = null;

            var extractedTags = _statusExtractor.Extract(post.MessageContent);
            _statisticsHandler.ExtractedStatus(extractedTags.tags.Count(x => x.type == "Mention"));

            // Replace RT by a link
            var content = extractedTags.content;
            if (post.IsRetweet)
            {
                // content = "RT: " + content;
                cc = new[] {"https://www.w3.org/ns/activitystreams#Public"};
            }
            cc = new[] {"https://www.w3.org/ns/activitystreams#Public"};

            string inReplyTo = null;
            if (post.InReplyToStatusId != default)
                inReplyTo = $"https://{_instanceSettings.Domain}/users/{post.InReplyToAccount.ToLowerInvariant()}/statuses/{post.InReplyToStatusId}";

            var note = new Note
            {
                id = noteUrl,
                announceId = announceId,

                published = post.CreatedAt.ToString("s") + "Z",
                url = noteUrl,
                attributedTo = actorUrl,

                inReplyTo = inReplyTo,

                to = new[] { to },
                cc = cc,

                sensitive = false,
                summary = summary,
                content = $"<p>{content}</p>",
                attachment = Convert(post.Media),
                tag = extractedTags.tags
            };

            return note;
        }

        public Note GetStatus(string username, ExtractedTweet tweet)
        {
            return GetStatus(username, (SocialMediaPost)tweet);
        }

        public ActivityCreateNote GetActivity(string username, ExtractedTweet tweet)
        {
            var note = GetStatus(username, tweet);
            var actor = UrlFactory.GetActorUrl(_instanceSettings.Domain, username);
            String noteUri;
            string activityType;
            if (tweet.IsRetweet) 
            {
                noteUri = UrlFactory.GetNoteUrl(_instanceSettings.Domain, username, tweet.Id.ToString());
                activityType = "Announce";
            } else
            {
                noteUri = UrlFactory.GetNoteUrl(_instanceSettings.Domain, username, tweet.Id.ToString());
                activityType = "Create";
            }

            var now = DateTime.UtcNow;
            var nowString = now.ToString("s") + "Z";

            var noteActivity = new ActivityCreateNote()
            {
                context = "https://www.w3.org/ns/activitystreams",
                id = $"{noteUri}/activity",
                type = activityType,
                actor = actor,
                published = nowString,

                to = new[] {$"{actor}/followers"},
                cc = note.cc,
                apObject = note
            };

            return noteActivity;
        }

        private Attachment[] Convert(ExtractedMedia[] media)
        {
            if(media == null) return new Attachment[0];
            return media.Select(x =>
            {
                return new Attachment
                {
                    type = "Document",
                    url = x.Url,
                    mediaType = x.MediaType,
                    name = x.AltText
                };
            }).ToArray();
        }
    }
}
