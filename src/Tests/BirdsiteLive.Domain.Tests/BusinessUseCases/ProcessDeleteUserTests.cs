﻿using System.Collections.Generic;
using System.Threading.Tasks;
using BirdsiteLive.Common.Models;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Domain.BusinessUseCases;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Domain.Tests.BusinessUseCases
{
    [TestClass]
    public class ProcessDeleteUserTests
    {
        [TestMethod]
        public async Task ExecuteAsync_NoMoreFollowings()
        {
            #region Stubs
            var follower = new Follower
            {
                Id = 12,
                Followings = new List<int> { 1 }
            };
            #endregion

            #region Mocks
            var followersDalMock = new Mock<IFollowersDal>();

            followersDalMock
                .Setup(x => x.DeleteFollowerAsync(
                    It.Is<int>(y => y == 12)))
                .Returns(Task.CompletedTask);

            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            twitterUserDalMock
                .Setup(x => x.DeleteUserAsync(
                    It.Is<int>(y => y == 1)))
                .Returns(Task.CompletedTask);
            #endregion

            var action = new ProcessDeleteUser(followersDalMock.Object, twitterUserDalMock.Object);
            await action.ExecuteAsync(follower);

            #region Validations
            #endregion
        }

        [TestMethod]
        public async Task ExecuteAsync_HaveFollowings()
        {
            #region Stubs
            var follower = new Follower
            {
                Id = 12,
                Followings = new List<int> { 1 }
            };

            var followers = new List<Follower>
            {
                follower,
                new Follower
                {
                    Id = 11
                }
            };
            #endregion

            #region Mocks
            var followersDalMock = new Mock<IFollowersDal>();

            followersDalMock
                .Setup(x => x.DeleteFollowerAsync(
                    It.Is<int>(y => y == 12)))
                .Returns(Task.CompletedTask);

            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            #endregion

            var action = new ProcessDeleteUser(followersDalMock.Object, twitterUserDalMock.Object);
            await action.ExecuteAsync(follower);

            #region Validations
            twitterUserDalMock.VerifyAll();
            #endregion
        }
    }
}