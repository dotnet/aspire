// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.NodeJs.Tests;

public class AddViteAppTests
{
    [Fact]
    public async Task VerifyDefaultDockerfile()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputPath: tempDir.Path).WithResourceCleanUp(true);

        // Create a vite directory in the builder's AppHostDirectory
        var viteDir = Path.Combine(builder.AppHostDirectory, "vite");
        Directory.CreateDirectory(viteDir);

        var nodeApp = builder.AddViteApp("vite", "vite")
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource, builder.AppHostDirectory);

        var expectedManifest = $$"""
            {
              "type": "container.v1",
              "build": {
                "context": "vite",
                "dockerfile": "vite.Dockerfile"
              },
              "env": {
                "NODE_ENV": "production",
                "PORT": "{vite.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8000
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        var dockerfilePath = Path.Combine(builder.AppHostDirectory, "vite.Dockerfile");
        var dockerfileContents = File.ReadAllText(dockerfilePath);
        var expectedDockerfile = $$"""
            FROM node:22-slim
            WORKDIR /app
            COPY . .
            RUN npm install
            RUN npm run build

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);
    }
}
