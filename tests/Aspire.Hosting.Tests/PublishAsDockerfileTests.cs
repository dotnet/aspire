// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using System.Text.Json.Nodes;
using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.Tests;

public class PublishAsDockerfileTests
{
    [Fact]
    public async Task PublishAsDockerFileConfiguresManifestWithoutBuildArgs()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
            .PublishAsDockerFile();

        Assert.True(redis.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var annotation));

        var manifest = await GetManifestAsync(annotation.Callback!);

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

        var redis = builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("SOME_ARG", "TEST")
            ]);

        Assert.True(redis.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var annotation));

        var manifest = await GetManifestAsync(annotation.Callback!);

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

        var redis = builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("THIS_SHOULD_THROW_AS_THERE_IS_NO_ENV_VAR_VALUE")
            ]);

        Assert.True(redis.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var annotation));

        await Assert.ThrowsAsync<DistributedApplicationException>(async () => await GetManifestAsync(annotation.Callback!));
    }

    private static async Task<JsonNode> GetManifestAsync(Func<ManifestPublishingContext, Task> writeManifest)
    {
        using var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        await writeManifest(new ManifestPublishingContext(executionContext, Environment.CurrentDirectory, writer)).ConfigureAwait(false);
        writer.WriteEndObject();
        writer.Flush();
        ms.Position = 0;
        var obj = JsonNode.Parse(ms);
        Assert.NotNull(obj);
        return obj;
    }
}
