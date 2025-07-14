// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Tests.Publishing;

public class ResourceContainerImageBuilderTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromProjectResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options: null, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource servicea"));
        Assert.Contains(logs, log => log.Message.Contains(".NET CLI completed with exit code: 0"));
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromDockerfileResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();
        var servicea = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options: null, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource container"));
        // Ensure no error logs were produced during the build process
        Assert.DoesNotContain(logs, log => log.Level >= LogLevel.Error &&
            log.Message.Contains("Failed to build container image"));
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromProjectResourceWithOptions()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            ImageFormat = ContainerImageFormat.Oci,
            OutputPath = "/tmp/test-output",
            TargetPlatform = ContainerTargetPlatform.LinuxAmd64
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource servicea"));
        Assert.Contains(logs, log => log.Message.Contains(".NET CLI completed with exit code: 0"));

        // Ensure no error logs were produced during the build process
        Assert.DoesNotContain(logs, log => log.Level >= LogLevel.Error &&
            log.Message.Contains("Failed to build container image"));
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromProjectResource_WithDockerImageFormat()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            ImageFormat = ContainerImageFormat.Docker
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource servicea"));
        Assert.Contains(logs, log => log.Message.Contains(".NET CLI completed with exit code: 0"));
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromProjectResource_WithLinuxArm64Platform()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            TargetPlatform = ContainerTargetPlatform.LinuxArm64
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource servicea"));
        Assert.Contains(logs, log => log.Message.Contains(".NET CLI completed with exit code: 0"));
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromDockerfileResource_WithCustomOutputPath()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();
        var container = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        var tempOutputPath = Path.GetTempPath();
        var options = new ContainerBuildOptions
        {
            OutputPath = tempOutputPath,
            ImageFormat = ContainerImageFormat.Oci
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(container.Resource, options, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource container"));
        // Ensure no error logs were produced during the build process
        Assert.DoesNotContain(logs, log => log.Level >= LogLevel.Error &&
            log.Message.Contains("Failed to build container image"));
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromDockerfileResource_WithAllOptionsSet()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();
        var container = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        var tempOutputPath = Path.GetTempPath();
        var options = new ContainerBuildOptions
        {
            ImageFormat = ContainerImageFormat.Oci,
            OutputPath = tempOutputPath,
            TargetPlatform = ContainerTargetPlatform.LinuxAmd64
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(container.Resource, options, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource container"));

        // Ensure no error logs were produced during the build process
        Assert.DoesNotContain(logs, log => log.Level >= LogLevel.Error &&
            log.Message.Contains("Failed to build container image"));
    }

    [Theory]
    [InlineData(ContainerImageFormat.Docker)]
    [InlineData(ContainerImageFormat.Oci)]
    [RequiresDocker]
    public async Task CanBuildImageFromProjectResource_WithDifferentImageFormats(ContainerImageFormat imageFormat)
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            ImageFormat = imageFormat
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource servicea"));
        Assert.Contains(logs, log => log.Message.Contains(".NET CLI completed with exit code: 0"));
    }

    [Theory]
    [InlineData(ContainerTargetPlatform.LinuxAmd64)]
    [InlineData(ContainerTargetPlatform.LinuxArm64)]
    [RequiresDocker]
    public async Task CanBuildImageFromProjectResource_WithDifferentTargetPlatforms(ContainerTargetPlatform targetPlatform)
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        var options = new ContainerBuildOptions
        {
            TargetPlatform = targetPlatform
        };

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(servicea.Resource, options, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource servicea"));
        Assert.Contains(logs, log => log.Message.Contains(".NET CLI completed with exit code: 0"));
    }

    [Fact]
    [RequiresDocker]
    public async Task BuildImageAsync_WithNullOptions_UsesDefaults()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        // Test with null options - should use defaults
        await imageBuilder.BuildImageAsync(servicea.Resource, options: null, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource servicea"));
        Assert.Contains(logs, log => log.Message.Contains(".NET CLI completed with exit code: 0"));
    }

    [Fact]
    public void ContainerBuildOptions_CanSetAllProperties()
    {
        var options = new ContainerBuildOptions
        {
            ImageFormat = ContainerImageFormat.Oci,
            OutputPath = "/custom/path",
            TargetPlatform = ContainerTargetPlatform.LinuxArm64
        };

        Assert.Equal(ContainerImageFormat.Oci, options.ImageFormat);
        Assert.Equal("/custom/path", options.OutputPath);
        Assert.Equal(ContainerTargetPlatform.LinuxArm64, options.TargetPlatform);
    }

    [Fact]
    public async Task BuildImagesAsync_WithOnlyProjectResources_DoesNotNeedContainerRuntime()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        // Create a fake container runtime that would fail if called
        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: true);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        var servicea = builder.AddProject<Projects.ServiceA>("servicea");

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        // This should not fail despite the fake container runtime being configured to fail
        // because we only have project resources (no DockerfileBuildAnnotation)
        await imageBuilder.BuildImagesAsync([servicea.Resource], options: null, cts.Token);

        // Validate that the container runtime health check was not called
        Assert.False(fakeContainerRuntime.WasHealthCheckCalled);
    }

    [Fact]
    public async Task BuildImagesAsync_WithDockerfileResources_ChecksContainerRuntimeHealth()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        // Create a fake container runtime that tracks health check calls
        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: false);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();
        var dockerfileResource = builder.AddDockerfile("test-dockerfile", tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        await imageBuilder.BuildImagesAsync([dockerfileResource.Resource], options: null, cts.Token);

        // Validate that the container runtime health check was called for resources with DockerfileBuildAnnotation
        Assert.True(fakeContainerRuntime.WasHealthCheckCalled);
    }

    private sealed class FakeContainerRuntime(bool shouldFail) : IContainerRuntime
    {
        public string Name => "fake-runtime";
        public bool WasHealthCheckCalled { get; private set; }

        public Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken)
        {
            WasHealthCheckCalled = true;
            return Task.FromResult(!shouldFail);
        }

        public Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, CancellationToken cancellationToken)
        {
            // For testing, we don't need to actually build anything
            return Task.CompletedTask;
        }
    }
}
