// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Web;
using Aspire.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class FrontendOpenIdConnectAuthTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Get_Unauthenticated_RedirectsToAuthority()
    {
        // Create a fake identity provider that will return well-known configuration for OIDC to use,
        // so that we don't have to make HTTP requests across the internet from unit tests.
        var idProvider = new WebHostBuilder()
            .ConfigureServices(services => services.AddRouting())
            // Bind to loopback on a random available port
            .UseKestrel(options => options.Listen(IPAddress.Loopback, 0))
            //.UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps()))
            .Configure(app =>
            {
                // Based on code from https://github.com/dotnet/aspnetcore/blob/3f99d45b0b7d8f0427a3d98acc63098694613362/src/Components/test/testassets/Components.TestServer/RemoteAuthenticationStartup.cs#L37-L94

                var issuer = "";
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet(
                        ".well-known/openid-configuration",
                        (HttpRequest request, [FromHeader] string host) =>
                        {
                            issuer = $"{(request.IsHttps ? "https" : "http")}://{host}";
                            return Results.Json(new
                            {
                                issuer,
                                authorization_endpoint = $"{issuer}/authorize",
                                token_endpoint = $"{issuer}/token",
                            });
                        });

                    var lastCode = "";
                    endpoints.MapGet(
                        "authorize",
                        (string redirect_uri, string? state, string? prompt, bool? preservedExtraQueryParams) =>
                        {
                            // Require interaction so silent sign-in does not skip RedirectToLogin.razor.
                            if (prompt == "none")
                            {
                                return Results.Redirect($"{redirect_uri}?error=interaction_required&state={state}");
                            }

                            // Verify that the extra query parameters added by RedirectToLogin.razor are preserved.
                            if (preservedExtraQueryParams != true)
                            {
                                return Results.Redirect($"{redirect_uri}?error=invalid_request&error_description=extraQueryParams%20not%20preserved&state={state}");
                            }

                            lastCode = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
                            return Results.Redirect($"{redirect_uri}?code={lastCode}&state={state}");
                        });

                    var jwtHandler = new JsonWebTokenHandler();
                    endpoints.MapPost(
                        "token",
                        ([FromForm] string code) =>
                        {
                            if (string.IsNullOrEmpty(lastCode) && code != lastCode)
                            {
                                return Results.BadRequest("Bad code");
                            }

                            return Results.Json(new
                            {
                                token_type = "Bearer",
                                scope = "openid profile",
                                expires_in = 3600,
                                id_token = jwtHandler.CreateToken(new SecurityTokenDescriptor
                                {
                                    Issuer = issuer,
                                    Audience = "s6BhdRkqt3",
                                    Claims = new Dictionary<string, object>
                                    {
                                        ["sub"] = "248289761001",
                                        ["name"] = "Jane Doe",
                                    },
                                }),
                            });
                        }).DisableAntiforgery();
                });
            })
            // Configure logging to the console
            .ConfigureLogging(logging => logging.AddConsole())
            .Build();

        await idProvider.StartAsync();

        var serverAddress = idProvider.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.NotNull(serverAddress);

        var authorityUrl = serverAddress.Addresses.First().Replace("127.0.0.1", "localhost");
        Assert.StartsWith("http://localhost", authorityUrl);

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
        Assert.Equal("/authorize", redirectedTo.AbsolutePath);

        var query = HttpUtility.ParseQueryString(redirectedTo.Query);
        Assert.Equal("MyClientId", query.Get("client_id"));
        Assert.Equal("code", query.Get("response_type"));
        Assert.Equal("openid profile", query.Get("scope"));

        await app.StopAsync();

        await idProvider.StopAsync();
    }
}
