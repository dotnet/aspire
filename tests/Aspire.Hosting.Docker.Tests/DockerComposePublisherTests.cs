// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Docker.Resources.ComposeNodes;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposePublisherTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishAsync_GeneratesValidDockerComposeFile()
    {
        using var tempDir = new TempDirectory();
        // Arrange

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        var param0 = builder.AddParameter("param0");
        var param1 = builder.AddParameter("param1", secret: true);
        var param2 = builder.AddParameter("param2", "default", publishValueAsDefault: true);
        var cs = builder.AddConnectionString("cs", ReferenceExpression.Create($"Url={param0}, Secret={param1}"));

        // Add a container to the application
        var redis = builder.AddContainer("cache", "redis")
                    .WithEntrypoint("/bin/sh")
                    .WithArgs("-c", "hello $MSG")
                    .WithEnvironment("MSG", "world");

        var migration = builder.AddContainer("something", "dummy/migration:latest")
                         .WithContainerName("cn");

        var api = builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
                         .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                         .WithHttpEndpoint(env: "PORT")
                         .WithEnvironment("param0", param0)
                         .WithEnvironment("param1", param1)
                         .WithEnvironment("param2", param2)
                         .WithReference(cs)
                         .WithArgs("--cs", cs.Resource)
                         .WaitFor(redis)
                         .WaitForCompletion(migration)
                         .WaitFor(param0);

        builder.AddProject(
            "project1",
            "..\\TestingAppHost1\\TestingAppHost1.MyWebApp\\TestingAppHost1.MyWebApp.csproj",
            launchProfileName: null)
            .WithReference(api.GetEndpoint("http"));

        var app = builder.Build();

        // Act
        app.Run();

        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        var envPath = Path.Combine(tempDir.Path, ".env");
        Assert.True(File.Exists(composePath));
        Assert.True(File.Exists(envPath));

        await Verify(File.ReadAllText(composePath), "yaml")
            .AppendContentAsFile(File.ReadAllText(envPath), "env")
            .UseHelixAwareDirectory();
    }

    [Fact]
    public async Task DockerComposeCorrectlyEmitsPortMappings()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path)
            .WithTestAndResourceLogging(outputHelper);

        builder.AddDockerComposeEnvironment("docker-compose");

        builder.AddContainer("resource", "mcr.microsoft.com/dotnet/aspnet:8.0")
               .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
               .WithHttpEndpoint(env: "HTTP_PORT");

        var app = builder.Build();

        await app.RunAsync().WaitAsync(TimeSpan.FromSeconds(60));

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        await Verify(File.ReadAllText(composePath), "yaml")
            .UseHelixAwareDirectory();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DockerComposeHandleImageBuilding(bool shouldBuildImages)
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(["--operation", "publish", "--publisher", "default", "--output-path", tempDir.Path])
            .WithTestAndResourceLogging(outputHelper);

        builder.AddDockerComposeEnvironment("docker-compose")
               .WithProperties(e => e.BuildContainerImages = shouldBuildImages);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddContainer("resource", "mcr.microsoft.com/dotnet/aspnet:8.0")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithHttpEndpoint(env: "HTTP_PORT");

        var app = builder.Build();

        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;

        Assert.NotNull(mockImageBuilder);

        // Act
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));
        Assert.Equal(shouldBuildImages, mockImageBuilder.BuildImageCalled);
    }

    [Fact]
    public async Task DockerComposeAppliesServiceCustomizations()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose")
               .WithProperties(e => e.DefaultNetworkName = "default-network")
               .ConfigureComposeFile(file =>
               {
                   file.AddNetwork(new Network { Name = "custom-network", Driver = "host" });

                   file.Name = "my application";
               });

        // Add a container to the application
        var container = builder.AddContainer("service", "nginx")
            .WithEnvironment("ORIGINAL_ENV", "value")
            .PublishAsDockerComposeService((serviceResource, composeService) =>
            {
                // Add a custom label
                composeService.Labels["custom-label"] = "test-value";

                // Add a custom environment variable
                composeService.AddEnvironmentalVariable("CUSTOM_ENV", "custom-value");

                // Set a restart policy
                composeService.Restart = "always";

                // Add a custom network
                composeService.Networks.Add("custom-network");
            });

        var app = builder.Build();

        app.Run();
        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        await Verify(File.ReadAllText(composePath), "yaml")
            .UseHelixAwareDirectory();
    }

    private sealed class MockImageBuilder : IResourceContainerImageBuilder
    {
        public bool BuildImageCalled { get; private set; }

        public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = Directory.CreateTempSubdirectory(".aspire-compose").FullName;
        }

        public string Path { get; }
        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
