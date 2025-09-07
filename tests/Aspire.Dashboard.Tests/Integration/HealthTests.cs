// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.InternalTesting;
using System.Net;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration;

public class HealthTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task HealthEndpoint_SendRequest_200Response()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(testOutputHelper);
        await app.StartAsync().DefaultTimeout();

        await MakeRequestAndAssert($"http://{app.FrontendSingleEndPointAccessor().EndPoint}", HttpVersion.Version11).DefaultTimeout();
        await MakeRequestAndAssert($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}", HttpVersion.Version11).DefaultTimeout();
        await MakeRequestAndAssert($"http://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", HttpVersion.Version20).DefaultTimeout();

        static async Task MakeRequestAndAssert(string basePath, Version httpVersion)
        {
            using var httpClientHandler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(httpClientHandler) { BaseAddress = new Uri(basePath) };

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, $"/{DashboardUrls.HealthBasePath}");
            request.Version = httpVersion;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            var response = await client.SendAsync(request).DefaultTimeout();

            // Assert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
