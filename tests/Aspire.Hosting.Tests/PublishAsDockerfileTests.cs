// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;

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

        var manifest = await ManifestUtils.GetManifest(frontend.Resource).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "dockerfile.v0",
              "path": "NodeFrontend/Dockerfile",
              "context": "NodeFrontend",
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}"
              }
            }
            """;

        var actual = manifest.ToString();

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    [Fact]
    public async Task PublishAsDockerFileConfiguresManifestWithBuildArgs()
    {
        var builder = DistributedApplication.CreateBuilder();

        var frontend = builder.AddNpmApp("frontend", "NodeFrontend", "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("SOME_STRING", "Test"),
                new DockerBuildArg("SOME_BOOL", true),
                new DockerBuildArg("SOME_OTHER_BOOL", false),
                new DockerBuildArg("SOME_NUMBER", 7),
                new DockerBuildArg("SOME_NONVALUE"),
            ]);

        Assert.True(frontend.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out _));

        var manifest = await ManifestUtils.GetManifest(frontend.Resource).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "dockerfile.v0",
              "path": "NodeFrontend/Dockerfile",
              "context": "NodeFrontend",
              "buildArgs": {
                "SOME_STRING": "Test",
                "SOME_BOOL": "true",
                "SOME_OTHER_BOOL": "false",
                "SOME_NUMBER": "7",
                "SOME_NONVALUE": null
              },
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}"
              }
            }
            """;

        var actual = manifest.ToString();

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    [Fact]
    public async Task PublishAsDockerFileConfiguresManifestWithBuildArgsThatHaveNoValue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var frontend = builder.AddNpmApp("frontend", "NodeFrontend", "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("SOME_ARG")
            ]);

        Assert.True(frontend.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out _));

        var manifest = await ManifestUtils.GetManifest(frontend.Resource).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "dockerfile.v0",
              "path": "NodeFrontend/Dockerfile",
              "context": "NodeFrontend",
              "buildArgs": {
                "SOME_ARG": null
              },
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}"
              }
            }
            """;

        var actual = manifest.ToString();

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }
}
