// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.NodeJs.Tests;

public class AddNodeAppTests
{
    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithResourceCleanUp(true);

        var workingDirectory = AppContext.BaseDirectory;
        var nodeApp = builder.AddNodeApp("nodeapp", workingDirectory, "..\\foo\\app.js")
            .WithHttpEndpoint(port: 5031, env: "PORT");
        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var expectedManifest = $$"""
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "node",
              "args": [
                "..\\foo\\app.js"
              ],
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}",
                "PORT": "{nodeapp.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 5031,
                  "targetPort": 8000
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

#pragma warning disable CS0618 // Type or member is obsolete
        var npmApp = builder.AddNpmApp("npmapp", workingDirectory)
            .WithHttpEndpoint(port: 5032, env: "PORT");
#pragma warning restore CS0618 // Type or member is obsolete

        manifest = await ManifestUtils.GetManifest(npmApp.Resource);

        expectedManifest = $$"""
            {
              "type": "executable.v0",
              "workingDirectory": ".",
              "command": "npm",
              "args": [
                "run",
                "start"
              ],
              "env": {
                "NODE_ENV": "{{builder.Environment.EnvironmentName.ToLowerInvariant()}}",
                "PORT": "{npmapp.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 5032,
                  "targetPort": 8000
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyDockerfile(bool includePackageJson)
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputPath: tempDir.Path).WithResourceCleanUp(true);

        var appDir = Path.Combine(tempDir.Path, "js");
        Directory.CreateDirectory(appDir);

        if (includePackageJson)
        {
            File.WriteAllText(Path.Combine(appDir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(appDir, "package-lock.json"), "{}");
        }

        var nodeApp = builder.AddNodeApp("js", appDir, "app.js");

        await ManifestUtils.GetManifest(nodeApp.Resource, tempDir.Path);

        var dockerfilePath = Path.Combine(tempDir.Path, "js.Dockerfile");
        var dockerfileContents = File.ReadAllText(dockerfilePath);
        var expectedDockerfile = $"""
            FROM node:22-alpine AS build
            
            WORKDIR /app
            COPY . .
            
            {(includePackageJson ? "RUN npm ci\n" : "")}
            FROM node:22-alpine AS runtime
            
            WORKDIR /app
            COPY --from=build /app /app
            
            ENV NODE_ENV=production
            EXPOSE 3000
            
            USER node
            
            ENTRYPOINT ["node","app.js"]

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);

        var dockerBuildAnnotation = nodeApp.Resource.Annotations.OfType<DockerfileBuildAnnotation>().Single();
        Assert.True(dockerBuildAnnotation.HasEntrypoint);

        Assert.Empty(nodeApp.Resource.Annotations.OfType<ContainerFilesSourceAnnotation>());
    }

    [Fact]
    public async Task VerifyDockerfileWithBuildScript()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputPath: tempDir.Path).WithResourceCleanUp(true);

        var appDir = Path.Combine(tempDir.Path, "js");
        Directory.CreateDirectory(appDir);
        File.WriteAllText(Path.Combine(appDir, "package.json"), "{}");

        var nodeApp = builder.AddNodeApp("js", appDir, "app.js")
            .WithAnnotation(new JavaScriptPackageManagerAnnotation("mypm", runScriptCommand: null))
            .WithAnnotation(new JavaScriptInstallCommandAnnotation(["myinstall"]))
            .WithBuildScript("mybuild");

        await ManifestUtils.GetManifest(nodeApp.Resource, tempDir.Path);

        var dockerfilePath = Path.Combine(tempDir.Path, "js.Dockerfile");
        var dockerfileContents = File.ReadAllText(dockerfilePath);
        var expectedDockerfile = $"""
            FROM node:22-alpine AS build

            WORKDIR /app
            COPY . .

            RUN mypm myinstall
            RUN mypm mybuild

            FROM node:22-alpine AS runtime

            WORKDIR /app
            COPY --from=build /app /app

            ENV NODE_ENV=production
            EXPOSE 3000

            USER node

            ENTRYPOINT ["node","app.js"]

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);
    }

    [Fact]
    public void AddNodeApp_DoesNotAddNpmWhenNoPackageJson()
    {
        var tempDir = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDir.Path, "app.js"), "{}");

        var builder = DistributedApplication.CreateBuilder();

        builder.AddNodeApp("nodeapp", tempDir.Path, "app.js");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the JavaScriptApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        // Verify the package manager annotation
        Assert.False(nodeResource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out _));

        // Verify the install command annotation
        Assert.False(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out _));

        // Verify the JavaScriptInstallerResource resource does not exist
        Assert.Empty(appModel.Resources.OfType<JavaScriptInstallerResource>());
    }

    [Fact]
    public void AddNodeApp_AddsNpmWhenPackageJsonExists()
    {
        var tempDir = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDir.Path, "package.json"), "{}");

        var builder = DistributedApplication.CreateBuilder();

        builder.AddNodeApp("nodeapp", tempDir.Path, "app.js");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the JavaScriptApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        // Verify the package manager annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager));
        Assert.Equal("npm", packageManager.ExecutableName);
        Assert.Equal("run", packageManager.ScriptCommand);

        // Verify the install command annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installAnnotation));
        Assert.Equal(["install"], installAnnotation.Args);

        // Verify the JavaScriptInstallerResource resource exists
        var installerResources = Assert.Single(appModel.Resources.OfType<JavaScriptInstallerResource>());
        Assert.NotNull(installerResources);
    }

    [Fact]
    public async Task WithRunScript_SetsCustomRunCommand()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddNodeApp("nodeapp", ".", "app.js")
            .WithYarn()
            .WithRunScript("start", ["--my-arg1"]);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the JavaScriptApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        var args = await ArgumentEvaluator.GetArgumentListAsync(nodeResource);

        Assert.Collection(args,
            arg => Assert.Equal("run", arg),
            arg => Assert.Equal("start", arg),
            arg => Assert.Equal("--my-arg1", arg));
    }
}
