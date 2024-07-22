using Microsoft.ApplicationInsights;

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
        }

        public void GotNewTweets(int number)
        {
            var metric = _telemetryClient.GetMetric("Twitter.NewTweets");
            metric.TrackValue(number);
        }

    }
}