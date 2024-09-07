using BirdsiteLive.Common.Settings;
using Ipfs.Http;

namespace dotMakeup.ipfs;

public interface IIpfsService
{
    string GetIpfsPublicLink(string hash);
    Task<string> Mirror(string upstream);
    
}
public class DotmakeupIpfs : IIpfsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private InstanceSettings _instanceSettings;
    private readonly IpfsClient _ipfs;
    #region Ctor
    public DotmakeupIpfs(InstanceSettings instanceSettings, IHttpClientFactory httpClientFactory)
    {
        _instanceSettings = instanceSettings;
        _httpClientFactory = httpClientFactory;
        _ipfs = new IpfsClient();
        if (_instanceSettings.IpfsApi is not null)
            _ipfs.ApiUri = new Uri(_instanceSettings.IpfsApi);

    }
    #endregion

    public string GetIpfsPublicLink(string hash)
    {
        return "https://ipfs.io/ipfs/" + hash;
    }

    public async Task<string> Mirror(string upstream)
    {
        var client = _httpClientFactory.CreateClient();
        var pic = await client.GetAsync(upstream);
        pic.EnsureSuccessStatusCode();
        var picData = await pic.Content.ReadAsByteArrayAsync();
        
        using var memoryStream = new MemoryStream(picData);
        
        var i = await _ipfs.FileSystem.AddAsync(memoryStream);
        await _ipfs.Pin.AddAsync(i.Id);
        
        return i.Id;
    }
}
