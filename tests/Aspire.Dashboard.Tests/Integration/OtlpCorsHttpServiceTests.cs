// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration;

public class OtlpCorsHttpServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OtlpCorsHttpServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ReceivePreflight_OtlpHttpEndPoint_NoCorsConfiguration_NotFound()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync(TestContext.Current.CancellationToken).DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var preflightRequest = new HttpRequestMessage(HttpMethod.Options, "/v1/logs");
        preflightRequest.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        preflightRequest.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "x-requested-with,x-custom,Content-Type");
        preflightRequest.Headers.TryAddWithoutValidation("Origin", "http://localhost:8000");

        // Act
        var responseMessage = await httpClient.SendAsync(preflightRequest, TestContext.Current.CancellationToken).DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, responseMessage.StatusCode);
    }

    [Fact]
    public async Task ReceivePreflight_OtlpHttpEndPoint_ValidCorsOrigin_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpCorsAllowedOriginsKeyName.ConfigKey] = "http://localhost:8000, http://localhost:8001";
        });
        await app.StartAsync(TestContext.Current.CancellationToken).DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        // Act 1
        var preflightRequest1 = new HttpRequestMessage(HttpMethod.Options, "/v1/logs");
        preflightRequest1.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        preflightRequest1.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "x-requested-with,x-custom,Content-Type");
        preflightRequest1.Headers.TryAddWithoutValidation("Origin", "http://localhost:8000");

        var responseMessage1 = await httpClient.SendAsync(preflightRequest1, TestContext.Current.CancellationToken).DefaultTimeout();

        // Assert 1
        Assert.Equal(HttpStatusCode.NoContent, responseMessage1.StatusCode);
        Assert.Equal("http://localhost:8000", responseMessage1.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("POST", responseMessage1.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Equal("X-Requested-With", responseMessage1.Headers.GetValues("Access-Control-Allow-Headers").Single());

        // Act 2
        var preflightRequest2 = new HttpRequestMessage(HttpMethod.Options, "/v1/logs");
        preflightRequest2.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        preflightRequest2.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "x-requested-with,x-custom,Content-Type");
        preflightRequest2.Headers.TryAddWithoutValidation("Origin", "http://localhost:8001");

        var responseMessage2 = await httpClient.SendAsync(preflightRequest2, TestContext.Current.CancellationToken).DefaultTimeout();

        // Assert 2
        Assert.Equal(HttpStatusCode.NoContent, responseMessage2.StatusCode);
        Assert.Equal("http://localhost:8001", responseMessage2.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("POST", responseMessage2.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Equal("X-Requested-With", responseMessage2.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }

    [Fact]
    public async Task ReceivePreflight_OtlpHttpEndPoint_InvalidCorsOrigin_NoCorsHeadersReturned()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpCorsAllowedOriginsKeyName.ConfigKey] = "http://localhost:8000";
        });
        await app.StartAsync(TestContext.Current.CancellationToken).DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var preflightRequest = new HttpRequestMessage(HttpMethod.Options, "/v1/logs");
        preflightRequest.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        preflightRequest.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "x-requested-with,x-custom,Content-Type");
        preflightRequest.Headers.TryAddWithoutValidation("Origin", "http://localhost:8001");

        // Act
        var responseMessage = await httpClient.SendAsync(preflightRequest, TestContext.Current.CancellationToken).DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, responseMessage.StatusCode);
        Assert.False(responseMessage.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(responseMessage.Headers.Contains("Access-Control-Allow-Methods"));
        Assert.False(responseMessage.Headers.Contains("Access-Control-Allow-Headers"));
    }

    [Fact]
    public async Task ReceivePreflight_OtlpHttpEndPoint_AnyOrigin_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpCorsAllowedOriginsKeyName.ConfigKey] = "*";
            config[DashboardConfigNames.DashboardOtlpCorsAllowedHeadersKeyName.ConfigKey] = "*";
        });
        await app.StartAsync(TestContext.Current.CancellationToken).DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var preflightRequest = new HttpRequestMessage(HttpMethod.Options, "/v1/logs");
        preflightRequest.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        preflightRequest.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "x-requested-with,x-custom,Content-Type");
        preflightRequest.Headers.TryAddWithoutValidation("Origin", "http://localhost:8000");

        // Act
        var responseMessage = await httpClient.SendAsync(preflightRequest, TestContext.Current.CancellationToken).DefaultTimeout();

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, responseMessage.StatusCode);
        Assert.Equal("*", responseMessage.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("POST", responseMessage.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Equal("x-requested-with,x-custom,Content-Type", responseMessage.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }
}
