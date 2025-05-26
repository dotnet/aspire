// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
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
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        
        // Add the Docker client container from our local Dockerfile
        var dockerClientContainer = builder.AddContainer("docker-client", "docker-client-image")
            .WithHttpEndpoint(containerPort: 80, name: "http")
            .WithDockerSocketBindMount() // Add the Docker socket mount
            .WithContainerImageSource(ContainerImageSource.Build, context: "./DockerSocketClientFiles");
            
        // Build and start the application
        using var app = builder.Build();
        await app.StartAsync();
        
        // Wait for the container to be ready
        await app.WaitForHealthyStatusAsync("docker-client");
        
        // Create a client to test the API
        var client = app.CreateHttpClient("docker-client", "http");
        
        // Test the ping endpoint to verify the container is running
        var pingResponse = await client.GetAsync("/ping");
        pingResponse.EnsureSuccessStatusCode();
        var pingContent = await pingResponse.Content.ReadAsStringAsync();
        Assert.Equal("pong", pingContent);
        
        // Test the containers endpoint to verify Docker communication works
        var containersResponse = await client.GetAsync("/containers");
        containersResponse.EnsureSuccessStatusCode();
        var containerResult = await containersResponse.Content.ReadFromJsonAsync<ContainerListResponse>();
        
        Assert.NotNull(containerResult);
        Assert.Equal("success", containerResult.Status);
        Assert.NotEmpty(containerResult.Containers);
        
        // At least one of the containers should be our test container
        Assert.Contains(containerResult.Containers, c => 
            c.Names.Any(name => name.Contains("docker-client", StringComparison.OrdinalIgnoreCase)));
        
        await app.StopAsync();
    }
    
    private class ContainerListResponse
    {
        public string Status { get; set; } = default!;
        public List<ContainerInfo> Containers { get; set; } = new();
    }
    
    private class ContainerInfo
    {
        public string Id { get; set; } = default!;
        public string[] Names { get; set; } = default!;
        public string Image { get; set; } = default!;
        public string State { get; set; } = default!;
        public string Status { get; set; } = default!;
    }
}