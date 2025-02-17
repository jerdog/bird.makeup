﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.Domain.Statistics;
using Microsoft.AspNetCore.Mvc;

namespace BirdsiteLive.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly IFollowersDal _followersDal;
        private readonly IExtractionStatisticsHandler _extractionStatistics;

        #region Ctor
        public StatisticsController(ITwitterUserDal twitterUserDal, IFollowersDal followersDal, IExtractionStatisticsHandler extractionStatistics)
        {
            _twitterUserDal = twitterUserDal;
            _followersDal = followersDal;
            _extractionStatistics = extractionStatistics;
        }
        #endregion

        public async Task<IActionResult> Index()
        {
            var stats = new Models.StatisticsModels.Statistics
            {
                FollowersCount = await _followersDal.GetFollowersCountAsync(),
                FailingFollowersCount = await _followersDal.GetFailingFollowersCountAsync(),
                TwitterUserCount = await _twitterUserDal.GetTwitterUsersCountAsync(),
                FailingTwitterUserCount = await _twitterUserDal.GetFailingTwitterUsersCountAsync(),
            };
            return View(stats);
        }
    }
}
