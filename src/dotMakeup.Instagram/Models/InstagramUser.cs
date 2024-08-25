using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Instagram.Models;

namespace dotMakeup.Instagram.Models;

public class InstagramUser : SocialMediaUser
{
    
        public long Id { get; set; }
        public string Name { get; set; }
        public bool Protected { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> PinnedPosts { get; set; }
        public IEnumerable<InstagramPost> RecentPosts { get; set; }
        public string Url { get; set; }
        public string Acct { get; set; }
        public string Location { get; set; }
        public string ProfileUrl { get; set; }
        public string ProfileImageUrl { get; set; }
        public string ProfileBannerURL { get; set; }
}