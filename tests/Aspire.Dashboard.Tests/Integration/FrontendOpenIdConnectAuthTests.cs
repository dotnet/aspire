// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Web;
using Aspire.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class FrontendOpenIdConnectAuthTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Get_Unauthenticated_RedirectsToAuthority()
    {
        await using var authority = await MockOpenIdAuthority.CreateAsync();

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(
            testOutputHelper,
            additionalConfiguration: config =>
            {
                // Configure the resource service, as otherwise HTTP requests are redirected to /structuredlogs before OIDC
                config[DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey] = "Unsecured";
                config[DashboardConfigNames.ResourceServiceUrlName.ConfigKey] = "https://localhost:1234"; // won't actually exist

                // Configure OIDC. It (the RP) will communicate with the mock authority (IdP) spun up for this test.
                config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = "OpenIdConnect";
                config["Authentication:Schemes:OpenIdConnect:Authority"] = authority.Url;
                config["Authentication:Schemes:OpenIdConnect:ClientId"] = "MyClientId";
                config["Authentication:Schemes:OpenIdConnect:ClientSecret"] = "MyClientSecret";
                // Allow the requirement of HTTPS communication with the OpenIdConnect authority to be relaxed during tests.
                config["Authentication:Schemes:OpenIdConnect:RequireHttpsMetadata"] = "false";
            });

        await app.StartAsync();

        var handler = new HttpClientHandler()
        {
            // Don't follow redirects. We want to validate where the redirect would take us.
            AllowAutoRedirect = false
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri($"http://{app.FrontendEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var redirectedTo = response.Headers.Location;

        Assert.NotNull(redirectedTo);
        Assert.True(redirectedTo.IsAbsoluteUri);
        Assert.Equal("localhost", redirectedTo.Host);
        Assert.Equal("/authorize", redirectedTo.AbsolutePath);

        var query = HttpUtility.ParseQueryString(redirectedTo.Query);
        Assert.Equal("MyClientId", query.Get("client_id"));
        Assert.Equal("code", query.Get("response_type"));
        Assert.Equal("openid profile", query.Get("scope"));

        await app.StopAsync();
    }
}
