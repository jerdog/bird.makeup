using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using System.Diagnostics.Metrics;

namespace BirdsiteLive.Statistics.Domain
{
    public interface ITwitterStatisticsHandler
    {
        void CalledApi(string ApiName);
        void GotNewTweets(int number);
    }

    public class TwitterStatisticsHandler : ITwitterStatisticsHandler
    {
        private TelemetryClient _telemetryClient;

        static Meter s_meter = new("DotMakeup.Twitter", "1.0.0");
        static Counter<int> newTweets = s_meter.CreateCounter<int>("NewTweets");
        static Counter<int> apiCalled = s_meter.CreateCounter<int>("ApiCalled");
        
        #region Ctor
        public TwitterStatisticsHandler(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }
        #endregion


        public void CalledApi(string ApiName)
        {
            var metric = _telemetryClient.GetMetric("ApiCalled." + ApiName);
            metric.TrackValue(1);
            apiCalled.Add(1, new KeyValuePair<string, object>("api", ApiName));
        }

        public void GotNewTweets(int number)
        {
            var metric = _telemetryClient.GetMetric("Twitter.NewTweets");
            metric.TrackValue(number);
            newTweets.Add(number);
        }

    }
}