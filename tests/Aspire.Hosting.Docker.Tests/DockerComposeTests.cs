// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DockerComposeSetsComputeEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(false);

        // Add a container to the application
        var container = builder.AddContainer("service", "nginx");

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.Same(composeEnv.Resource, container.Resource.GetDeploymentTargetAnnotation()?.ComputeEnvironment);
    }

    [Fact]
    public void PublishingDockerComposeEnviromentPublishesFile()
    {
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName);

        builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(false);

        // Add a container to the application
        builder.AddContainer("service", "nginx");

        var app = builder.Build();
        app.Run();

        var composeFile = Path.Combine(tempDir.FullName, "docker-compose.yaml");
        Assert.True(File.Exists(composeFile), "Docker Compose file was not created.");

        tempDir.Delete(recursive: true);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

    [Fact]
    public async Task DashboardEnabled_InPublishMode_AddsDashboardResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose");

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify dashboard was added (enabled by default)
        var dashboardResource = appModel.Resources.FirstOrDefault(r => r.Name == "docker-compose-dashboard");
        Assert.NotNull(dashboardResource);
        Assert.IsType<ContainerResource>(dashboardResource);
    }

    [Fact]
    public async Task DashboardEnabled_WithOtlpExporter_ConfiguresOtlpEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container with OTLP exporter
        var container = builder.AddContainer("service", "nginx")
            .WithOtlpExporter();

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        // Verify the container has OtlpExporterAnnotation
        var otlpAnnotation = container.Resource.Annotations.OfType<OtlpExporterAnnotation>().FirstOrDefault();
        Assert.NotNull(otlpAnnotation);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify dashboard was added (enabled by default)
        var dashboardResource = appModel.Resources.FirstOrDefault(r => r.Name == "docker-compose-dashboard");
        Assert.NotNull(dashboardResource);
    }

    [Fact]
    public async Task DashboardDisabled_DoesNotAddDashboard()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(false);

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify dashboard was not added
        var dashboardResource = appModel.Resources.FirstOrDefault(r => r.Name == "docker-compose-dashboard");
        Assert.Null(dashboardResource);
    }

    [Fact]
    public async Task DashboardEnabled_InRunMode_DoesNotAddDashboard()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose");

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify dashboard was not added in run mode (even though enabled by default)
        var dashboardResource = appModel.Resources.FirstOrDefault(r => r.Name == "docker-compose-dashboard");
        Assert.Null(dashboardResource);
    }

    [Fact]
    public async Task WithDashboard_EnablesAndDisablesDashboard()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(true);

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify dashboard was added
        var dashboardResource = appModel.Resources.FirstOrDefault(r => r.Name == "docker-compose-dashboard");
        Assert.NotNull(dashboardResource);
    }

    [Fact]
    public async Task ConfigureDashboard_ModifiesDashboardConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customImageCalled = false;
        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard()
            .ConfigureDashboard(dashboard =>
            {
                customImageCalled = true;
                dashboard.WithImage("custom-dashboard", "v1.0");
            });

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify dashboard was added and configured
        var dashboardResource = appModel.Resources.FirstOrDefault(r => r.Name == "docker-compose-dashboard");
        Assert.NotNull(dashboardResource);
        Assert.True(customImageCalled);

        // Verify the custom image was applied
        var containerImageAnnotation = dashboardResource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);
        Assert.Equal("custom-dashboard", containerImageAnnotation.Image);
        Assert.Equal("v1.0", containerImageAnnotation.Tag);
    }
}
