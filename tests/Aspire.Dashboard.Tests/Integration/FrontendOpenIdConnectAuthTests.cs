// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Web;
using Aspire.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class FrontendOpenIdConnectAuthTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Get_Unauthenticated_RedirectsToAuthority()
    {
        // The well-known configuration JSON for our mock OpenIDConnect authority.
        // Will be populate after the server is bound, as it needs to contain the bound
        // port number which we won't have until later.
        var wellKnownConfiguration = "";

        // Create a fake identity provider that will return well-known configuration for OIDC to use,
        // so that we don't have to make HTTP requests across the internet from unit tests.
        var idProvider = new WebHostBuilder()
            .UseKestrel(options =>
            {
                // Bind to loopback on a random available port
                options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps());
            })
            .Configure(app => app.Run(async context => await context.Response.WriteAsync(wellKnownConfiguration)))
            .Build();

        await idProvider.StartAsync();

        var serverAddress = idProvider.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.NotNull(serverAddress);

        var authorityUrl = serverAddress.Addresses.First().Replace("127.0.0.1", "localhost");
        Assert.StartsWith("https://localhost", authorityUrl);

        // Based on configuration from: https://login.microsoftonline.com/<directory-id>/v2.0/.well-known/openid-configuration
        wellKnownConfiguration = $$"""
            {
                "token_endpoint": "{{authorityUrl}}/oauth2/v2.0/token",
                "token_endpoint_auth_methods_supported": [ "client_secret_post", "private_key_jwt", "client_secret_basic" ],
                "jwks_uri": "{{authorityUrl}}/discovery/v2.0/keys",
                "response_modes_supported": [ "query", "fragment", "form_post" ],
                "subject_types_supported": [ "pairwise" ],
                "id_token_signing_alg_values_supported": [ "RS256" ],
                "response_types_supported": [ "code", "id_token", "code id_token", "id_token token" ],
                "scopes_supported": [ "openid", "profile", "email", "offline_access" ],
                "issuer": "{{authorityUrl}}/v2.0",
                "request_uri_parameter_supported": false,
                "userinfo_endpoint": "https://graph.microsoft.com/oidc/userinfo",
                "authorization_endpoint": "{{authorityUrl}}/oauth2/v2.0/authorize",
                "device_authorization_endpoint": "{{authorityUrl}}/oauth2/v2.0/devicecode",
                "http_logout_supported": true,
                "frontchannel_logout_supported": true,
                "end_session_endpoint": "{{authorityUrl}}/oauth2/v2.0/logout",
                "claims_supported": [ "sub", "iss", "cloud_instance_name", "cloud_instance_host_name", "cloud_graph_host_name", "msgraph_host",
                    "aud", "exp", "iat", "auth_time", "acr", "nonce", "preferred_username", "name", "tid", "ver", "at_hash", "c_hash", "email" ],
                "kerberos_endpoint": "{{authorityUrl}}/kerberos",
                "tenant_region_scope": "WW",
                "cloud_instance_name": "microsoftonline.com",
                "cloud_graph_host_name": "graph.windows.net",
                "msgraph_host": "graph.microsoft.com",
                "rbac_url": "https://pas.windows.net"
            }
            """;

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(
            testOutputHelper,
            additionalConfiguration: config =>
            {
                // Configure the resource service, as otherwise HTTP requests are redirected to /structuredlogs before OIDC
                config[DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey] = "Unsecured";
                config[DashboardConfigNames.ResourceServiceUrlName.ConfigKey] = "https://localhost:1234"; // won't actually exist

                // Configure OIDC. We must use an actual IdP because the provider will query "/.well-known/openid-configuration"
                // at the authority.
                config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = "OpenIdConnect";
                config["Authentication:Schemes:OpenIdConnect:Authority"] = authorityUrl;
                config["Authentication:Schemes:OpenIdConnect:ClientId"] = "MyClientId";
                config["Authentication:Schemes:OpenIdConnect:ClientSecret"] = "MyClientSecret";
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
        Assert.Equal("/oauth2/v2.0/authorize", redirectedTo.AbsolutePath);

        var query = HttpUtility.ParseQueryString(redirectedTo.Query);
        Assert.Equal("MyClientId", query.Get("client_id"));
        Assert.Equal("code", query.Get("response_type"));
        Assert.Equal("openid profile", query.Get("scope"));

        await app.StopAsync();

        await idProvider.StopAsync();
    }
}
