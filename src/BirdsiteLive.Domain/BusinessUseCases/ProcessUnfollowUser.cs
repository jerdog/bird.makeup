using System.Linq;
using System.Threading.Tasks;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.DAL.Contracts;

namespace BirdsiteLive.Domain.BusinessUseCases
{
    public interface IProcessUndoFollowUser
    {
        Task ExecuteAsync(string followerUsername, string followerDomain, string twitterUsername);
    }

    public class ProcessUndoFollowUser : IProcessUndoFollowUser
    {
        private readonly IFollowersDal _followerDal;
        private readonly ISocialMediaService _socialMediaService;

        #region Ctor
        public ProcessUndoFollowUser(IFollowersDal followerDal, ISocialMediaService socialMediaService)
        {
            _followerDal = followerDal;
            _socialMediaService = socialMediaService;
        }
        #endregion

        public async Task ExecuteAsync(string followerUsername, string followerDomain, string twitterUsername)
        {
            // Get Follower and Twitter Users
            var follower = await _followerDal.GetFollowerAsync(followerUsername, followerDomain);
            if (follower == null) return;

            var twitterUser = await _socialMediaService.UserDal.GetUserAsync(twitterUsername);
            if (twitterUser == null) return;

            // Update Follower
            var twitterUserId = twitterUser.Id;

            await _socialMediaService.UserDal.RemoveFollower(follower.Id, twitterUserId);
            
            // remove account if it follows no one
            var follower2 = await _followerDal.GetFollowerAsync(followerUsername, followerDomain);
            if (follower2.TotalFollowings == 0)
                await _followerDal.DeleteFollowerAsync(followerUsername, followerDomain);
            
            // Check if TwitterUser has still followers
            var followers = await _socialMediaService.UserDal.GetFollowersCountAsync(twitterUser.Id);
            if (followers == 0)
                await _socialMediaService.UserDal.DeleteUserAsync(twitterUsername);
        }
    }
}