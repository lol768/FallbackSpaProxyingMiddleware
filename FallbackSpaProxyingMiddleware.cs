using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace FallbackSpaProxyingMiddleware;

/// SPDX-License-Identifier: MIT
public class FallbackSpaProxyingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _baseUrl;
    private readonly HttpClient _neverTimeOutHttpClient;

    public FallbackSpaProxyingMiddleware(RequestDelegate next, IHostApplicationLifetime lifetime, string baseUrl,
        HttpClient neverTimeOutHttpClient)
    {
        _next = next;
        _lifetime = lifetime;
        _baseUrl = baseUrl;
        _neverTimeOutHttpClient = neverTimeOutHttpClient;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.GetEndpoint() == null)
        {
            var spaProxyType =
                Type.GetType(
                    "Microsoft.AspNetCore.SpaServices.Extensions.Proxy.SpaProxy, Microsoft.AspNetCore.SpaServices.Extensions");
            var performProxyRequestMethod =
                spaProxyType.GetMethod("PerformProxyRequest", BindingFlags.Public | BindingFlags.Static);

            await (Task<bool>)performProxyRequestMethod.Invoke(
                null,
                new object[]
                {
                    context, _neverTimeOutHttpClient, Task.FromResult(new Uri(_baseUrl)),
                    _lifetime.ApplicationStopping, true
                });
        }
        else
        {
            // Defer to the registered endpoint
            await _next(context);
        }
    }
}

public static class FallbackSpaProxyingMiddlewareExtensions
{
    public static IApplicationBuilder UseFallbackSpaProxying(
        this IApplicationBuilder builder, string baseUri)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            MaxConnectionsPerServer = 10000,
        };

        var neverTimeOutHttpClient = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        return builder.UseMiddleware<FallbackSpaProxyingMiddleware>(baseUri, neverTimeOutHttpClient);
    }
}
