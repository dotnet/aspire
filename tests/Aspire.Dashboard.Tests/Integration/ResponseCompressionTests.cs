// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using System.Net;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class ResponseCompressionTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Html_Responses_Are_Not_Compressed()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(testOutputHelper);
        await app.StartAsync();

        using var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None };
        using var client = new HttpClient(httpClientHandler) { BaseAddress = new Uri($"http://{app.FrontendEndPointAccessor().EndPoint}") };

        // Act 1
        var request = new HttpRequestMessage(HttpMethod.Get, DashboardUrls.StructuredLogsBasePath);
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        var response = await client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(response.Content.Headers, h => h.Key == "Content-Encoding");
    }

    [Theory]
    [InlineData("/js/app.js")]
    [InlineData("/css/app.css")]
    public async Task Static_Asset_Responses_Are_Compressed(string path)
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(testOutputHelper);
        await app.StartAsync();

        using var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None };
        using var client = new HttpClient(httpClientHandler) { BaseAddress = new Uri($"http://{app.FrontendEndPointAccessor().EndPoint}") };

        // Act 1
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        var response = await client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Content.Headers, h => h.Key == "Content-Encoding" && h.Value.Contains("br"));
    }
}
