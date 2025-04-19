// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposePublisherTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishAsync_GeneratesValidDockerComposeFile()
    {
        using var tempDir = new TempDirectory();
        // Arrange
        var options = new OptionsMonitor(new DockerComposePublisherOptions { OutputPath = tempDir.Path });
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

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

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var publisher = new DockerComposePublisher("test",
            options,
            NullLogger<DockerComposePublisher>.Instance,
            builder.ExecutionContext,
            new MockImageBuilder()
            );

        // Act
        await publisher.PublishAsync(model, default);

        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        var envPath = Path.Combine(tempDir.Path, ".env");
        Assert.True(File.Exists(composePath));
        Assert.True(File.Exists(envPath));

        var content = await File.ReadAllTextAsync(composePath);
        var envContent = await File.ReadAllTextAsync(envPath);

        Assert.Equal(
            """
            services:
              cache:
                image: "redis:latest"
                command:
                  - "-c"
                  - "hello $$MSG"
                entrypoint:
                  - "/bin/sh"
                environment:
                  MSG: "world"
                networks:
                  - "aspire"
              something:
                image: "dummy/migration:latest"
                container_name: "cn"
                networks:
                  - "aspire"
              myapp:
                image: "mcr.microsoft.com/dotnet/aspnet:8.0"
                command:
                  - "--cs"
                  - "Url=${PARAM0}, Secret=${PARAM1}"
                environment:
                  ASPNETCORE_ENVIRONMENT: "Development"
                  PORT: "8000"
                  param0: "${PARAM0}"
                  param1: "${PARAM1}"
                  param2: "${PARAM2}"
                  ConnectionStrings__cs: "Url=${PARAM0}, Secret=${PARAM1}"
                ports:
                  - "8001:8000"
                depends_on:
                  cache:
                    condition: "service_started"
                  something:
                    condition: "service_completed_successfully"
                networks:
                  - "aspire"
              project1:
                image: "${PROJECT1_IMAGE}"
                environment:
                  OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES: "true"
                  OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES: "true"
                  OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY: "in_memory"
                  services__myapp__http__0: "http://myapp:8000"
                networks:
                  - "aspire"
            networks:
              aspire:
                driver: "bridge"

            """,
            content, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);

        Assert.Equal(
            """
            # Parameter param0
            PARAM0=

            # Parameter param1
            PARAM1=

            # Parameter param2
            PARAM2=default

            # Container image name for project1
            PROJECT1_IMAGE=project1:latest


            """,
            envContent, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task PublishAsync_ThrowsWhenDockerComposeEnvironmentNotAdded()
    {
        using var tempDir = new TempDirectory();
        // Arrange
        var options = new OptionsMonitor(new DockerComposePublisherOptions { OutputPath = tempDir.Path });
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Add a simple container resource
        builder.AddContainer("cache", "redis");

        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var publisher = new DockerComposePublisher("test",
            options,
            NullLogger<DockerComposePublisher>.Instance,
            builder.ExecutionContext,
            new MockImageBuilder()
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(model, default)
        );

        Assert.Contains("No Docker Compose environment found. Ensure a Docker Compose environment is registered by calling AddDockerComposeEnvironment.", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DockerComposeCorrectlyEmitsPortMappings()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(["--operation", "publish", "--publisher", "docker-compose", "--output-path", tempDir.Path])
                                                             .WithTestAndResourceLogging(outputHelper);

        builder.AddDockerComposeEnvironment("docker-compose");
        builder.AddDockerComposePublisher();

        builder.AddContainer("resource", "mcr.microsoft.com/dotnet/aspnet:8.0")
               .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
               .WithHttpEndpoint(env: "HTTP_PORT");

        var app = builder.Build();

        await app.RunAsync().WaitAsync(TimeSpan.FromSeconds(60));

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var content = await File.ReadAllTextAsync(composePath);

        Assert.Equal(
            """
            services:
              resource:
                image: "mcr.microsoft.com/dotnet/aspnet:8.0"
                environment:
                  ASPNETCORE_ENVIRONMENT: "Development"
                  HTTP_PORT: "8000"
                ports:
                  - "8001:8000"
                networks:
                  - "aspire"
            networks:
              aspire:
                driver: "bridge"

            """,
            content, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DockerComposeHandleImageBuilding(bool shouldBuildImages)
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(["--operation", "publish", "--publisher", "docker-compose", "--output-path", tempDir.Path])
            .WithTestAndResourceLogging(outputHelper);

        builder.AddDockerComposeEnvironment("docker-compose");

        var options = new OptionsMonitor(new DockerComposePublisherOptions
        {
            OutputPath = tempDir.Path,
            BuildImages = shouldBuildImages,
        });

        var mockImageBuilder = new MockImageBuilder();

        builder.AddDockerComposePublisher();

        builder.AddContainer("resource", "mcr.microsoft.com/dotnet/aspnet:8.0")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithHttpEndpoint(env: "HTTP_PORT");

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, CancellationToken.None);

        var publisher = new DockerComposePublisher("test",
            options,
            NullLogger<DockerComposePublisher>.Instance,
            builder.ExecutionContext,
            mockImageBuilder
        );

        // Act
        await publisher.PublishAsync(model, CancellationToken.None);

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));
        Assert.Equal(shouldBuildImages, mockImageBuilder.BuildImageCalled);
    }

    [Fact]
    public async Task DockerComposeAppliesServiceCustomizations()
    {
        using var tempDir = new TempDirectory();
        var options = new OptionsMonitor(new DockerComposePublisherOptions { OutputPath = tempDir.Path });
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddDockerComposeEnvironment("docker-compose")
               .WithProperties(e => e.DefaultNetworkName = "default-network");

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

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var publisher = new DockerComposePublisher("test",
            options,
            NullLogger<DockerComposePublisher>.Instance,
            builder.ExecutionContext,
            new MockImageBuilder()
        );

        // Act
        await publisher.PublishAsync(model, default);

        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var content = await File.ReadAllTextAsync(composePath);

        Assert.Equal(
            """
            services:
              service:
                image: "nginx:latest"
                environment:
                  ORIGINAL_ENV: "value"
                  CUSTOM_ENV: "custom-value"
                networks:
                  - "default-network"
                  - "custom-network"
                restart: "always"
                labels:
                  custom-label: "test-value"
            networks:
              default-network:
                driver: "bridge"

            """,
            content, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

    private sealed class MockImageBuilder : IResourceContainerImageBuilder
    {
        public bool BuildImageCalled { get; private set; }

        public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class OptionsMonitor(DockerComposePublisherOptions options) : IOptionsMonitor<DockerComposePublisherOptions>
    {
        public DockerComposePublisherOptions Get(string? name) => options;

        public IDisposable OnChange(Action<DockerComposePublisherOptions, string> listener) => null!;

        public DockerComposePublisherOptions CurrentValue => options;
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
