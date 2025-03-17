// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Kubernetes.Tests;

public class KubernetesPublisherTests
{
    [Fact]
    public async Task PublishAsync_GeneratesValidHelmChart()
    {
        using var tempDir = new TempDirectory();
        // Arrange
        var options = new OptionsMonitor(new KubernetesPublisherOptions() { OutputPath = tempDir.Path });
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

        var publisher = new KubernetesPublisher("test", options,
            NullLogger<KubernetesPublisher>.Instance,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish));

        // Act
        var act = publisher.PublishAsync(model, CancellationToken.None);

        // Assert
        // TODO: implement once the publisher is implemented.
        await Assert.ThrowsAsync<NotImplementedException>(() => act);
    }

    private sealed class OptionsMonitor(KubernetesPublisherOptions options) : IOptionsMonitor<KubernetesPublisherOptions>
    {
        public KubernetesPublisherOptions Get(string? name) => options;

        public IDisposable OnChange(Action<KubernetesPublisherOptions, string> listener) => null!;

        public KubernetesPublisherOptions CurrentValue => options;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = Directory.CreateTempSubdirectory(".aspire-kubernetes").FullName;
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
