// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.NodeJs.Tests;

public class AddViteAppTests
{
    [Fact]
    public async Task VerifyDefaultDockerfile()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithResourceCleanUp(true);

        var workingDirectory = AppContext.BaseDirectory;
        var nodeApp = builder.AddViteApp("vite", "vite")
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v1",
              "build": {
                "context": "../../../../../tests/Aspire.Hosting.Tests/vite",
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

        var dockerfileContents = File.ReadAllText("vite.Dockerfile");
        var expectedDockerfile = $$"""
            FROM node:22-slim
            WORKDIR /app
            COPY . .
            RUN npm install
            RUN npm run build

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);
    }

    [Fact]
    public async Task VerifyDockerfileWithNodeVersionFromPackageJson()
    {
        using var tempDir = new TempDirectory();

        // Create a package.json with engines.node specification
        var packageJson = """
            {
              "name": "test-vite",
              "engines": {
                "node": ">=20.12"
              }
            }
            """;
        File.WriteAllText(Path.Combine(tempDir.Path, "package.json"), packageJson);

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithResourceCleanUp(true);
        var nodeApp = builder.AddViteApp("vite", tempDir.Path)
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var dockerfileContents = File.ReadAllText("vite.Dockerfile");
        
        try
        {
            // Should detect version 20 from package.json
            Assert.Contains("FROM node:20-slim", dockerfileContents);
        }
        finally
        {
            if (File.Exists("vite.Dockerfile"))
            {
                File.Delete("vite.Dockerfile");
            }
        }
    }

    [Fact]
    public async Task VerifyDockerfileWithNodeVersionFromNvmrc()
    {
        using var tempDir = new TempDirectory();

        // Create an .nvmrc file
        File.WriteAllText(Path.Combine(tempDir.Path, ".nvmrc"), "18.20.0");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithResourceCleanUp(true);
        var nodeApp = builder.AddViteApp("vite", tempDir.Path)
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var dockerfileContents = File.ReadAllText("vite.Dockerfile");
        
        try
        {
            // Should detect version 18 from .nvmrc
            Assert.Contains("FROM node:18-slim", dockerfileContents);
        }
        finally
        {
            if (File.Exists("vite.Dockerfile"))
            {
                File.Delete("vite.Dockerfile");
            }
        }
    }

    [Fact]
    public async Task VerifyDockerfileWithNodeVersionFromNodeVersion()
    {
        using var tempDir = new TempDirectory();

        // Create a .node-version file
        File.WriteAllText(Path.Combine(tempDir.Path, ".node-version"), "v21.5.0");

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithResourceCleanUp(true);
        var nodeApp = builder.AddViteApp("vite", tempDir.Path)
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var dockerfileContents = File.ReadAllText("vite.Dockerfile");
        
        try
        {
            // Should detect version 21 from .node-version
            Assert.Contains("FROM node:21-slim", dockerfileContents);
        }
        finally
        {
            if (File.Exists("vite.Dockerfile"))
            {
                File.Delete("vite.Dockerfile");
            }
        }
    }

    [Fact]
    public async Task VerifyDockerfileWithNodeVersionFromToolVersions()
    {
        using var tempDir = new TempDirectory();

        // Create a .tool-versions file
        var toolVersions = """
            ruby 3.2.0
            nodejs 19.8.1
            python 3.11.0
            """;
        File.WriteAllText(Path.Combine(tempDir.Path, ".tool-versions"), toolVersions);

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithResourceCleanUp(true);
        var nodeApp = builder.AddViteApp("vite", tempDir.Path)
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var dockerfileContents = File.ReadAllText("vite.Dockerfile");
        
        try
        {
            // Should detect version 19 from .tool-versions
            Assert.Contains("FROM node:19-slim", dockerfileContents);
        }
        finally
        {
            if (File.Exists("vite.Dockerfile"))
            {
                File.Delete("vite.Dockerfile");
            }
        }
    }

    [Fact]
    public async Task VerifyDockerfileDefaultsTo22WhenNoVersionFound()
    {
        using var tempDir = new TempDirectory();

        // Don't create any version files - should default to 22
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithResourceCleanUp(true);
        var nodeApp = builder.AddViteApp("vite", tempDir.Path)
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var dockerfileContents = File.ReadAllText("vite.Dockerfile");
        
        try
        {
            // Should default to version 22
            Assert.Contains("FROM node:22-slim", dockerfileContents);
        }
        finally
        {
            if (File.Exists("vite.Dockerfile"))
            {
                File.Delete("vite.Dockerfile");
            }
        }
    }

    [Theory]
    [InlineData("18", "node:18-slim")]
    [InlineData("v20.1.0", "node:20-slim")]
    [InlineData(">=18.12", "node:18-slim")]
    [InlineData("^16.0.0", "node:16-slim")]
    [InlineData("~19.5.0", "node:19-slim")]
    public async Task VerifyDockerfileHandlesVariousVersionFormats(string versionString, string expectedImage)
    {
        using var tempDir = new TempDirectory();

        File.WriteAllText(Path.Combine(tempDir.Path, ".nvmrc"), versionString);

        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).WithResourceCleanUp(true);
        var nodeApp = builder.AddViteApp("vite", tempDir.Path)
            .WithNpmPackageManager();

        var manifest = await ManifestUtils.GetManifest(nodeApp.Resource);

        var dockerfileContents = File.ReadAllText("vite.Dockerfile");
        
        try
        {
            Assert.Contains($"FROM {expectedImage}", dockerfileContents);
        }
        finally
        {
            if (File.Exists("vite.Dockerfile"))
            {
                File.Delete("vite.Dockerfile");
            }
        }
    }
}
