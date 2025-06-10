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
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        using var tempDir = CreateDirectoryWithDockerFile();

        var path = tempDir.Path;

        var frontend = builder.AddNpmApp("frontend", path, "watch")
            .PublishAsDockerFile();

        // There should be an equivalent container resource with the same name
        // as the npm app resource.
        var containerResource = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal("frontend", containerResource.Name);

        var manifest = await ManifestUtils.GetManifest(frontend.Resource, manifestDirectory: path).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile"
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
    public async Task PublishAsDockerFileConfiguresManifestWithBuildArgs()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        using var tempDir = CreateDirectoryWithDockerFile();

        var path = tempDir.Path;

#pragma warning disable CS0618 // Type or member is obsolete
        var frontend = builder.AddNpmApp("frontend", path, "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("SOME_STRING", "Test"),
                new DockerBuildArg("SOME_BOOL", true),
                new DockerBuildArg("SOME_OTHER_BOOL", false),
                new DockerBuildArg("SOME_NUMBER", 7),
                new DockerBuildArg("SOME_NONVALUE"),
            ]);
#pragma warning restore CS0618 // Type or member is obsolete

        // There should be an equivalent container resource with the same name
        // as the npm app resource.
        var containerResource = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal("frontend", containerResource.Name);

        var manifest = await ManifestUtils.GetManifest(frontend.Resource, manifestDirectory: path).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "args": {
                  "SOME_STRING": "Test",
                  "SOME_BOOL": "true",
                  "SOME_OTHER_BOOL": "false",
                  "SOME_NUMBER": "7",
                  "SOME_NONVALUE": null
                }
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
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        using var tempDir = CreateDirectoryWithDockerFile();

        var path = tempDir.Path;

#pragma warning disable CS0618 // Type or member is obsolete
        var frontend = builder.AddNpmApp("frontend", path, "watch")
            .PublishAsDockerFile(buildArgs: [
                new DockerBuildArg("SOME_ARG")
            ]);
#pragma warning restore CS0618 // Type or member is obsolete

        // There should be an equivalent container resource with the same name
        // as the npm app resource.
        var containerResource = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal("frontend", containerResource.Name);

        var manifest = await ManifestUtils.GetManifest(frontend.Resource, manifestDirectory: path).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "args": {
                  "SOME_ARG": null
                }
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
    public async Task PublishAsDockerFileConfigureContainer()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        using var tempDir = CreateDirectoryWithDockerFile();

        var path = tempDir.Path;

        var secret = builder.AddParameter("secret", secret: true);

        var frontend = builder.AddNpmApp("frontend", path, "watch")
            .WithArgs("/usr/foo")
            .PublishAsDockerFile(c =>
            {
                c.WithBuildSecret("buildSecret", secret);
                c.WithArgs("/app");
                c.WithVolume("vol", "/app/node_modules");
            });

        // There should be an equivalent container resource with the same name
        // as the npm app resource.
        var containerResource = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal("frontend", containerResource.Name);

        var manifest = await ManifestUtils.GetManifest(frontend.Resource, manifestDirectory: path).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "secrets": {
                  "buildSecret": {
                    "type": "env",
                    "value": "{secret.value}"
                  }
                }
              },
              "args": [
                "/app"
              ],
              "volumes": [
                {
                  "name": "vol",
                  "target": "/app/node_modules",
                  "readOnly": false
                }
              ],
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}"
              }
            }
            """;

        var actual = manifest.ToString();

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    [Fact]
    public async Task PublishProjectAsDockerFile()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        using var tempDir = CreateDirectoryWithDockerFile();

        var path = tempDir.Path;
        var projectPath = Path.Combine(path, "project.csproj");

        var project = builder.AddProject("project", projectPath, o => o.ExcludeLaunchProfile = true)
                            .WithArgs("/usr/foo")
                            .PublishAsDockerFile(c =>
                             {
                                 c.WithBuildArg("X", "y");
                                 c.WithArgs("/app");
                                 c.WithVolume("vol", "/app/shared");
                             });
        // There should be an equivalent container resource with the same name
        // as the project resource.
        var containerResource = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal("project", containerResource.Name);

        var manifest = await ManifestUtils.GetManifest(project.Resource, manifestDirectory: path).DefaultTimeout();

        var expected =
            $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "args": {
                  "X": "y"
                }
              },
              "args": [
                "/app"
              ],
              "volumes": [
                {
                  "name": "vol",
                  "target": "/app/shared",
                  "readOnly": false
                }
              ],
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory"
              }
            }
            """;

        var actual = manifest.ToString();
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    private static TempDirectory CreateDirectoryWithDockerFile()
    {
        var tempDir = new TempDirectory();
        File.WriteAllText(Path.Join(tempDir.Path, "Dockerfile"), "this does not matter");
        return tempDir;
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
