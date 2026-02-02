// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Backchannel;

public class AuxiliaryBackchannelRpcTargetTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task GetResourceSnapshotsAsync_ReturnsEmptyList_WhenAppModelIsNull()
    {
        var services = new ServiceCollection();
        services.AddSingleton(ResourceNotificationServiceTestHelpers.Create());
        var serviceProvider = services.BuildServiceProvider();

        var target = new AuxiliaryBackchannelRpcTarget(
            NullLogger<AuxiliaryBackchannelRpcTarget>.Instance,
            serviceProvider);

        var result = await target.GetResourceSnapshotsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetResourceSnapshotsAsync_EnumeratesResources()
    {
        using var builder = TestDistributedApplicationBuilder.Create(outputHelper);

        builder.AddParameter("myparam");
        builder.AddResource(new CustomResource(KnownResourceNames.AspireDashboard));

        var resourceWithReplicas = builder.AddResource(new CustomResource("myresource"));
        resourceWithReplicas.WithAnnotation(new DcpInstancesAnnotation([
            new DcpInstance("myresource-abc123", "abc123", 0),
            new DcpInstance("myresource-def456", "def456", 1)
        ]));

        using var app = builder.Build();
        await app.StartAsync();

        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await notificationService.PublishUpdateAsync(resourceWithReplicas.Resource, "myresource-abc123", s => s with
        {
            State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Success)
        });
        await notificationService.PublishUpdateAsync(resourceWithReplicas.Resource, "myresource-def456", s => s with
        {
            State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Success)
        });

        var target = new AuxiliaryBackchannelRpcTarget(
            NullLogger<AuxiliaryBackchannelRpcTarget>.Instance,
            app.Services);

        var result = await target.GetResourceSnapshotsAsync();

        // Dashboard resource should be skipped
        Assert.DoesNotContain(result, r => r.Name == KnownResourceNames.AspireDashboard);

        // Parameter resource (no replicas) should be returned with matching Name/DisplayName
        var paramSnapshot = Assert.Single(result, r => r.Name == "myparam");
        Assert.Equal("myparam", paramSnapshot.DisplayName);
        Assert.Equal("Parameter", paramSnapshot.ResourceType);

        // Resource with DcpInstancesAnnotation should return multiple instances
        Assert.Contains(result, r => r.Name == "myresource-abc123");
        Assert.Contains(result, r => r.Name == "myresource-def456");
        Assert.All(result.Where(r => r.Name.StartsWith("myresource-")), r => Assert.Equal("myresource", r.DisplayName));

        await app.StopAsync();
    }

    [Fact]
    public async Task GetResourceSnapshotsAsync_MapsSnapshotData()
    {
        using var builder = TestDistributedApplicationBuilder.Create(outputHelper);

        var custom = builder.AddResource(new CustomResource("myresource"));

        using var app = builder.Build();
        await app.StartAsync();

        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var startedAt = DateTime.UtcNow.AddMinutes(-4);

        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await notificationService.PublishUpdateAsync(custom.Resource, s => s with
        {
            State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Success),
            CreationTimeStamp = createdAt,
            StartTimeStamp = startedAt,
            Urls = [
                new UrlSnapshot("http", "http://localhost:5000", false) { DisplayProperties = new UrlDisplayPropertiesSnapshot("HTTP Endpoint", 1) },
                new UrlSnapshot("https", "https://localhost:5001", true) { DisplayProperties = new UrlDisplayPropertiesSnapshot("HTTPS Endpoint", 2) },
                new UrlSnapshot("inactive", "http://localhost:5002", false) { IsInactive = true }
            ],
            Relationships = [
                new RelationshipSnapshot("dependency1", "Reference"),
                new RelationshipSnapshot("dependency2", "WaitFor")
            ],
            HealthReports = [
                new HealthReportSnapshot("check1", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, "All good", null),
                new HealthReportSnapshot("check2", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, "Failed", "Exception occurred")
            ],
            Volumes = [
                new VolumeSnapshot("/host/path", "/container/path", "bind", false),
                new VolumeSnapshot("myvolume", "/data", "volume", true)
            ],
            EnvironmentVariables = [
                new EnvironmentVariableSnapshot("MY_VAR", "my-value", false),
                new EnvironmentVariableSnapshot("ANOTHER_VAR", "another-value", true)
            ],
            Commands = [
                new ResourceCommandSnapshot("resource-start", ResourceCommandState.Enabled, "Start", "Start the resource", null, null, null, null, false),
                new ResourceCommandSnapshot("resource-stop", ResourceCommandState.Disabled, "Stop", "Stop the resource", null, null, null, null, false),
                new ResourceCommandSnapshot("resource-restart", ResourceCommandState.Hidden, "Restart", null, null, null, null, null, true)
            ],
            Properties = [
                new ResourcePropertySnapshot(CustomResourceKnownProperties.Source, "normal-value"),
                new ResourcePropertySnapshot("ConnectionString", "secret-value") { IsSensitive = true }
            ]
        });

        var target = new AuxiliaryBackchannelRpcTarget(
            NullLogger<AuxiliaryBackchannelRpcTarget>.Instance,
            app.Services);

        var result = await target.GetResourceSnapshotsAsync();

        var snapshot = Assert.Single(result);

        // State
        Assert.Equal("Running", snapshot.State);
        Assert.Equal(KnownResourceStateStyles.Success, snapshot.StateStyle);

        // Timestamps
        Assert.Equal(createdAt, snapshot.CreatedAt);
        Assert.Equal(startedAt, snapshot.StartedAt);

        // URLs (inactive URLs should be excluded)
        Assert.Equal(2, snapshot.Urls.Length);
        Assert.Contains(snapshot.Urls, u => u.Name == "http" && u.Url == "http://localhost:5000" && !u.IsInternal);
        Assert.Contains(snapshot.Urls, u => u.Name == "https" && u.Url == "https://localhost:5001" && u.IsInternal);
        Assert.DoesNotContain(snapshot.Urls, u => u.Name == "inactive");

        // URL display properties
        var httpUrl = snapshot.Urls.Single(u => u.Name == "http");
        Assert.NotNull(httpUrl.DisplayProperties);
        Assert.Equal("HTTP Endpoint", httpUrl.DisplayProperties.DisplayName);
        Assert.Equal(1, httpUrl.DisplayProperties.SortOrder);

        var httpsUrl = snapshot.Urls.Single(u => u.Name == "https");
        Assert.NotNull(httpsUrl.DisplayProperties);
        Assert.Equal("HTTPS Endpoint", httpsUrl.DisplayProperties.DisplayName);
        Assert.Equal(2, httpsUrl.DisplayProperties.SortOrder);

        // Relationships
        Assert.Equal(2, snapshot.Relationships.Length);
        Assert.Contains(snapshot.Relationships, r => r.ResourceName == "dependency1" && r.Type == "Reference");
        Assert.Contains(snapshot.Relationships, r => r.ResourceName == "dependency2" && r.Type == "WaitFor");

        // Health reports
        Assert.Equal(2, snapshot.HealthReports.Length);
        Assert.Contains(snapshot.HealthReports, h => h.Name == "check1" && h.Status == "Healthy");
        Assert.Contains(snapshot.HealthReports, h => h.Name == "check2" && h.Status == "Unhealthy" && h.ExceptionText == "Exception occurred");

        // Volumes
        Assert.Equal(2, snapshot.Volumes.Length);
        Assert.Contains(snapshot.Volumes, v => v.Source == "/host/path" && v.Target == "/container/path" && !v.IsReadOnly);
        Assert.Contains(snapshot.Volumes, v => v.Source == "myvolume" && v.Target == "/data" && v.IsReadOnly);

        // Environment variables
        Assert.Equal(2, snapshot.EnvironmentVariables.Length);
        Assert.Contains(snapshot.EnvironmentVariables, e => e.Name == "MY_VAR" && e.Value == "my-value" && !e.IsFromSpec);
        Assert.Contains(snapshot.EnvironmentVariables, e => e.Name == "ANOTHER_VAR" && e.Value == "another-value" && e.IsFromSpec);

        // Commands
        Assert.Equal(3, snapshot.Commands.Length);
        Assert.Contains(snapshot.Commands, c => c.Name == "resource-start" && c.DisplayName == "Start" && c.Description == "Start the resource" && c.State == "Enabled");
        Assert.Contains(snapshot.Commands, c => c.Name == "resource-stop" && c.DisplayName == "Stop" && c.Description == "Stop the resource" && c.State == "Disabled");
        Assert.Contains(snapshot.Commands, c => c.Name == "resource-restart" && c.DisplayName == "Restart" && c.Description == null && c.State == "Hidden");

        // Properties (sensitive values should be redacted)
        Assert.True(snapshot.Properties.TryGetValue(CustomResourceKnownProperties.Source, out var normalValue));
        Assert.Equal("normal-value", normalValue);
        Assert.True(snapshot.Properties.TryGetValue("ConnectionString", out var sensitiveValue));
        Assert.Null(sensitiveValue);

        await app.StopAsync();
    }

    private sealed class CustomResource(string name) : Resource(name)
    {
    }
}
