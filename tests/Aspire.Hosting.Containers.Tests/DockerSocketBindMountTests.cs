// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;

namespace Aspire.Hosting.Containers.Tests;

public class DockerSocketBindMountTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void WithDockerSocketBindMountCreatesCorrectAnnotation()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddContainer("container", "none")
            .WithDockerSocketBindMount();

        using var app = appBuilder.Build();

        var containerResource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().GetContainerResources());

        Assert.True(containerResource.TryGetLastAnnotation<ContainerMountAnnotation>(out var mountAnnotation));

        Assert.Equal("/var/run/docker.sock", mountAnnotation.Source);
        Assert.Equal("/var/run/docker.sock", mountAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, mountAnnotation.Type);
        Assert.False(mountAnnotation.IsReadOnly);
    }
    
    [Fact]
    [RequiresDocker]
    public async Task DockerSocketBindMountEnablesContainerToAccessDockerDaemon()
    {
        // Create a builder with the test container registry
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["ContainerRegistry:Password"] = "";
        builder.Configuration["ContainerRegistry:Username"] = "";
        builder.Configuration["ContainerRegistry:Address"] = ComponentTestConstants.AspireTestContainerRegistry;
        
        // Add the Docker client container from a pre-built Docker image that can interact with the Docker API
        var dockerClientContainer = builder.AddContainer("docker-client", "docker")
            .WithHttpEndpoint(containerPort: 80, name: "http")
            .WithDockerSocketBindMount() // Add the Docker socket mount
            .WithEnvironment("PATH", "/bin:/usr/bin:/usr/local/bin")
            .WithEntrypoint("/bin/sh", "-c", "docker info && sleep infinity");
            
        // Build the application
        using var app = builder.Build();
        
        // Configure test logging
        app.Services.GetRequiredService<ILoggerFactory>().AddXUnit(testOutputHelper);
        
        // Start the application
        await app.StartAsync();
        
        // Wait for the container to be ready (for 30 seconds max)
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        // Get container logs and verify Docker access
        var containerLogs = "";
        
        // Give it some time to output logs
        await Task.Delay(2000, cts.Token);
        
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"logs {app.Services.GetRequiredService<IResourceMapper>().MapResource(dockerClientContainer.Resource)}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        containerLogs = await process.StandardOutput.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);
        
        // Check for expected Docker info content in logs
        Assert.Contains("Containers:", containerLogs);
        Assert.Contains("Images:", containerLogs);
        Assert.Contains("Server Version:", containerLogs);
        Assert.Contains("OSType:", containerLogs);
        
        await app.StopAsync();
    }
}