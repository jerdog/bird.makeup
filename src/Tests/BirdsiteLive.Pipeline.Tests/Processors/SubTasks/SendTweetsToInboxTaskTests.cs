﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BirdsiteLive.ActivityPub;
using BirdsiteLive.ActivityPub.Models;
using BirdsiteLive.Common.Models;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Domain;
using BirdsiteLive.Pipeline.Processors.SubTasks;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Pipeline.Tests.Processors.SubTasks
{
    [TestClass]
    public class SendTweetsToInboxTaskTests
    {
        [TestMethod]
        public async Task ExecuteAsync_SingleTweet_Test()
        {
            #region Stubs
            var tweetId = 10;
            var tweets = new List<ExtractedTweet>
            {
                new ExtractedTweet
                {
                    Id = tweetId.ToString(),
                }
            };

            var noteId = "noteId";
            var note = new Note()
            {
                id = noteId
            };
            var activity = new ActivityCreateNote() {
                apObject = note
            };

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
            };

            var settings = new InstanceSettings
            {
                PublishReplies = false
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>();
            activityPubService
                .Setup(x => x.PostNewActivity(
                    It.Is<ActivityCreateNote>(y => y.apObject.id == noteId),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .Returns(Task.CompletedTask);

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            statusServiceMock
                .Setup(x => x.GetActivity(
                It.Is<string>(y => y == twitterHandle),
                It.Is<ExtractedTweet>(y => y.Id == tweetId.ToString())))
                .Returns(activity);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);

            var loggerMock = new Mock<ILogger<SendTweetsToInboxTask>>();
            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object, settings, loggerMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ExecuteAsync_SingleTweet_Reply_Test()
        {
            #region Stubs
            var tweetId = 10;
            var tweets = new List<ExtractedTweet>
            {
                new ExtractedTweet
                {
                    Id = tweetId.ToString(),
                    IsReply = true,
                    IsThread = false
                }
            };

            var noteId = "noteId";
            var note = new Note()
            {
                id = noteId
            };
            var activity = new ActivityCreateNote() 
            {
                apObject = note
            };

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
            };

            var settings = new InstanceSettings { };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);
            activityPubService
                .Setup(x => x.PostNewActivity(
                    It.Is<ActivityCreateNote>(y => y.apObject.id == noteId),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .Returns(Task.CompletedTask);
                
            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            statusServiceMock
                .Setup(x => x.GetActivity(
                It.Is<string>(y => y == twitterHandle),
                It.Is<ExtractedTweet>(y => y.Id == tweetId.ToString())))
                .Returns(activity);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);

            var loggerMock = new Mock<ILogger<SendTweetsToInboxTask>>();
            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object, settings, loggerMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ExecuteAsync_SingleTweet_ReplyThread_Test()
        {
            #region Stubs
            var tweetId = 10;
            var tweets = new List<ExtractedTweet>
            {
                new ExtractedTweet
                {
                    Id = tweetId.ToString(),
                    IsReply = true,
                    IsThread = true
                }
            };

            var noteId = "noteId";
            var note = new Note()
            {
                id = noteId
            };
            var activity = new ActivityCreateNote()
            {
                apObject = note
            };

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
            };

            var settings = new InstanceSettings
            {
                PublishReplies = false
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);
            activityPubService
                .Setup(x => x.PostNewActivity(
                    It.Is<ActivityCreateNote>(y => y.apObject.id == noteId),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .Returns(Task.CompletedTask);

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            statusServiceMock
                .Setup(x => x.GetActivity(
                It.Is<string>(y => y == twitterHandle),
                It.Is<ExtractedTweet>(y => y.Id == tweetId.ToString())))
                .Returns(activity);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);


            var loggerMock = new Mock<ILogger<SendTweetsToInboxTask>>();
            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object, settings, loggerMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ExecuteAsync_SingleTweet_PublishReply_Test()
        {
            #region Stubs
            var tweetId = 10;
            var tweets = new List<ExtractedTweet>
            {
                new ExtractedTweet
                {
                    Id = tweetId.ToString(),
                    IsReply = true,
                    IsThread = false
                }
            };

            var noteId = "noteId";
            var note = new Note()
            {
                id = noteId
            };
            var activity = new ActivityCreateNote()
            {
                apObject = note
            };

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
            };

            var settings = new InstanceSettings
            {
                PublishReplies = true
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);
            activityPubService
                .Setup(x => x.PostNewActivity(
                    It.Is<ActivityCreateNote>(y => y.apObject.id == noteId),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .Returns(Task.CompletedTask);

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            statusServiceMock
                .Setup(x => x.GetActivity(
                It.Is<string>(y => y == twitterHandle),
                It.Is<ExtractedTweet>(y => y.Id == tweetId.ToString())))
                .Returns(activity);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
 

            var loggerMock = new Mock<ILogger<SendTweetsToInboxTask>>();
            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object, settings, loggerMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ExecuteAsync_MultipleTweets_Test()
        {
            #region Stubs
            var tweetId1 = 10;
            var tweetId2 = 11;
            var tweetId3 = 12;
            var tweets = new List<ExtractedTweet>();
            foreach (var tweetId in new[] { tweetId1, tweetId2, tweetId3 })
            {
                tweets.Add(new ExtractedTweet
                {
                    Id = tweetId.ToString()
                });
            }

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
            };

            var settings = new InstanceSettings
            {
                PublishReplies = false
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);
            foreach (var tweetId in new[] { tweetId1, tweetId2, tweetId3 })
            {
                activityPubService
                    .Setup(x => x.PostNewActivity(
                        It.Is<ActivityCreateNote>(y => y.apObject.id == tweetId.ToString()),
                        It.Is<string>(y => y == twitterHandle),
                        It.Is<string>(y => y == tweetId.ToString()),
                        It.Is<string>(y => y == host),
                        It.Is<string>(y => y == inbox)))
                    .Returns(Task.CompletedTask);
            }

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            foreach (var tweetId in new[] { tweetId1, tweetId2, tweetId3 })
            {
                statusServiceMock
                    .Setup(x => x.GetActivity(
                        It.Is<string>(y => y == twitterHandle),
                        It.Is<ExtractedTweet>(y => y.Id == tweetId.ToString())))
                    .Returns(new ActivityCreateNote { apObject = new Note { id = tweetId.ToString() }});
            }

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);


            var loggerMock = new Mock<ILogger<SendTweetsToInboxTask>>();
            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object, settings, loggerMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task ExecuteAsync_MultipleTweets_Error_Test()
        {
            #region Stubs
            var tweetId1 = 10;
            var tweetId2 = 11;
            var tweetId3 = 12;
            var tweets = new List<ExtractedTweet>();
            foreach (var tweetId in new[] { tweetId1, tweetId2, tweetId3 })
            {
                tweets.Add(new ExtractedTweet
                {
                    Id = tweetId.ToString()
                });
            }

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
            };

            var settings = new InstanceSettings
            {
                PublishReplies = false
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);

            activityPubService
                .Setup(x => x.PostNewActivity(
                    It.Is<ActivityCreateNote>(y => y.apObject.id == tweetId1.ToString()),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId1.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .Returns(Task.CompletedTask);
            
            activityPubService
                .Setup(x => x.PostNewActivity(
                    It.Is<ActivityCreateNote>(y => y.apObject.id == tweetId2.ToString()),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId2.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .Returns(Task.CompletedTask);

            activityPubService
                .Setup(x => x.PostNewActivity(
                    It.Is<ActivityCreateNote>(y => y.apObject.id == tweetId3.ToString()),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId3.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .Throws(new HttpRequestException());

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            foreach (var tweetId in new[] { tweetId1, tweetId2, tweetId3 })
            {
                statusServiceMock
                    .Setup(x => x.GetActivity(
                        It.Is<string>(y => y == twitterHandle),
                        It.Is<ExtractedTweet>(y => y.Id == tweetId.ToString())))
                    .Returns(new ActivityCreateNote { apObject = new Note { id = tweetId.ToString() }});
            }

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);

            var loggerMock = new Mock<ILogger<SendTweetsToInboxTask>>();
            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object, settings, loggerMock.Object);

            try
            {
                await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);
            }
            finally
            {
                #region Validations
                activityPubService.VerifyAll();
                statusServiceMock.VerifyAll();
                followersDalMock.VerifyAll();
                #endregion
            }
        }

        [TestMethod]
        public async Task ExecuteAsync_SingleTweet_ParsingError_Test()
        {
            #region Stubs
            var tweetId = 10;
            var tweets = new List<ExtractedTweet>
            {
                new ExtractedTweet
                {
                    Id = tweetId.ToString(),
                }
            };

            var noteId = "noteId";
            var note = new Note()
            {
                id = noteId
            };

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
            };

            var settings = new InstanceSettings
            {
                PublishReplies = false
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            statusServiceMock
                .Setup(x => x.GetActivity(
                It.Is<string>(y => y == twitterHandle),
                It.Is<ExtractedTweet>(y => y.Id == tweetId.ToString())))
                .Throws(new ArgumentException("Invalid pattern blabla at offset 9"));

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);


            var loggerMock = new Mock<ILogger<SendTweetsToInboxTask>>();
            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object, settings, loggerMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

    }
}