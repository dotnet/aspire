#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DockerComposeSetsComputeEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose");

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

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container to the application
        builder.AddContainer("service", "nginx");

        var app = builder.Build();
        app.Run();

        var composeFile = Path.Combine(tempDir.FullName, "docker-compose.yaml");
        Assert.True(File.Exists(composeFile), "Docker Compose file was not created.");

        tempDir.Delete(recursive: true);
    }

    [Fact]
    public async Task DockerComposeOnlyExposesExternalEndpoints()
    {
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container with both external and non-external endpoints
        builder.AddContainer("service", "nginx")
               .WithEndpoint(scheme: "http", port: 8080, name: "internal")  // Non-external endpoint
               .WithEndpoint(scheme: "http", port: 8081, name: "external", isExternal: true); // External endpoint

        var app = builder.Build();
        app.Run();

        var composeFile = Path.Combine(tempDir.FullName, "docker-compose.yaml");
        Assert.True(File.Exists(composeFile), "Docker Compose file was not created.");

        var composeContent = File.ReadAllText(composeFile);

        await Verify(composeContent, "yaml");
        
        tempDir.Delete(recursive: true);
    }

    private sealed class MockImageBuilder : IResourceContainerImageBuilder
    {
        public bool BuildImageCalled { get; private set; }

        public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }

        public Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);
}
