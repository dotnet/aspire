// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.HealthChecks.Tests;

public class AspireHttpHealthCheckUnitTests
{
    [Fact]
    public async Task CheckHealthAsync_ParsesAspireFormatJson()
    {
        // Arrange
        var json = """
            {
                "status": "Unhealthy",
                "entries": {
                    "database": {
                        "status": "Healthy",
                        "description": "Database is responsive"
                    },
                    "cache": {
                        "status": "Healthy",
                        "description": "Cache is connected"
                    },
                    "storage": {
                        "status": "Degraded",
                        "description": "Storage is slow"
                    },
                    "external_api": {
                        "status": "Unhealthy",
                        "description": "External API is unavailable"
                    }
                }
            }
            """;

        var handler = new TestHttpMessageHandler(HttpStatusCode.ServiceUnavailable, json);
        var httpClient = new HttpClient(handler);
        var healthCheck = new AspireHttpHealthCheck(
            () => new Uri("http://test/health"),
            () => httpClient,
            "test-resource",
            200);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);

        // Verify the data dictionary contains the required keys
        Assert.True(result.Data.ContainsKey("__AspireMultipleHealthChecks"));
        Assert.Equal(true, result.Data["__AspireMultipleHealthChecks"]);

        Assert.True(result.Data.ContainsKey("SubEntries"));
        var subEntries = Assert.IsType<Dictionary<string, HealthReportEntry>>(result.Data["SubEntries"]);

        // Verify all 4 sub-entries are present
        Assert.Equal(4, subEntries.Count);
        Assert.Contains("database", subEntries.Keys);
        Assert.Contains("cache", subEntries.Keys);
        Assert.Contains("storage", subEntries.Keys);
        Assert.Contains("external_api", subEntries.Keys);

        // Verify individual statuses
        Assert.Equal(HealthStatus.Healthy, subEntries["database"].Status);
        Assert.Equal(HealthStatus.Healthy, subEntries["cache"].Status);
        Assert.Equal(HealthStatus.Degraded, subEntries["storage"].Status);
        Assert.Equal(HealthStatus.Unhealthy, subEntries["external_api"].Status);
    }

    private sealed class TestHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
            return Task.FromResult(response);
        }
    }
}
