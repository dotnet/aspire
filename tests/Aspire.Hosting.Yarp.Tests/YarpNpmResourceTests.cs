// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTING001 // Type is for evaluation purposes only

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Yarp.Tests;

public class YarpNpmResourceTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void AddYarpNpmAppCreatesYarpNpmResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path);

        var resource = Assert.Single(builder.Resources.OfType<YarpNpmResource>());
        Assert.Equal("frontend", resource.Name);
    }

    [Fact]
    public void AddYarpNpmAppAddsNodeStaticBuildOptionsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>());
        Assert.NotNull(annotation.Options);
        Assert.Equal("npm", annotation.Options.PackageManager);
        Assert.Equal("install", annotation.Options.InstallCommand);
        Assert.Equal("run build", annotation.Options.BuildCommand);
        Assert.Equal("dist", annotation.Options.OutputDir);
        Assert.Equal("22", annotation.Options.NodeVersion);
    }

    [Fact]
    public void AddYarpNpmAppAddsDockerfileBuildAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.NotNull(annotation.DockerfileFactory);
        Assert.Equal(tempDir.Path, annotation.ContextPath);
    }

    [Fact]
    public async Task AddYarpNpmAppEnablesStaticFiles()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var value = Assert.Contains("YARP_ENABLE_STATIC_FILES", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public void WithPackageManagerSetsPackageManager()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithPackageManager("pnpm");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>());
        Assert.Equal("pnpm", annotation.Options.PackageManager);
    }

    [Fact]
    public void WithInstallCommandSetsInstallCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithInstallCommand("install --frozen-lockfile");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>());
        Assert.Equal("install --frozen-lockfile", annotation.Options.InstallCommand);
    }

    [Fact]
    public void WithBuildCommandSetsBuildCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithBuildCommand("build");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>());
        Assert.Equal("build", annotation.Options.BuildCommand);
    }

    [Fact]
    public void WithOutputDirSetsOutputDir()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithOutputDir("build");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>());
        Assert.Equal("build", annotation.Options.OutputDir);
    }

    [Fact]
    public void WithNodeVersionSetsNodeVersion()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithNodeVersion("20");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>());
        Assert.Equal("20", annotation.Options.NodeVersion);
    }

    [Fact]
    public void FluentConfigurationChaining()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithPackageManager("pnpm")
            .WithInstallCommand("install --frozen-lockfile")
            .WithBuildCommand("build")
            .WithOutputDir("build")
            .WithNodeVersion("20");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>());
        Assert.Equal("pnpm", annotation.Options.PackageManager);
        Assert.Equal("install --frozen-lockfile", annotation.Options.InstallCommand);
        Assert.Equal("build", annotation.Options.BuildCommand);
        Assert.Equal("build", annotation.Options.OutputDir);
        Assert.Equal("20", annotation.Options.NodeVersion);
    }

    [Fact]
    public async Task DockerfileFactoryGeneratesCorrectDockerfileWithDefaults()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.NotNull(annotation.DockerfileFactory);

        var context = new DockerfileFactoryContext
        {
            Services = TestServiceProvider.Instance,
            Resource = yarp.Resource,
            CancellationToken = CancellationToken.None
        };

        var dockerfile = await annotation.DockerfileFactory(context);

        Assert.Contains("FROM node:22 AS builder", dockerfile);
        Assert.Contains("WORKDIR /app", dockerfile);
        Assert.Contains("COPY . .", dockerfile);
        Assert.Contains("RUN npm install", dockerfile);
        Assert.Contains("RUN npm run build", dockerfile);
        Assert.Contains("COPY --from=builder /app/dist ./wwwroot", dockerfile);
    }

    [Fact]
    public async Task DockerfileFactoryGeneratesCorrectDockerfileWithPnpm()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithPackageManager("pnpm")
            .WithInstallCommand("install --frozen-lockfile")
            .WithBuildCommand("build")
            .WithOutputDir("build")
            .WithNodeVersion("20");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.NotNull(annotation.DockerfileFactory);

        var context = new DockerfileFactoryContext
        {
            Services = TestServiceProvider.Instance,
            Resource = yarp.Resource,
            CancellationToken = CancellationToken.None
        };

        var dockerfile = await annotation.DockerfileFactory(context);

        Assert.Contains("FROM node:20 AS builder", dockerfile);
        Assert.Contains("WORKDIR /app", dockerfile);
        Assert.Contains("COPY . .", dockerfile);
        Assert.Contains("RUN pnpm install --frozen-lockfile", dockerfile);
        Assert.Contains("RUN pnpm build", dockerfile);
        Assert.Contains("COPY --from=builder /app/build ./wwwroot", dockerfile);
    }

    [Fact]
    public async Task DockerfileFactoryGeneratesCorrectDockerfileWithYarn()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path)
            .WithPackageManager("yarn")
            .WithInstallCommand("install")
            .WithNodeVersion("18");

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.NotNull(annotation.DockerfileFactory);

        var context = new DockerfileFactoryContext
        {
            Services = TestServiceProvider.Instance,
            Resource = yarp.Resource,
            CancellationToken = CancellationToken.None
        };

        var dockerfile = await annotation.DockerfileFactory(context);

        Assert.Contains("FROM node:18 AS builder", dockerfile);
        Assert.Contains("WORKDIR /app", dockerfile);
        Assert.Contains("COPY . .", dockerfile);
        Assert.Contains("RUN yarn install", dockerfile);
        Assert.Contains("RUN yarn run build", dockerfile);
    }

    [Fact]
    public async Task DockerfileFactoryIncludesYarpImageReference()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarpNpmApp("frontend", tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.NotNull(annotation.DockerfileFactory);

        var context = new DockerfileFactoryContext
        {
            Services = TestServiceProvider.Instance,
            Resource = yarp.Resource,
            CancellationToken = CancellationToken.None
        };

        var dockerfile = await annotation.DockerfileFactory(context);

        Assert.Contains($"FROM {YarpContainerImageTags.Registry}/{YarpContainerImageTags.Image}:{YarpContainerImageTags.Tag} AS yarp", dockerfile);
    }
}
