// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.HealthChecks.Tests;

public sealed class AspireHealthCheckFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    public async Task HealthEndpointReturnsAspireFormatJson()
    {
        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan);

        // Direct HTTP test to verify the endpoint returns correct JSON
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var healthCheckService = builder.AddProject<Projects.HealthCheckService>("healthcheckservice", launchProfileName: "http");

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        // Wait for the service to be running and get its URL
        var runningEvent = await app.ResourceNotifications.WaitForResourceAsync(
            "healthcheckservice",
            re => re.Snapshot.State?.Text == Aspire.Hosting.ApplicationModel.KnownResourceStates.Running &&
                  re.Snapshot.Urls.Any(),
            cts.Token);

        var httpUrl = runningEvent.Snapshot.Urls.First().Url;
        var healthUrl = $"{httpUrl}/health";

        testOutputHelper.WriteLine($"Calling health endpoint: {healthUrl}");

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(healthUrl, cts.Token);
        var json = await response.Content.ReadAsStringAsync(cts.Token);

        // Output for debugging
        testOutputHelper.WriteLine($"Status Code: {response.StatusCode}");
        testOutputHelper.WriteLine($"JSON Response: {json}");

        // Assert - Should return JSON with multiple entries
        Assert.Contains("\"entries\":", json);
        Assert.Contains("\"database\":", json);
        Assert.Contains("\"cache\":", json);
        Assert.Contains("\"storage\":", json);
        Assert.Contains("\"external_api\":", json);
    }

    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    public async Task WithHttpHealthCheckParsesMultipleHealthChecksFromHttpEndpoint()
    {
        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan);
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        // Add the health check service with HTTP health check annotation
        // This verifies that ResourceBuilderExtensions.WithHttpHealthCheck correctly parses
        // the Aspire health check JSON format and expands multiple health checks
        var healthCheckService = builder.AddProject<Projects.HealthCheckService>("healthcheckservice", launchProfileName: "http")
            .WithHttpHealthCheck("/health");

        using var app = builder.Build();
        await app.StartAsync(cts.Token);

        // Wait for the service to be running and get its URL
        var runningEvent = await app.ResourceNotifications.WaitForResourceAsync(
            "healthcheckservice",
            re => re.Snapshot.State?.Text == Aspire.Hosting.ApplicationModel.KnownResourceStates.Running &&
                  re.Snapshot.Urls.Any(),
            cts.Token);

        // Verify the HTTP endpoint is actually ready by making a direct call
        // This ensures the service is ready before we check health reports
        using var httpClient = new HttpClient();
        var httpUrl = runningEvent.Snapshot.Urls.First().Url;
        var healthUrl = $"{httpUrl}/health";

        // Retry until endpoint responds (service might still be starting up)
        HttpResponseMessage? response = null;
        for (var i = 0; i < 30; i++) // 30 attempts with 1 second delay = 30 seconds max
        {
            try
            {
                response = await httpClient.GetAsync(healthUrl, cts.Token);
                if (response != null)
                {
                    break; // Got a response, endpoint is ready
                }
            }
            catch (HttpRequestException)
            {
                // Endpoint not ready yet, retry
                await Task.Delay(1000, cts.Token);
            }
        }

        Assert.NotNull(response);

        // Wait until the health checks have run and the individual entries are expanded
        // Use ResourceNotifications so the test proceeds as soon as the condition is met.
        var resourceName = "healthcheckservice";
        var latestResource = await app.ResourceNotifications.WaitForResourceAsync(
            resourceName,
            re => re.Snapshot.State?.Text == Aspire.Hosting.ApplicationModel.KnownResourceStates.Running && re.Snapshot.HealthReports.Length >= 4,
            cts.Token);

        // Verify that all individual health checks are expanded and visible to the Dashboard
        var healthReports = latestResource.Snapshot.HealthReports;

        // The key assertion: even though the overall status is Unhealthy,
        // all individual health checks should be expanded and visible
        Assert.True(healthReports.Length >= 4, $"Expected at least 4 health checks, but got {healthReports.Length}. " +
            $"This suggests AspireHttpHealthCheck failed to parse the Aspire JSON format.");

        // Verify database check (Healthy)
        var databaseReport = healthReports.FirstOrDefault(r => r.Name == "database");
        Assert.NotNull(databaseReport);
        Assert.Equal(HealthStatus.Healthy, databaseReport.Status);
        Assert.Equal("Database is responsive", databaseReport.Description);

        // Verify cache check (Healthy)
        var cacheReport = healthReports.FirstOrDefault(r => r.Name == "cache");
        Assert.NotNull(cacheReport);
        Assert.Equal(HealthStatus.Healthy, cacheReport.Status);
        Assert.Equal("Cache is connected", cacheReport.Description);

        // Verify storage check (Degraded)
        var storageReport = healthReports.FirstOrDefault(r => r.Name == "storage");
        Assert.NotNull(storageReport);
        Assert.Equal(HealthStatus.Degraded, storageReport.Status);
        Assert.Equal("Storage is slow", storageReport.Description);

        // Verify external_api check (Unhealthy with exception)
        var externalApiReport = healthReports.FirstOrDefault(r => r.Name == "external_api");
        Assert.NotNull(externalApiReport);
        Assert.Equal(HealthStatus.Unhealthy, externalApiReport.Status);
        Assert.Equal("External API is unavailable", externalApiReport.Description);
        Assert.Contains("Connection timeout", externalApiReport.ExceptionText);

        await app.StopAsync(cts.Token);
    }

    [Fact]
    public async Task AspireHealthCheckResponseWriterHandlesEmptyHealthReport()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.FromMilliseconds(10));

        await AspireHealthCheckResponseWriter.WriteResponse(context, report);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = reader.ReadToEnd();
        var jsonObject = JsonSerializer.Deserialize<JsonNode>(json);
        Assert.NotNull(jsonObject);
        var entries = jsonObject["entries"]?.AsObject();
        Assert.NotNull(entries);
        Assert.Empty(entries);
    }
}
