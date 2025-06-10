// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Resources;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Kubernetes.Tests;

public class KubernetesPublisherTests()
{
    [Fact]
    public async Task PublishAsync_GeneratesValidHelmChart()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, "default", outputPath: tempDir.Path);

        builder.AddKubernetesEnvironment("env");

        var param0 = builder.AddParameter("param0");
        var param1 = builder.AddParameter("param1", secret: true);
        var param2 = builder.AddParameter("param2", "default", publishValueAsDefault: true);
        var param3 = builder.AddResource(ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, "param3"));
        var cs = builder.AddConnectionString("cs", ReferenceExpression.Create($"Url={param0}, Secret={param1}"));

        // Add a container to the application
        var api = builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithHttpEndpoint(targetPort: 8080)
            .WithEnvironment("param0", param0)
            .WithEnvironment("param1", param1)
            .WithEnvironment("param2", param2)
            .WithEnvironment("param3", param3)
            .WithReference(cs)
            .WithVolume("logs", "/logs")
            .WithArgs("--cs", cs.Resource);

        builder.AddProject<TestProject>("project1", launchProfileName: null)
            .WithReference(api.GetEndpoint("http"));

        var app = builder.Build();

        app.Run();

        // Assert
        var expectedFiles = new[]
        {
            "Chart.yaml",
            "values.yaml",
            "templates/project1/deployment.yaml",
            "templates/project1/config.yaml",
            "templates/myapp/deployment.yaml",
            "templates/myapp/service.yaml",
            "templates/myapp/config.yaml",
            "templates/myapp/secrets.yaml"
        };

        SettingsTask settingsTask = default!;

        foreach (var expectedFile in expectedFiles)
        {
            var filePath = Path.Combine(tempDir.Path, expectedFile);
            var fileExtension = Path.GetExtension(filePath)[1..];

            if (settingsTask is null)
            {
                settingsTask = Verify(File.ReadAllText(filePath), fileExtension);
            }
            else
            {
                settingsTask = settingsTask.AppendContentAsFile(File.ReadAllText(filePath), fileExtension);
            }
        }

        await settingsTask;
    }

    [Fact]
    public async Task PublishAppliesServiceCustomizations()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);

        builder.AddKubernetesEnvironment("env")
            .WithProperties(e => e.DefaultImagePullPolicy = "Always");

        // Add a container to the application
        var container = builder.AddContainer("service", "nginx")
            .WithEnvironment("ORIGINAL_ENV", "value")
            .PublishAsKubernetesService(serviceResource =>
            {
                serviceResource.Deployment!.Spec.RevisionHistoryLimit = 5;
            });

        var app = builder.Build();

        app.Run();

        // Assert
        var deploymentPath = Path.Combine(tempDir.Path, "templates/service/deployment.yaml");
        Assert.True(File.Exists(deploymentPath));

        var content = await File.ReadAllTextAsync(deploymentPath);

        await Verify(content, "yaml");
    }

    [Fact]
    public async Task PublishAsync_GeneratesAdditionalResourcesInChart()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, "default", outputPath: tempDir.Path);

        builder.AddKubernetesEnvironment("env");

        // Add a container to the application
        var api = builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsKubernetesService(resource => {
                resource.Resources.Add(new Secret { Metadata = { Name = "mycustomresource"} });
        });

        builder.AddProject<TestProject>("project1", launchProfileName: null)
            .WithReference(api.GetEndpoint("http"));

        var app = builder.Build();

        app.Run();

        // Assert
        var expectedFiles = new[]
        {
            "templates/myapp/mycustomresource.yaml"
        };

        SettingsTask settingsTask = default!;

        foreach (var expectedFile in expectedFiles)
        {
            var filePath = Path.Combine(tempDir.Path, expectedFile);
            var fileExtension = Path.GetExtension(filePath)[1..];

            if (settingsTask is null)
            {
                settingsTask = Verify(File.ReadAllText(filePath), fileExtension);
            }
            else
            {
                settingsTask = settingsTask.AppendContentAsFile(File.ReadAllText(filePath), fileExtension);
            }
        }

        await settingsTask;
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
