// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DockerComposeSetsComputeEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

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

        builder.AddDockerComposeEnvironment("docker-compose");

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
}
