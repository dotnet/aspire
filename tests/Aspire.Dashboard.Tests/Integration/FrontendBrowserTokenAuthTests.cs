// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using System.Web;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class FrontendBrowserTokenAuthTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public FrontendBrowserTokenAuthTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Get_Unauthenticated_RedirectToLogin()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.StructuredLogsUrl()), response.RequestMessage!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Get_LoginPage_ValidToken_RedirectToApp()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendEndPointAccessor().EndPoint}") };

        // Act 1
        var response1 = await client.GetAsync(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.TracesUrl(), token: apiKey));

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(DashboardUrls.TracesUrl(), response1.RequestMessage!.RequestUri!.PathAndQuery);

        // Act 2
        var response2 = await client.GetAsync(DashboardUrls.StructuredLogsUrl());

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(DashboardUrls.StructuredLogsUrl(), response2.RequestMessage!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Get_LoginPage_InvalidToken_RedirectToLoginWithoutToken()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.TracesUrl(), token: "Wrong!"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.TracesUrl()), response.RequestMessage!.RequestUri!.PathAndQuery, ignoreCase: true);
    }

    [Theory]
    [InlineData(FrontendAuthMode.BrowserToken, "TestKey123!", HttpStatusCode.OK, true)]
    [InlineData(FrontendAuthMode.BrowserToken, "Wrong!", HttpStatusCode.OK, false)]
    [InlineData(FrontendAuthMode.Unsecured, "Wrong!", HttpStatusCode.BadRequest, null)]
    public async Task Post_ValidateTokenApi_AvailableBasedOnOptions(FrontendAuthMode authMode, string requestToken, HttpStatusCode statusCode, bool? result)
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = authMode.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.PostAsync("/api/validatetoken?token=" + requestToken, content: null);

        // Assert
        Assert.Equal(statusCode, response.StatusCode);

        if (result != null)
        {
            var actualResult = await response.Content.ReadFromJsonAsync<bool>();
            Assert.Equal(result, actualResult);
        }
    }

    [Fact]
    public async Task LogOutput_NoToken_GeneratedTokenLogged()
    {
        // Arrange
        var testSink = new TestSink();
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
        }, testSink: testSink);

        // Act
        await app.StartAsync();

        // Assert
        var l = testSink.Writes.Where(w => w.LoggerName == typeof(DashboardWebApplication).FullName).ToList();
        Assert.Collection(l,
            w =>
            {
                Assert.Equal("Aspire version: {Version}", GetValue(w.State, "{OriginalFormat}"));
            },
            w =>
            {
                Assert.Equal("Now listening on: {DashboardUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "DashboardUri")!);
                Assert.NotEqual(0, uri.Port);
            },
            w =>
            {
                Assert.Equal("Login to the dashboard at {DashboardLoginUrl}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "DashboardLoginUrl")!, UriKind.Absolute);
                var queryString = HttpUtility.ParseQueryString(uri.Query);
                Assert.NotNull(queryString["t"]);
            },
            w =>
            {
                Assert.Equal("OTLP/gRPC listening on: {OtlpEndpointUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "OtlpEndpointUri")!);
                Assert.NotEqual(0, uri.Port);
            },
            w =>
            {
                Assert.Equal("OTLP/HTTP listening on: {OtlpEndpointUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "OtlpEndpointUri")!);
                Assert.NotEqual(0, uri.Port);
            },
            w =>
            {
                Assert.Equal("OTLP server is unsecured. Untrusted apps can send telemetry to the dashboard. For more information, visit https://go.microsoft.com/fwlink/?linkid=2267030", GetValue(w.State, "{OriginalFormat}"));
                Assert.Equal(LogLevel.Warning, w.LogLevel);
            });

        object? GetValue(object? values, string key)
        {
            var list = values as IReadOnlyList<KeyValuePair<string, object>>;
            return list?.SingleOrDefault(kvp => kvp.Key == key).Value;
        }
    }
}
