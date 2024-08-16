using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Models;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Moderation.Actions;
using BirdsiteLive.Pipeline.Contracts;
using BirdsiteLive.Pipeline.Models;
using BirdsiteLive.Pipeline.Processors.SubTasks;
using BirdsiteLive.Pipeline.Processors;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Pipeline.Tests.Processors
{
    [TestClass]
    public class SendTweetsToFollowersProcessorTests
    {
        [TestMethod]
        public async Task ProcessAsync_SameInstance_SharedInbox_OneTweet_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host = "domain.ext";
            var sharedInbox = "/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new []
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new []
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host,
                        SharedInboxRoute = sharedInbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host,
                        SharedInboxRoute = sharedInbox
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);
            sendTweetsToSharedInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                    It.Is<string>(y => y == host),
                    It.Is<Follower[]>(y => y.Length == 2)))
                .Returns(Task.CompletedTask);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);
            
            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new[] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_SharedInbox_OneTweet_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var sharedInbox = "/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        SharedInboxRoute = sharedInbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        SharedInboxRoute = sharedInbox
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);
            foreach (var host in new [] { host1, host2})
            {
                sendTweetsToSharedInboxTaskMock
                    .Setup(x => x.ExecuteAsync(
                        It.Is<ExtractedTweet[]>(y => y.Length == 1),
                        It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                        It.Is<string>(y => y == host),
                        It.Is<Follower[]>(y => y.Length == 1)))
                    .Returns(Task.CompletedTask);
            }

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);
            
            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new[] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_SharedInbox_OneTweet_Error_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var sharedInbox = "/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        SharedInboxRoute = sharedInbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        SharedInboxRoute = sharedInbox
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);
            sendTweetsToSharedInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                    It.Is<string>(y => y == host1),
                    It.Is<Follower[]>(y => y.Length == 1)))
                .Returns(Task.CompletedTask);

            sendTweetsToSharedInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                    It.Is<string>(y => y == host2),
                    It.Is<Follower[]>(y => y.Length == 1)))
                .Throws(new Exception());

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);
            
            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId2),
                        It.Is<int>(y => y == 1)
                ))
                .Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new[] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_SharedInbox_OneTweet_ErrorReset_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var sharedInbox = "/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        SharedInboxRoute = sharedInbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        SharedInboxRoute = sharedInbox,
                        PostingErrorCount = 50
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);
            sendTweetsToSharedInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                    It.Is<string>(y => y == host1),
                    It.Is<Follower[]>(y => y.Length == 1)))
                .Returns(Task.CompletedTask);

            sendTweetsToSharedInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                    It.Is<string>(y => y == host2),
                    It.Is<Follower[]>(y => y.Length == 1)))
                .Returns(Task.CompletedTask);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId2),
                        It.Is<int>(y => y == 0)
                ))
                .Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_SharedInbox_OneTweet_ErrorAndReset_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var sharedInbox = "/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        SharedInboxRoute = sharedInbox,
                        PostingErrorCount = 50
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        SharedInboxRoute = sharedInbox,
                        PostingErrorCount = 50
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);
            sendTweetsToSharedInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                    It.Is<string>(y => y == host1),
                    It.Is<Follower[]>(y => y.Length == 1)))
                .Returns(Task.CompletedTask);

            sendTweetsToSharedInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct),
                    It.Is<string>(y => y == host2),
                    It.Is<Follower[]>(y => y.Length == 1)))
                .Throws(new Exception());

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId1),
                        It.Is<int>(y => y == 0)
                ))
                .Returns(Task.CompletedTask);

            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId2),
                        It.Is<int>(y => y == 51)
                ))
                .Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_SameInstance_Inbox_OneTweet_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host = "domain.ext";
            var inbox = "/user/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host,
                        InboxRoute = inbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host,
                        InboxRoute = inbox
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);
            foreach (var userId in new[] { userId1, userId2 })
            {
                sendTweetsToInboxTaskMock
                    .Setup(x => x.ExecuteAsync(
                        It.Is<ExtractedTweet[]>(y => y.Length == 1),
                        It.Is<Follower>(y => y.Id == userId),
                        It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                    .Returns(Task.CompletedTask);
            }

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_Inbox_OneTweet_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var inbox = "/user/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        InboxRoute = inbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        InboxRoute = inbox
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);
            foreach (var userId in new[] { userId1, userId2 })
            {
                sendTweetsToInboxTaskMock
                    .Setup(x => x.ExecuteAsync(
                        It.Is<ExtractedTweet[]>(y => y.Length == 1),
                        It.Is<Follower>(y => y.Id == userId),
                        It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                    .Returns(Task.CompletedTask);
            }

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_Inbox_OneTweet_Error_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var inbox = "/user/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        InboxRoute = inbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        InboxRoute = inbox
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);
            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Returns(Task.CompletedTask);

            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId2),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Throws(new Exception());

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);
            
            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId2),
                        It.Is<int>(y => y == 1)
                ))
                .Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_Inbox_OneTweet_Error_SettingsThreshold_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var inbox = "/user/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        InboxRoute = inbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        InboxRoute = inbox,
                        PostingErrorCount = 42
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);
            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Returns(Task.CompletedTask);

            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId2),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Throws(new Exception());

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                FailingFollowerCleanUpThreshold = 10,
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            removeFollowerMock
                .Setup(x => x.ProcessAsync(It.Is<Follower>(y => y.Id == userId2)))
                .Returns(Task.CompletedTask);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_Inbox_OneTweet_Error_MaxThreshold_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var inbox = "/user/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        InboxRoute = inbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        InboxRoute = inbox,
                        PostingErrorCount = 2147483600
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);
            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Returns(Task.CompletedTask);

            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId2),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Throws(new Exception());

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                FailingFollowerCleanUpThreshold = 0,
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            removeFollowerMock
                .Setup(x => x.ProcessAsync(It.Is<Follower>(y => y.Id == userId2)))
                .Returns(Task.CompletedTask);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_Inbox_OneTweet_ErrorReset_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var inbox = "/user/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        InboxRoute = inbox
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        InboxRoute = inbox,
                        PostingErrorCount = 50
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);
            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Returns(Task.CompletedTask);

            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId2),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Returns(Task.CompletedTask);

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId2),
                        It.Is<int>(y => y == 0)
                ))
                .Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };

            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] {userWithTweets}, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_MultiInstances_Inbox_OneTweet_ErrorAndReset_Test()
        {
            #region Stubs
            var tweetId = 1;
            var host1 = "domain1.ext";
            var host2 = "domain2.ext";
            var inbox = "/user/inbox";
            var userId1 = 2;
            var userId2 = 3;
            var userAcct = "user";

            var userWithTweets = new UserWithDataToSync()
            {
                Tweets = new[]
                {
                    new ExtractedTweet
                    {
                        Id = tweetId.ToString()
                    }
                },
                User = new SyncTwitterUser
                {
                    Acct = userAcct,
                    Id = 10
                },
                Followers = new[]
                {
                    new Follower
                    {
                        Id = userId1,
                        Host = host1,
                        InboxRoute = inbox,
                        PostingErrorCount = 50
                    },
                    new Follower
                    {
                        Id = userId2,
                        Host = host2,
                        InboxRoute = inbox,
                        PostingErrorCount = 50
                    },
                }
            };
            #endregion

            #region Mocks
            var sendTweetsToInboxTaskMock = new Mock<ISendTweetsToInboxTask>(MockBehavior.Strict);
            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId1),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Returns(Task.CompletedTask);

            sendTweetsToInboxTaskMock
                .Setup(x => x.ExecuteAsync(
                    It.Is<ExtractedTweet[]>(y => y.Length == 1),
                    It.Is<Follower>(y => y.Id == userId2),
                    It.Is<SyncTwitterUser>(y => y.Acct == userAcct)))
                .Throws(new Exception());

            var sendTweetsToSharedInboxTaskMock = new Mock<ISendTweetsToSharedInboxTask>(MockBehavior.Strict);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            var socialServiceMock = new Mock<ISocialMediaService>();
            socialServiceMock
                .Setup(x => x.UserDal.GetFollowersAsync(
                    It.Is<int>(y => y == 10)
                    ))
                .ReturnsAsync(userWithTweets.Followers);

            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId1),
                        It.Is<int>(y => y == 0)
                ))
                .Returns(Task.CompletedTask);

            followersDalMock
                .Setup(x => x.UpdateFollowerErrorCountAsync(
                        It.Is<int>(y => y == userId2),
                        It.Is<int>(y => y == 51)
                ))
                .Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<SendTweetsToFollowersProcessor>>();

            var settings = new InstanceSettings
            {
                ParallelFediversePosts = 1
            };
            
            var removeFollowerMock = new Mock<IRemoveFollowerAction>(MockBehavior.Strict);
            #endregion

            var processor = new SendTweetsToFollowersProcessor(sendTweetsToInboxTaskMock.Object, sendTweetsToSharedInboxTaskMock.Object, followersDalMock.Object, socialServiceMock.Object, loggerMock.Object, settings, removeFollowerMock.Object);
            await processor.ProcessAsync(new [] { userWithTweets }, CancellationToken.None);

            #region Validations
            sendTweetsToInboxTaskMock.VerifyAll();
            sendTweetsToSharedInboxTaskMock.VerifyAll();
            followersDalMock.VerifyAll();
            removeFollowerMock.VerifyAll();
            #endregion
        }
    }
}