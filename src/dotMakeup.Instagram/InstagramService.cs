using System.Net;
using System.Text.Json;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using dotMakeup.Instagram.Models;

namespace dotMakeup.Instagram;

public class InstagramService : ISocialMediaService
{
        private readonly IHttpClientFactory _httpClientFactory;

        #region Ctor
        public InstagramService(IInstagramUserDal userDal, IHttpClientFactory httpClientFactory, InstanceSettings settings)
        {
            _httpClientFactory = httpClientFactory;
            UserDal = userDal;
        }
        #endregion

        public async Task<SocialMediaPost?> GetPostAsync(long id)
        {
            return null;
        }

        public string ServiceName { get; } = "Instagram";
        public SocialMediaUserDal UserDal { get; }
        public async Task<SocialMediaUser> GetUserAsync(string username)
        {
            var client = _httpClientFactory.CreateClient();
            string requestUrl;
            requestUrl = "http://localhost:5000/instagram/user/" + username;
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            var httpResponse = await client.SendAsync(request);
            if (httpResponse.StatusCode != HttpStatusCode.OK)
                return null;
            
            var c = await httpResponse.Content.ReadAsStringAsync();
            var userDocument = JsonDocument.Parse(c);
            return new InstagramUser()
            {
                Description = userDocument.RootElement.GetProperty("bio").GetString(),
                Acct = username,
                ProfileImageUrl = userDocument.RootElement.GetProperty("profilePic").GetString(),
                Name = userDocument.RootElement.GetProperty("name").GetString(),
            };
        }
}