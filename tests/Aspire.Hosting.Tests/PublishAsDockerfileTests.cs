// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class PublishAsDockerfileTests
{
    [Fact]
    public async Task PublishAsDockerFileConfiguresManifestWithoutBuildArgs()
    {
        var builder = DistributedApplication.CreateBuilder();

        var frontend = builder.AddNpmApp("frontend", "NodeFrontend", "watch")
            .PublishAsDockerFile();

        Assert.True(frontend.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out _));

        var manifest = await ManifestUtils.GetManifest(frontend.Resource);

        Assert.NotNull(manifest);
        Assert.Equal("dockerfile.v0", manifest?["type"]?.ToString());
        Assert.Equal("NodeFrontend/Dockerfile", manifest?["path"]?.ToString());
        Assert.Equal("NodeFrontend", manifest?["context"]?.ToString());
        Assert.Equal("development", manifest?["env"]?["NODE_ENV"]?.ToString());
    }

    [Fact]
    public async Task PublishAsDockerFileConfiguresManifestWithBuildArgs()
    {
        var builder = DistributedApplication.CreateBuilder();

        var frontend = builder.AddNpmApp("frontend", "NodeFrontend", "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("SOME_ARG", "TEST")
            ]);

        Assert.True(frontend.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out _));

        var manifest = await ManifestUtils.GetManifest(frontend.Resource);

        Assert.NotNull(manifest);
        Assert.Equal("dockerfile.v0", manifest?["type"]?.ToString());
        Assert.Equal("NodeFrontend/Dockerfile", manifest?["path"]?.ToString());
        Assert.Equal("NodeFrontend", manifest?["context"]?.ToString());
        Assert.Equal("development", manifest?["env"]?["NODE_ENV"]?.ToString());
        Assert.Equal("TEST", manifest?["buildArgs"]?["SOME_ARG"]?.ToString());
    }

    [Fact]
    public async Task PublishAsDockerFileThrowsWhenBuildArgHasNoEnvVarValue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var frontend = builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("THIS_SHOULD_THROW_AS_THERE_IS_NO_ENV_VAR_VALUE")
            ]);

        Assert.True(frontend.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out _));

        await Assert.ThrowsAsync<DistributedApplicationException>(async () => await ManifestUtils.GetManifest(frontend.Resource));
    }
}
