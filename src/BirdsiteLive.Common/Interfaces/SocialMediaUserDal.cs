using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BirdsiteLive.Common.Interfaces;

public interface SocialMediaUserDal
{ 
        Task<SyncUser> GetUserAsync(string acct);
        Task<SyncUser> GetUserAsync(int acct);
        Task DeleteUserAsync(int id);
        Task DeleteUserAsync(string acct);
        Task CreateUserAsync(string acct);
        Task AddFollower(int follower, int followed);
        Task RemoveFollower(int follower, int followed);
        Task<long> GetFollowersCountAsync(int id);
}

public interface SyncUser
{
        public int Id { get; set; }
        public long TwitterUserId { get; set; }
        public string Acct { get; set; }
        public string FediAcct { get; set; }

        public long LastTweetPostedId { get; set; }

        public DateTime LastSync { get; set; }

        public int FetchingErrorCount { get; set; } //TODO: update DAL
        public long Followers { get; set; } 
        public long StatusesCount { get; set; }
        public JsonElement ExtraData { get; set; }
        public string PreDescriptionHook { get; }
        public string PostDescriptionHook { get; }
        public string DisclaimerReplacementHook { get; }
        public (string, string)[] AdditionnalAttachments { get; }
}