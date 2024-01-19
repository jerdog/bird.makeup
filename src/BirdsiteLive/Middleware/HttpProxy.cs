namespace BirdsiteLive.Middleware;

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddProxySupport(this IHttpClientBuilder builder, string proxyUri, string proxyUser, string proxyPassword)
    {
        if (proxyUri is null)
            return builder;
        
        var proxy = new WebProxy
        {
            Address = new Uri(proxyUri),
            BypassProxyOnLocal = false,
            UseDefaultCredentials = false,

            // Proxy credentials
            Credentials = new NetworkCredential(
                userName: proxyUser,
                password: proxyPassword)
        };
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true,
            };
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });
    }
}