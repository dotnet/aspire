// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Dashboard.Tests.Middleware;

public class ValidateTokenMiddlewareTests
{
    [Fact]
    public async Task ValidateToken_NotBrowserTokenAuth_RedirectedToHomepage()
    {
        using var host = await SetUpHostAsync(FrontendAuthMode.Unsecured, string.Empty).DefaultTimeout();
        var response = await host.GetTestClient().GetAsync("/login?t=test", TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ValidateToken_NotBrowserTokenAuth_RedirectedToReturnUrl()
    {
        using var host = await SetUpHostAsync(FrontendAuthMode.Unsecured, string.Empty).DefaultTimeout();
        var response = await host.GetTestClient().GetAsync("/login?t=test&returnUrl=/test", TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.Equal("/test", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ValidateToken_BrowserTokenAuth_WrongToken_RedirectsToLogin()
    {
        using var host = await SetUpHostAsync(FrontendAuthMode.BrowserToken, "token").DefaultTimeout();
        var response = await host.GetTestClient().GetAsync("/login?t=wrong", TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.Equal("/login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ValidateToken_BrowserTokenAuth_WrongToken_RedirectsToLogin_WithReturnUrl()
    {
        using var host = await SetUpHostAsync(FrontendAuthMode.BrowserToken, "token").DefaultTimeout();
        var response = await host.GetTestClient().GetAsync("/login?t=wrong&returnUrl=/test", TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.Equal("/login?returnUrl=%2ftest", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ValidateToken_BrowserTokenAuth_RightToken_RedirectsToHome()
    {
        using var host = await SetUpHostAsync(FrontendAuthMode.BrowserToken, "token").DefaultTimeout();
        var response = await host.GetTestClient().GetAsync("/login?t=token", TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ValidateToken_BrowserTokenAuth_RightToken_RedirectsToReturnUrl()
    {
        using var host = await SetUpHostAsync(FrontendAuthMode.BrowserToken, "token").DefaultTimeout();
        var response = await host.GetTestClient().GetAsync("/login?t=token&returnUrl=/test", TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.Equal("/test", response.Headers.Location?.OriginalString);
    }

    private static async Task<IHost> SetUpHostAsync(FrontendAuthMode authMode, string expectedToken)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddAuthentication().AddCookie();

                        services.Configure<DashboardOptions>(o =>
                        {
                            o.Frontend = new FrontendOptions
                            {
                                AuthMode = authMode,
                                BrowserToken = expectedToken,
                                EndpointUrls = "http://localhost/" // required for TryParseOptions
                            };

                            Assert.True(o.Frontend.TryParseOptions(out _));
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<ValidateTokenMiddleware>();
                    });
            })
            .StartAsync();
    }
}
