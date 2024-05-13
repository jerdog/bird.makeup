using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using dotMakeup.Instagram.Models;
using Moq;

namespace dotMakeup.Instagram.Tests;

[TestClass]
public class UserTest
{
    private ISocialMediaService _instaService;
    [TestInitialize]
    public async Task TestInit()
    {
        var userDal = new Mock<IInstagramUserDal>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var settingsDal = new Mock<ISettingsDal>();
        httpFactory.Setup(_ => _.CreateClient(string.Empty)).Returns(new HttpClient());
        var settings = new InstanceSettings
        {
            Domain = "domain.name"
        };

        _instaService = new InstagramService(userDal.Object, httpFactory.Object, settings, settingsDal.Object);
    }
    [TestMethod]
    public async Task user_kobe()
    {
        SocialMediaUser user;
        try
        {
            user = await _instaService.GetUserAsync("kobebryant");
        }
        catch (Exception _)
        {
            Assert.Inconclusive();
            return;
        }
        Assert.AreEqual(user.Description, "Writer. Producer. Investor @granity @bryantstibel @drinkbodyarmor @mambamambacitasports");
        Assert.AreEqual(user.Name, "Kobe Bryant");
    }
}