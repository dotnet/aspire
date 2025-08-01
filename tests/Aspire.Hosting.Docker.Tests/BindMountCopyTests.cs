// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Docker.Tests;

public class BindMountCopyTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishAsync_WithBindMounts_CopiesSourceFilesToOutputFolder()
    {
        using var tempDir = new TempDirectory();
        
        // Create source files for bind mount
        var sourceDir = Path.Combine(tempDir.Path, "source");
        Directory.CreateDirectory(sourceDir);
        
        var configFile = Path.Combine(sourceDir, "config.json");
        await File.WriteAllTextAsync(configFile, """{"key": "value"}""");
        
        var scriptFile = Path.Combine(sourceDir, "script.sh");
        await File.WriteAllTextAsync(scriptFile, """
#!/bin/bash
echo "Hello World"
""");
        
        // Create output directory
        var outputDir = Path.Combine(tempDir.Path, "output");
        Directory.CreateDirectory(outputDir);
        
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: outputDir);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container with bind mount
        builder.AddContainer("test-container", "busybox")
               .WithBindMount(sourceDir, "/app/config");

        var app = builder.Build();

        // Act
        app.Run();

        // Assert
        var composePath = Path.Combine(outputDir, "docker-compose.yaml");
        Assert.True(File.Exists(composePath), "docker-compose.yaml should be generated");

        var composeContent = await File.ReadAllTextAsync(composePath);
        outputHelper.WriteLine("Generated docker-compose.yaml:");
        outputHelper.WriteLine(composeContent);

        // Check if bind mount source files are copied to output directory
        var containerOutputDir = Path.Combine(outputDir, "test-container");
        Assert.True(Directory.Exists(containerOutputDir), "Container-specific directory should be created in output");
        
        var copiedConfigFile = Path.Combine(containerOutputDir, "config.json");
        var copiedScriptFile = Path.Combine(containerOutputDir, "script.sh");
        
        Assert.True(File.Exists(copiedConfigFile), "config.json should be copied to output directory");
        Assert.True(File.Exists(copiedScriptFile), "script.sh should be copied to output directory");
        
        // Check that the content is preserved
        var copiedConfigContent = await File.ReadAllTextAsync(copiedConfigFile);
        var copiedScriptContent = await File.ReadAllTextAsync(copiedScriptFile);
        
        Assert.Equal("""{"key": "value"}""", copiedConfigContent);
        Assert.Contains("Hello World", copiedScriptContent);

        // Check that docker-compose.yaml uses relative paths for bind mounts
        Assert.Contains("./test-container", composeContent);
        Assert.DoesNotContain(sourceDir, composeContent); // Should not contain absolute source path
    }

    [Fact]
    public async Task PublishAsync_WithBindMountFile_CopiesFileToOutputFolder()
    {
        using var tempDir = new TempDirectory();
        
        // Create a single source file for bind mount
        var sourceFile = Path.Combine(tempDir.Path, "app.config");
        await File.WriteAllTextAsync(sourceFile, """<configuration><setting>value</setting></configuration>""");
        
        // Create output directory
        var outputDir = Path.Combine(tempDir.Path, "output");
        Directory.CreateDirectory(outputDir);
        
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: outputDir);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container with bind mount to a single file
        builder.AddContainer("test-container", "busybox")
               .WithBindMount(sourceFile, "/app/app.config");

        var app = builder.Build();

        // Act
        app.Run();

        // Assert
        var composePath = Path.Combine(outputDir, "docker-compose.yaml");
        Assert.True(File.Exists(composePath), "docker-compose.yaml should be generated");

        var composeContent = await File.ReadAllTextAsync(composePath);
        outputHelper.WriteLine("Generated docker-compose.yaml:");
        outputHelper.WriteLine(composeContent);

        // Check if bind mount source file is copied to output directory
        var containerOutputDir = Path.Combine(outputDir, "test-container");
        Assert.True(Directory.Exists(containerOutputDir), "Container-specific directory should be created in output");
        
        var copiedFile = Path.Combine(containerOutputDir, "app.config");
        Assert.True(File.Exists(copiedFile), "app.config should be copied to output directory");
        
        // Check that the content is preserved
        var copiedContent = await File.ReadAllTextAsync(copiedFile);
        Assert.Equal("""<configuration><setting>value</setting></configuration>""", copiedContent);

        // Check that docker-compose.yaml uses relative paths for bind mounts
        Assert.Contains("./test-container/app.config", composeContent);
        Assert.DoesNotContain(sourceFile, composeContent); // Should not contain absolute source path
    }

    [Fact]
    public async Task PublishAsync_WithBindMountDirectory_UpdatesDockerComposeFileWithRelativePaths()
    {
        using var tempDir = new TempDirectory();
        
        // Create source files for bind mount
        var sourceDir = Path.Combine(tempDir.Path, "source");
        Directory.CreateDirectory(sourceDir);
        
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "config.json"), """{"key": "value"}""");
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "script.sh"), """
#!/bin/bash
echo "Hello World"
""");
        
        // Create output directory
        var outputDir = Path.Combine(tempDir.Path, "output");
        Directory.CreateDirectory(outputDir);
        
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: outputDir);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container with bind mount
        builder.AddContainer("test-container", "busybox")
               .WithBindMount(sourceDir, "/app/config");

        var app = builder.Build();

        // Act
        app.Run();

        // Assert
        var composePath = Path.Combine(outputDir, "docker-compose.yaml");
        Assert.True(File.Exists(composePath), "docker-compose.yaml should be generated");

        var composeContent = await File.ReadAllTextAsync(composePath);
        
        // Check if bind mount source files are copied to output directory
        var containerOutputDir = Path.Combine(outputDir, "test-container");
        Assert.True(Directory.Exists(containerOutputDir), "Container-specific directory should be created in output");
        
        var copiedConfigFile = Path.Combine(containerOutputDir, "config.json");
        var copiedScriptFile = Path.Combine(containerOutputDir, "script.sh");
        
        Assert.True(File.Exists(copiedConfigFile), "config.json should be copied to output directory");
        Assert.True(File.Exists(copiedScriptFile), "script.sh should be copied to output directory");
        
        // Check that the content is preserved
        var copiedConfigContent = await File.ReadAllTextAsync(copiedConfigFile);
        var copiedScriptContent = await File.ReadAllTextAsync(copiedScriptFile);
        
        Assert.Equal("""{"key": "value"}""", copiedConfigContent);
        Assert.Contains("Hello World", copiedScriptContent);

        // Check that docker-compose.yaml uses relative paths for bind mounts
        Assert.Contains("./test-container", composeContent);
        Assert.DoesNotContain(sourceDir, composeContent); // Should not contain absolute source path

        await Verify(composeContent, "yaml");
    }

    private sealed class MockImageBuilder : IResourceContainerImageBuilder
    {
        public Task BuildImageAsync(IResource resource, ContainerBuildOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task BuildImagesAsync(IEnumerable<IResource> resources, ContainerBuildOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}