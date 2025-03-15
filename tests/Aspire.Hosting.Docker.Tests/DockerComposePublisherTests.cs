// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposePublisherTests
{
    [Fact]
    public async Task PublishAsync_GeneratesValidDockerComposeFile()
    {
        using var tempDir = new TempDirectory();
        // Arrange
        var options = new OptionsMonitor(new DockerComposePublisherOptions { OutputPath = tempDir.Path });
        var builder = DistributedApplication.CreateBuilder();

        var param0 = builder.AddParameter("param0");
        var param1 = builder.AddParameter("param1", secret: true);
        var param2 = builder.AddParameter("param2", "default", publishValueAsDefault: true);
        var cs = builder.AddConnectionString("cs", ReferenceExpression.Create($"Url={param0}, Secret={param1}"));

        // Add a container to the application
        var api = builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
                         .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                         .WithHttpEndpoint(env: "PORT")
                         .WithEnvironment("param0", param0)
                         .WithEnvironment("param1", param1)
                         .WithEnvironment("param2", param2)
                         .WithReference(cs)
                         .WithArgs("--cs", cs.Resource);

        builder.AddProject<TestProject>("project1", launchProfileName: null)
            .WithReference(api.GetEndpoint("http"));

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var publisher = new DockerComposePublisher("test", options,
            NullLogger<DockerComposePublisher>.Instance,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish));

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
            myapp:
                image: "mcr.microsoft.com/dotnet/aspnet:8.0"
                command:
                - "--cs"
                - "Url=${PARAM0}, Secret=${PARAM1}"
                environment:
                ASPNETCORE_ENVIRONMENT: "Development"
                PORT: "8001"
                param0: "${PARAM0}"
                param1: "${PARAM1}"
                param2: "${PARAM2}"
                ConnectionStrings__cs: "Url=${PARAM0}, Secret=${PARAM1}"
                ports:
                - "8001:8000"
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
