using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BirdsiteLive.Twitter
{

    public class TwitterService : ISocialMediaService
    {
        private readonly ITwitterTweetsService _twitterTweetsService;
        private readonly ITwitterUserService _twitterUserService;

        #region Ctor
        public TwitterService(ICachedTwitterTweetsService twitterService, ICachedTwitterUserService twitterUserService, ITwitterUserDal userDal, InstanceSettings settings)
        {
            _twitterTweetsService = twitterService;
            _twitterUserService = twitterUserService;
            UserDal = userDal;
        }
        #endregion

        public async Task<SocialMediaPost> GetPostAsync(long id)
        {
            var post = await _twitterTweetsService.GetTweetAsync(id);
            return post;
        }

        public string ServiceName { get; } = "Twitter";
        
        // https://help.twitter.com/en/managing-your-account/twitter-username-rules
        public Regex ValidUsername { get;  } = new Regex(@"^[a-zA-Z0-9_]{1,15}$");
        public Regex UserMention { get;  } = new Regex(@"(^|.?[ \n\.]+)@([a-zA-Z0-9_]+)(?=\s|$|[\[\]<>,;:'\.’!?/—\|-]|(. ))");
        public SocialMediaUserDal UserDal { get; }
        public async Task<SocialMediaUser> GetUserAsync(string user)
        {
            var res = await _twitterUserService.GetUserAsync(user);
            return res;
        }

    }
}