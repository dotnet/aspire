// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using Aspire.Dashboard.Api;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Otlp.Model.Serialization;
using Aspire.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration;

public class TelemetryApiTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TelemetryApiTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task GetSpans_NoAuth_ReturnsUnauthorized()
    {
        // Arrange - with browser token auth mode and API key auth required
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardApiAuthModeName.ConfigKey] = ApiAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardApiPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        // Use a handler that doesn't follow redirects to capture the redirect response
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };

        // Act - no auth (no API key header, no browser token)
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert - should redirect to login (since frontend auth is browser token)
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized or HttpStatusCode.Found,
            $"Expected redirect or unauthorized, got {response.StatusCode}");
    }

    [Fact]
    public async Task GetSpans_UnsecuredMode_Returns200()
    {
        // Arrange - unsecured frontend mode
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<TelemetryApiResponse<OtlpTelemetryDataJson>>(OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
        Assert.NotNull(content);
        Assert.NotNull(content.Data);
    }

    [Fact]
    public async Task GetSpans_WithApiKey_Returns200()
    {
        // Arrange - with Dashboard API key auth
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardApiAuthModeName.ConfigKey] = ApiAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardApiPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(TelemetryApiAuthenticationHandler.ApiKeyHeaderName, apiKey);

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<TelemetryApiResponse<OtlpTelemetryDataJson>>(OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetSpans_WithWrongApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardApiAuthModeName.ConfigKey] = ApiAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardApiPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        // Use a handler that doesn't follow redirects
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(TelemetryApiAuthenticationHandler.ApiKeyHeaderName, "WrongKey!");

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert - wrong API key should return 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSpans_McpDisabled_Returns404()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardMcpDisableName.ConfigKey] = "true";
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTraceById_NotFound_Returns404()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans/nonexistent-trace-id").DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_UnsecuredMode_Returns200()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/logs").DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<TelemetryApiResponse<OtlpTelemetryDataJson>>(OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
        Assert.NotNull(content);
        Assert.NotNull(content.Data);
    }

    [Fact]
    public async Task GetSpanLogs_NoData_ReturnsEmptyList()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans/some-trace-id/logs").DefaultTimeout();

        // Assert - returns 200 with empty data when no logs match the trace ID
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<TelemetryApiResponse<OtlpTelemetryDataJson>>(OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
        Assert.NotNull(content);
        Assert.Equal(0, content.TotalCount);
    }

    [Fact]
    public async Task GetSpans_WithQueryParameters_Returns200()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act - test query parameters without resource filter (no resources exist in test)
        var response = await httpClient.GetAsync("/api/telemetry/spans?hasError=true&limit=50").DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<TelemetryApiResponse<OtlpTelemetryDataJson>>(OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetSpans_WithUnknownResource_Returns404()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act - request with unknown resource filter
        var response = await httpClient.GetAsync("/api/telemetry/spans?resource=unknown-resource").DefaultTimeout();

        // Assert - should return 404 for unknown resource
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_WithQueryParameters_Returns200()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act - test query parameters without resource filter (no resources exist in test)
        var response = await httpClient.GetAsync("/api/telemetry/logs?severity=Error&limit=50").DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<TelemetryApiResponse<OtlpTelemetryDataJson>>(OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetLogs_WithUnknownResource_Returns404()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act - request with unknown resource filter
        var response = await httpClient.GetAsync("/api/telemetry/logs?resource=unknown-resource").DefaultTimeout();

        // Assert - should return 404 for unknown resource
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSpans_ApiAuthModeUnsecured_AllowsAccessWithoutAuth()
    {
        // Arrange - Api auth mode is unsecured (default)
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardApiAuthModeName.ConfigKey] = ApiAuthMode.Unsecured.ToString();
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        // Act - no auth headers at all
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert - should succeed because Api auth is unsecured
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSpans_ApiAuthModeApiKey_RequiresApiKey()
    {
        // Arrange - Api auth mode requires API key
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardApiAuthModeName.ConfigKey] = ApiAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardApiPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        // Use a handler that doesn't follow redirects
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };

        // Act - no API key header
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert - should redirect to login or return unauthorized
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized or HttpStatusCode.Found,
            $"Expected redirect or unauthorized, got {response.StatusCode}");
    }

    [Fact]
    public async Task GetSpans_WithSecondaryApiKey_Returns200()
    {
        // Arrange - with secondary API key (for key rotation)
        var primaryKey = "PrimaryKey123!";
        var secondaryKey = "SecondaryKey456!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardApiAuthModeName.ConfigKey] = ApiAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardApiPrimaryApiKeyName.ConfigKey] = primaryKey;
            config[DashboardConfigNames.DashboardApiSecondaryApiKeyName.ConfigKey] = secondaryKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(TelemetryApiAuthenticationHandler.ApiKeyHeaderName, secondaryKey);

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert - secondary key should work
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSpans_McpKeyFallback_Returns200()
    {
        // Arrange - using legacy MCP key config (backward compatibility)
        var apiKey = "LegacyMcpKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            // Use legacy MCP config instead of new Api config
            config[DashboardConfigNames.DashboardMcpAuthModeName.ConfigKey] = McpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardMcpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(TelemetryApiAuthenticationHandler.ApiKeyHeaderName, apiKey);

        // Act
        var response = await httpClient.GetAsync("/api/telemetry/spans").DefaultTimeout();

        // Assert - MCP key should work via fallback
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
