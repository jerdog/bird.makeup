using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using dotMakeup.Instagram.Models;
using dotMakeup.ipfs;
using Moq;

namespace dotMakeup.Instagram.Tests;

[TestClass]
public class UserTest
{
    private ISocialMediaService _instaService;
    private IIpfsService _ipfsService;
    [TestInitialize]
    public async Task TestInit()
    {
        var userDal = new Mock<IInstagramUserDal>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var settingsDal = new Mock<ISettingsDal>();
        httpFactory.Setup(_ => _.CreateClient(string.Empty)).Returns(new HttpClient());
        var settings = new InstanceSettings
        {
            Domain = "domain.name",
            SidecarURL = "http://localhost:5001"
        };

        _ipfsService = new DotmakeupIpfs(settings, httpFactory.Object);
        _instaService = new InstagramService(_ipfsService, userDal.Object, httpFactory.Object, settings, settingsDal.Object);
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