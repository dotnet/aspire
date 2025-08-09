// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Dashboard.Tests.Middleware;

public class ForwardedHeadersMiddlewareTests
{
    [Fact]
    public async Task Validate_ForwardedHeaders_MapToHttpRequest()
    {
        using var host = await SetUpHostAsync(true);
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-Host", "example.com");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        var response = await client.GetAsync("/");
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.NotNull(responseContent);
        Assert.Equal("https://example.com", responseContent);
    }

    [Fact]
    public async Task Validate_ForwardedHeaders_NotMapToHttpRequest()
    {
        using var host = await SetUpHostAsync();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-Host", "example.com");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        var response = await client.GetAsync("/");
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.NotNull(responseContent);
        Assert.Equal("http://localhost", responseContent);
    }

    private static async Task<IHost> SetUpHostAsync(bool mapForwardedHeaders = false)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        if (mapForwardedHeaders)
                        {
                            services.Configure<ForwardedHeadersOptions>(options =>
                            {
                                options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
                            });
                        }
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        if (mapForwardedHeaders)
                        {
                            app.UseForwardedHeaders();
                        }

                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync($"{context.Request.Scheme}://{context.Request.Host}");
                        });
                    });
            })
            .StartAsync();
    }
}
