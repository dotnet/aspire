// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES003

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
    [ActiveIssue("https://github.com/dotnet/dnceng/issues/6232", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningFromAzdo))]
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
    [ActiveIssue("https://github.com/dotnet/dnceng/issues/6232", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningFromAzdo))]
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
    [ActiveIssue("https://github.com/dotnet/dnceng/issues/6232", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningFromAzdo))]
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

        using var tempDir = new TempDirectory();
        var options = new ContainerBuildOptions
        {
            ImageFormat = ContainerImageFormat.Oci,
            OutputPath = Path.Combine(tempDir.Path, "NewFolder"), // tests that the folder is created if it doesn't exist
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
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/dnceng/issues/6232", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningFromAzdo))]
    public async Task CanBuildImageFromDockerfileResource_WithTrailingSlashContextPath()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();

        // Add trailing slashes to simulate the issue scenario
        var contextPathWithTrailingSlash = tempContextPath + Path.DirectorySeparatorChar;
        var servicea = builder.AddDockerfile("container", contextPathWithTrailingSlash, tempDockerfilePath);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        // This should not fail even with trailing slash in context path
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
    public async Task TagImageAsync_CallsContainerRuntimeTagImage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: false);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        // Act
        await imageBuilder.TagImageAsync("local-image:latest", "target-image:latest", cts.Token);

        // Assert
        Assert.True(fakeContainerRuntime.WasTagImageCalled);
        Assert.Collection(fakeContainerRuntime.TagImageCalls,
            call =>
            {
                Assert.Equal("local-image:latest", call.localImageName);
                Assert.Equal("target-image:latest", call.targetImageName);
            });
    }

    [Fact]
    public async Task PushImageAsync_CallsContainerRuntimePushImage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: false);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        // Act
        await imageBuilder.PushImageAsync("test-image:latest", cts.Token);

        // Assert
        Assert.True(fakeContainerRuntime.WasPushImageCalled);
        Assert.Collection(fakeContainerRuntime.PushImageCalls,
            imageName => Assert.Equal("test-image:latest", imageName));
    }

    [Fact]
    public async Task TagImageAsync_ThrowsWhenContainerRuntimeFails()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: true);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            imageBuilder.TagImageAsync("local-image:latest", "target-image:latest", cts.Token));

        Assert.Equal("Fake container runtime is configured to fail", exception.Message);
        Assert.True(fakeContainerRuntime.WasTagImageCalled);
    }

    [Fact]
    public async Task PushImageAsync_ThrowsWhenContainerRuntimeFails()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: true);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            imageBuilder.PushImageAsync("test-image:latest", cts.Token));

        Assert.Equal("Fake container runtime is configured to fail", exception.Message);
        Assert.True(fakeContainerRuntime.WasPushImageCalled);
    }

    [Fact]
    public async Task BuildImagesAsync_WithOnlyProjectResourcesAndOci_DoesNotNeedContainerRuntime()
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
        var options = new ContainerBuildOptions { ImageFormat = ContainerImageFormat.Oci, OutputPath = "/tmp/test-path" };
        await imageBuilder.BuildImagesAsync([servicea.Resource], options: options, cts.Token);

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

    [Fact]
    public async Task BuildImageAsync_NormalizesContextPathWithTrailingSlashes()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        // Create a fake container runtime that captures the actual context path used
        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: false);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();

        // Add trailing slashes to context path to test normalization
        var contextPathWithTrailingSlash = tempContextPath + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar;
        var dockerfileResource = builder.AddDockerfile("test-dockerfile", contextPathWithTrailingSlash, tempDockerfilePath);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        await imageBuilder.BuildImagesAsync([dockerfileResource.Resource], options: null, cts.Token);

        // Verify that the fake runtime was called to build the image
        Assert.True(fakeContainerRuntime.WasBuildImageCalled);
        Assert.Single(fakeContainerRuntime.BuildImageCalls);

        var buildCall = fakeContainerRuntime.BuildImageCalls[0];

        // The context path should be normalized (no trailing slashes)
        Assert.False(buildCall.contextPath.EndsWith(Path.DirectorySeparatorChar.ToString()));
        Assert.False(buildCall.contextPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()));

        // It should still point to the same directory
        Assert.Equal(Path.GetFullPath(tempContextPath), Path.GetFullPath(buildCall.contextPath));
    }

    [Fact]
    public async Task BuildImageAsync_ThrowsInvalidOperationException_WhenDockerRuntimeNotAvailable()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", new FakeContainerRuntime(shouldFail: true));

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();
        var container = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            imageBuilder.BuildImagesAsync([container.Resource], options: null, cts.Token));

        Assert.Equal("Container runtime is not running or is unhealthy.", exception.Message);

        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        Assert.Contains(logs, log => log.Message.Contains("Container runtime is not running or is unhealthy. Cannot build container images."));
    }

    [Fact]
    [RequiresDocker]
    public async Task CanBuildImageFromDockerfileWithBuildArgsSecretsAndStage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        // Create a fake container runtime to capture build arguments and secrets
        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: false);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();

        // Add parameters for build args and secrets
        builder.Configuration["Parameters:goversion"] = "1.22";
        builder.Configuration["Parameters:secret"] = "mysecret";

        var goVersionParam = builder.AddParameter("goversion");
        var secretParam = builder.AddParameter("secret", secret: true);

        var container = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath, stage: "runner")
                              .WithBuildArg("GO_VERSION", goVersionParam)
                              .WithBuildArg("STATIC_ARG", "static-value")
                              .WithBuildSecret("SECRET_ASENV", secretParam);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(container.Resource, options: null, cts.Token);

        // Validate that BuildImageAsync succeeded by checking the log output
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Check for success logs
        Assert.Contains(logs, log => log.Message.Contains("Building container image for resource container"));
        // Ensure no error logs were produced during the build process
        Assert.DoesNotContain(logs, log => log.Level >= LogLevel.Error &&
            log.Message.Contains("Failed to build container image"));

        // Verify that the correct build arguments were passed
        Assert.NotNull(fakeContainerRuntime.CapturedBuildArguments);
        Assert.Equal(2, fakeContainerRuntime.CapturedBuildArguments.Count);
        Assert.Equal("1.22", fakeContainerRuntime.CapturedBuildArguments["GO_VERSION"]);
        Assert.Equal("static-value", fakeContainerRuntime.CapturedBuildArguments["STATIC_ARG"]);

        // Verify that the correct build secrets were passed
        Assert.NotNull(fakeContainerRuntime.CapturedBuildSecrets);
        Assert.Single(fakeContainerRuntime.CapturedBuildSecrets);
        Assert.Equal("mysecret", fakeContainerRuntime.CapturedBuildSecrets["SECRET_ASENV"]);

        // Verify that the correct stage was passed
        Assert.Equal("runner", fakeContainerRuntime.CapturedStage);
    }

    [Fact]
    public async Task CanResolveBuildArgumentsWithDifferentValueTypes()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        // Create a fake container runtime to capture build arguments
        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: false);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();

        // Add parameters for different value types
        builder.Configuration["Parameters:stringparam"] = "test-value";
        builder.Configuration["Parameters:valueprovider"] = "provider-value";
        var stringParam = builder.AddParameter("stringparam");
        var valueProviderParam = builder.AddParameter("valueprovider");

        // Create a temporary file to test FileInfo handling
        var tempFile = Path.GetTempFileName();
        var fileInfo = new FileInfo(tempFile);

        var container = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath)
                              .WithBuildArg("STRING_ARG", stringParam)
                              .WithBuildArg("BOOL_TRUE_ARG", true)
                              .WithBuildArg("BOOL_FALSE_ARG", false)
                              .WithBuildArg("NULL_ARG", (string?)null)
                              .WithBuildArg("DIRECT_STRING_ARG", "direct-string")
                              .WithBuildArg("EMPTY_STRING_ARG", "")
                              .WithBuildArg("FILEINFO_ARG", fileInfo)
                              .WithBuildArg("VALUEPROVIDER_ARG", valueProviderParam)
                              .WithBuildArg("INT_ARG", 42)
                              .WithBuildArg("DECIMAL_ARG", 3.14);

        using var app = builder.Build();

        try
        {
            using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
            var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
            await imageBuilder.BuildImageAsync(container.Resource, options: null, cts.Token);

            // Verify that different value types are resolved correctly
            Assert.NotNull(fakeContainerRuntime.CapturedBuildArguments);
            Assert.Equal(10, fakeContainerRuntime.CapturedBuildArguments.Count);

            // Parameter should resolve to its configured value (IValueProvider)
            Assert.Equal("test-value", fakeContainerRuntime.CapturedBuildArguments["STRING_ARG"]);

            // Boolean values should be converted to strings
            Assert.Equal("true", fakeContainerRuntime.CapturedBuildArguments["BOOL_TRUE_ARG"]);
            Assert.Equal("false", fakeContainerRuntime.CapturedBuildArguments["BOOL_FALSE_ARG"]);

            // Null should be converted to null (not empty string)
            Assert.Null(fakeContainerRuntime.CapturedBuildArguments["NULL_ARG"]);

            // Direct string should be passed through
            Assert.Equal("direct-string", fakeContainerRuntime.CapturedBuildArguments["DIRECT_STRING_ARG"]);

            // Empty string should be passed through
            Assert.Equal("", fakeContainerRuntime.CapturedBuildArguments["EMPTY_STRING_ARG"]);

            // FileInfo should resolve to its FullName
            Assert.Equal(tempFile, fakeContainerRuntime.CapturedBuildArguments["FILEINFO_ARG"]);

            // IValueProvider (parameter) should resolve to its configured value
            Assert.Equal("provider-value", fakeContainerRuntime.CapturedBuildArguments["VALUEPROVIDER_ARG"]);

            // Integer should be converted to string via ToString()
            Assert.Equal("42", fakeContainerRuntime.CapturedBuildArguments["INT_ARG"]);

            // Decimal should be converted to string via ToString()
            Assert.Equal("3.14", fakeContainerRuntime.CapturedBuildArguments["DECIMAL_ARG"]);
        }
        finally
        {
            // Clean up the temporary file
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ResolveValue_FormatsDecimalWithInvariantCulture()
    {
        // Test decimal value
        var result = await ResourceContainerImageBuilder.ResolveValue(3.14, CancellationToken.None);
        Assert.Equal("3.14", result);
        
        // Test double value
        result = await ResourceContainerImageBuilder.ResolveValue(3.14d, CancellationToken.None);
        Assert.Equal("3.14", result);
        
        // Test float value
        result = await ResourceContainerImageBuilder.ResolveValue(3.14f, CancellationToken.None);
        Assert.Equal("3.14", result);
        
        // Test integer (should also work)
        result = await ResourceContainerImageBuilder.ResolveValue(42, CancellationToken.None);
        Assert.Equal("42", result);
    }

    [Fact]
    public async Task CanResolveBuildSecretsWithDifferentValueTypes()
    {
        using var builder = TestDistributedApplicationBuilder.Create(output);

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(output);
        });

        // Create a fake container runtime to capture build secrets
        var fakeContainerRuntime = new FakeContainerRuntime(shouldFail: false);
        builder.Services.AddKeyedSingleton<IContainerRuntime>("docker", fakeContainerRuntime);

        var (tempContextPath, tempDockerfilePath) = await DockerfileUtils.CreateTemporaryDockerfileAsync();

        // Add parameters for different value types
        builder.Configuration["Parameters:stringsecret"] = "secret-value";
        builder.Configuration["Parameters:nullsecret"] = null;
        var stringSecret = builder.AddParameter("stringsecret", secret: true);
        var nullSecret = builder.AddParameter("nullsecret", secret: true);

        var container = builder.AddDockerfile("container", tempContextPath, tempDockerfilePath)
                              .WithBuildSecret("STRING_SECRET", stringSecret)
                              .WithBuildSecret("NULL_SECRET", nullSecret);

        using var app = builder.Build();

        using var cts = new CancellationTokenSource(TestConstants.LongTimeoutTimeSpan);
        var imageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>();
        await imageBuilder.BuildImageAsync(container.Resource, options: null, cts.Token);

        // Verify that different value types are resolved correctly
        Assert.NotNull(fakeContainerRuntime.CapturedBuildSecrets);
        Assert.Equal(2, fakeContainerRuntime.CapturedBuildSecrets.Count);

        // Parameter should resolve to its configured value
        Assert.Equal("secret-value", fakeContainerRuntime.CapturedBuildSecrets["STRING_SECRET"]);

        // Null parameter should resolve to null
        Assert.Null(fakeContainerRuntime.CapturedBuildSecrets["NULL_SECRET"]);
    }

    private sealed class FakeContainerRuntime(bool shouldFail) : IContainerRuntime
    {
        public string Name => "fake-runtime";
        public bool WasHealthCheckCalled { get; private set; }
        public bool WasTagImageCalled { get; private set; }
        public bool WasPushImageCalled { get; private set; }
        public bool WasBuildImageCalled { get; private set; }
        public List<(string localImageName, string targetImageName)> TagImageCalls { get; } = [];
        public List<string> PushImageCalls { get; } = [];
        public List<(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options)> BuildImageCalls { get; } = [];
        public Dictionary<string, string?>? CapturedBuildArguments { get; private set; }
        public Dictionary<string, string?>? CapturedBuildSecrets { get; private set; }
        public string? CapturedStage { get; private set; }

        public Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken)
        {
            WasHealthCheckCalled = true;
            return Task.FromResult(!shouldFail);
        }

        public Task TagImageAsync(string localImageName, string targetImageName, CancellationToken cancellationToken)
        {
            WasTagImageCalled = true;
            TagImageCalls.Add((localImageName, targetImageName));
            if (shouldFail)
            {
                throw new InvalidOperationException("Fake container runtime is configured to fail");
            }
            return Task.CompletedTask;
        }

        public Task PushImageAsync(string imageName, CancellationToken cancellationToken)
        {
            WasPushImageCalled = true;
            PushImageCalls.Add(imageName);
            if (shouldFail)
            {
                throw new InvalidOperationException("Fake container runtime is configured to fail");
            }
            return Task.CompletedTask;
        }

        public Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken)
        {
            // Capture the arguments for verification in tests
            CapturedBuildArguments = buildArguments;
            CapturedBuildSecrets = buildSecrets;
            CapturedStage = stage;
            WasBuildImageCalled = true;
            BuildImageCalls.Add((contextPath, dockerfilePath, imageName, options));

            if (shouldFail)
            {
                throw new InvalidOperationException("Fake container runtime is configured to fail");
            }

            // For testing, we don't need to actually build anything
            return Task.CompletedTask;
        }
    }
}
