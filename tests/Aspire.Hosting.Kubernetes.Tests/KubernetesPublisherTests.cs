// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Kubernetes.Tests;

[Collection(KubernetesPublisherFixture.CollectionName)]
public class KubernetesPublisherTests(KubernetesPublisherFixture fixture)
{
    private static bool s_publisherHasRun;

    private static readonly Dictionary<string, string> s_expectedFilesCache = new()
    {
        ["Chart.yaml"] = ExpectedValues.Chart,
        ["values.yaml"] = ExpectedValues.Values,
        ["templates/project1/deployment.yaml"] = ExpectedValues.ProjectOneDeployment,
        ["templates/project1/configmap.yaml"] = ExpectedValues.ProjectOneConfigMap,
        ["templates/myapp/deployment.yaml"] = ExpectedValues.MyAppDeployment,
        ["templates/myapp/service.yaml"] = ExpectedValues.MyAppService,
        ["templates/myapp/configmap.yaml"] = ExpectedValues.MyAppConfigMap,
        ["templates/myapp/secret.yaml"] = ExpectedValues.MyAppSecret,
    };

    public static TheoryData<string> GetExpectedFiles() => new(s_expectedFilesCache.Keys);

    [Theory, MemberData(nameof(GetExpectedFiles))]
    public async Task PublishAsync_GeneratesValidHelmChart(string expectedFile)
    {
        if (!s_publisherHasRun)
        {
            // Arrange
            ArgumentNullException.ThrowIfNull(fixture.TempDirectoryInstance);
            var options = new OptionsMonitor(
                new()
                {
                    OutputPath = fixture.TempDirectoryInstance.Path,
                });

            var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

            var param0 = builder.AddParameter("param0");
            var param1 = builder.AddParameter("param1", secret: true);
            var param2 = builder.AddParameter("param2", "default", publishValueAsDefault: true);
            var cs = builder.AddConnectionString("cs", ReferenceExpression.Create($"Url={param0}, Secret={param1}"));

            // Add a container to the application
            var api = builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                .WithHttpEndpoint(targetPort: 8080)
                .WithEnvironment("param0", param0)
                .WithEnvironment("param1", param1)
                .WithEnvironment("param2", param2)
                .WithReference(cs)
                .WithVolume("logs", "/logs")
                .WithArgs("--cs", cs.Resource);

            builder.AddProject<TestProject>("project1", launchProfileName: null)
                .WithReference(api.GetEndpoint("http"));

            var app = builder.Build();

            await ExecuteBeforeStartHooksAsync(app, default);

            var model = app.Services.GetRequiredService<DistributedApplicationModel>();

            await ExecuteBeforeStartHooksAsync(app, default);

            var publisher = new KubernetesPublisher(
                "test", options,
                NullLogger<KubernetesPublisher>.Instance,
                builder.ExecutionContext);

            // Act
            await publisher.PublishAsync(model, CancellationToken.None);
            s_publisherHasRun = true;
        }

        ArgumentNullException.ThrowIfNull(fixture.TempDirectoryInstance);

        // Assert
        var file = Path.Combine(fixture.TempDirectoryInstance.Path, expectedFile);
        Assert.True(File.Exists(file), $"File not found: {file}");
        var outputContent = await File.ReadAllTextAsync(file);
        Assert.Equal(s_expectedFilesCache[expectedFile], outputContent, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

    private sealed class OptionsMonitor(KubernetesPublisherOptions options) : IOptionsMonitor<KubernetesPublisherOptions>
    {
        public KubernetesPublisherOptions Get(string? name) => options;

        public IDisposable OnChange(Action<KubernetesPublisherOptions, string> listener) => null!;

        public KubernetesPublisherOptions CurrentValue => options;
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
