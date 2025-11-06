// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.JavaScript.Tests;

public class AddJavaScriptAppTests
{
    [Fact]
    public async Task VerifyDockerfile()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputPath: tempDir.Path).WithResourceCleanUp(true);

        // Create directory to ensure manifest generates correct relative build context path
        var appDir = Path.Combine(tempDir.Path, "js");
        Directory.CreateDirectory(appDir);

        var yarnApp = builder.AddJavaScriptApp("js", appDir)
            .WithYarn(installArgs: ["--immutable"])
            .WithBuildScript("do", ["--build"]);

        await ManifestUtils.GetManifest(yarnApp.Resource, tempDir.Path);

        var dockerfilePath = Path.Combine(tempDir.Path, "js.Dockerfile");
        var dockerfileContents = File.ReadAllText(dockerfilePath);
        var expectedDockerfile = $$"""
            FROM node:22-slim
            WORKDIR /app
            COPY . .
            RUN yarn install --immutable
            RUN yarn run do --build

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);

        var dockerBuildAnnotation = yarnApp.Resource.Annotations.OfType<DockerfileBuildAnnotation>().Single();
        Assert.False(dockerBuildAnnotation.HasEntrypoint);

        var containerFilesSource = yarnApp.Resource.Annotations.OfType<ContainerFilesSourceAnnotation>().Single();
        Assert.Equal("/app/dist", containerFilesSource.SourcePath);
    }

    [Fact]
    public async Task VerifyPnpmDockerfile()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputPath: tempDir.Path).WithResourceCleanUp(true);

        // Create directory to ensure manifest generates correct relative build context path
        var appDir = Path.Combine(tempDir.Path, "js");
        Directory.CreateDirectory(appDir);

        var pnpmApp = builder.AddJavaScriptApp("js", appDir)
            .WithPnpm(installArgs: ["--prefer-frozen-lockfile"])
            .WithBuildScript("mybuild");

        await ManifestUtils.GetManifest(pnpmApp.Resource, tempDir.Path);

        var dockerfilePath = Path.Combine(tempDir.Path, "js.Dockerfile");
        var dockerfileContents = File.ReadAllText(dockerfilePath);
        var expectedDockerfile = $$"""
            FROM node:22-slim
            WORKDIR /app
            COPY . .
            RUN pnpm install --prefer-frozen-lockfile
            RUN pnpm run mybuild

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);
    }
}
