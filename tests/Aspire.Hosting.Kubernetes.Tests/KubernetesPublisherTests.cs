// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Kubernetes.Tests;

public class KubernetesPublisherTests
{
    private readonly List<string> _expectedFiles =
    [
        "Chart.yaml",
        "values.yaml",
        "templates/project1/deployment.yaml",
        "templates/project1/configmap.yaml",
        "templates/myapp/deployment.yaml",
        "templates/myapp/service.yaml",
        "templates/myapp/configmap.yaml",
        "templates/myapp/secret.yaml",
    ];

    private readonly Dictionary<string, string> _expectedValuesContentCache = [];

    [Fact]
    public async Task PublishAsync_GeneratesValidHelmChart()
    {
        // Arrange
        LoadSnapshots();
        using var tempDirectory = new TempDirectory();
        var options = new OptionsMonitor(
            new()
            {
                OutputPath = tempDirectory.Path,
            });

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
            .WithVolume("logs", "/logs")
            .WithArgs("--cs", cs.Resource);

        builder.AddProject<TestProject>("project1", launchProfileName: null)
            .WithReference(api.GetEndpoint("http"));

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var publisher = new KubernetesPublisher(
            "test", options,
            NullLogger<KubernetesPublisher>.Instance,
            new(DistributedApplicationOperation.Publish));

        // Act
        await publisher.PublishAsync(model, CancellationToken.None);

        // Assert
        foreach (var expectedFile in _expectedFiles)
        {
            await AssertOutputFileContentsEqualExpectedFileContents(tempDirectory, expectedFile);
        }
    }

    private void LoadSnapshots()
    {
        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        foreach (var expectedFile in _expectedFiles)
        {
            using var stream = embeddedProvider.GetFileInfo($"ExpectedValues.{expectedFile.Replace('/', '.')}").CreateReadStream() ?? throw new FileNotFoundException($"Expected file not found: {expectedFile}");
            using var reader = new StreamReader(stream);
            _expectedValuesContentCache[expectedFile] = reader.ReadToEnd();
        }
    }

    private async Task AssertOutputFileContentsEqualExpectedFileContents(TempDirectory tempDirectory, string outputPath)
    {
        var file = Path.Combine(tempDirectory.Path, outputPath);
        Assert.True(File.Exists(file), $"File not found: {file}");
        var outputContent = await File.ReadAllTextAsync(file);
        Assert.Equal(_expectedValuesContentCache[outputPath], outputContent, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
    }

    private sealed class OptionsMonitor(KubernetesPublisherOptions options) : IOptionsMonitor<KubernetesPublisherOptions>
    {
        public KubernetesPublisherOptions Get(string? name) => options;

        public IDisposable OnChange(Action<KubernetesPublisherOptions, string> listener) => null!;

        public KubernetesPublisherOptions CurrentValue => options;
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory(".aspire-kubernetes").FullName;

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
