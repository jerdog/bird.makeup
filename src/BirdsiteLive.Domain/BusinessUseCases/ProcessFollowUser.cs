using System.Threading.Tasks;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.DAL.Contracts;

namespace BirdsiteLive.Domain.BusinessUseCases
{
    public interface IProcessFollowUser
    {
        Task ExecuteAsync(string followerUsername, string followerDomain, string twitterUsername, string followerInbox, string sharedInbox, string followerActorId);
    }

    public class ProcessFollowUser : IProcessFollowUser
    {
        private readonly IFollowersDal _followerDal;
        private readonly ISocialMediaService _socialMediaService;

        #region Ctor
        public ProcessFollowUser(IFollowersDal followerDal, ISocialMediaService socialMediaService)
        {
            _followerDal = followerDal;
            _socialMediaService = socialMediaService;
        }
        #endregion

        public async Task ExecuteAsync(string followerUsername, string followerDomain, string twitterUsername, string followerInbox, string sharedInbox, string followerActorId)
        {
            // Get Follower and Twitter Users
            var follower = await _followerDal.GetFollowerAsync(followerUsername, followerDomain);
            if (follower == null)
            {
                await _followerDal.CreateFollowerAsync(followerUsername, followerDomain, followerInbox, sharedInbox, followerActorId);
                follower = await _followerDal.GetFollowerAsync(followerUsername, followerDomain);
            }

            var twitterUser = await _socialMediaService.UserDal.GetUserAsync(twitterUsername);
            if (twitterUser == null)
            {
                await _socialMediaService.UserDal.CreateUserAsync(twitterUsername);
                twitterUser = await _socialMediaService.UserDal.GetUserAsync(twitterUsername);
            }

            // Update Follower
            var twitterUserId = twitterUser.Id;
            
            // Save Follower
            await _socialMediaService.UserDal.AddFollower(follower.Id, twitterUserId);
        }
    }
}