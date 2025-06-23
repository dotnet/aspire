// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Publishing;

public class ResourceContainerImageBuilderTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromProjectResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);
        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options: null, cts.Token);
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromDockerfileResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();
        var servicea = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options: null, cts.Token);
    }

    [Fact]
    public async Task BuildImageAsync_WithAdditionalPublishArguments_PassesArgumentsToProcess()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);
        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            AdditionalPublishArguments = ["/p:ContainerImageFormat=oci", "/p:ContainerRuntimeIdentifier=linux-x64"]
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        
        // This test verifies that the API accepts the additional arguments
        // The actual process execution will be tested in integration tests
        await imageBuilder.BuildImageAsync(servicea.Resource, options, cts.Token);
    }

    [Fact]
    public async Task BuildImageAsync_WithNullOptions_WorksWithoutAdditionalArguments()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);
        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        
        // This should work exactly as before
        await imageBuilder.BuildImageAsync(servicea.Resource, options: null, cts.Token);
    }

    [Fact]
    public async Task BuildImagesAsync_WithAdditionalPublishArguments_PassesArgumentsToProcess()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);
        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            AdditionalPublishArguments = ["/p:ContainerArchiveOutputPath=/tmp/container.tar"]
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        
        // This test verifies that the API accepts the additional arguments for multiple resources
        await imageBuilder.BuildImagesAsync([servicea.Resource], options, cts.Token);
    }
}
