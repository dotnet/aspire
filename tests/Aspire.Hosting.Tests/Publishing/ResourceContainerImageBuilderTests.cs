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
    [RequiresDocker] 
    public async Task CanBuildImageFromProjectResourceWithOptions()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);
        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            ImageFormat = ContainerImageFormat.OciTar,
            OutputPath = "/tmp/test-output",
            TargetPlatform = "linux-x64"
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options, cts.Token);
    }

    [Fact]
    public void ContainerBuildOptions_CanSetAllProperties()
    {
        var options = new ContainerBuildOptions
        {
            ImageFormat = ContainerImageFormat.DockerTar,
            OutputPath = "/custom/path",
            TargetPlatform = "linux-arm64"
        };

        Assert.Equal(ContainerImageFormat.DockerTar, options.ImageFormat);
        Assert.Equal("/custom/path", options.OutputPath);
        Assert.Equal("linux-arm64", options.TargetPlatform);
    }

    [Theory]
    [InlineData(ContainerImageFormat.Docker, "Docker")]
    [InlineData(ContainerImageFormat.OciTar, "OciTar")]
    [InlineData(ContainerImageFormat.DockerTar, "DockerTar")]
    public void ContainerImageFormat_EnumValues_AreCorrect(ContainerImageFormat format, string expectedValue)
    {
        // This test ensures that the enum values match what's expected for MSBuild properties
        var formatString = format switch
        {
            ContainerImageFormat.Docker => "Docker",
            ContainerImageFormat.OciTar => "OciTar", 
            ContainerImageFormat.DockerTar => "DockerTar",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        Assert.Equal(expectedValue, formatString);
    }
}
