// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Resources;
using Aspire.Hosting.Utils;
using YamlDotNet.Serialization;

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
                serviceResource.Workload!.PodTemplate.Spec.Containers[0].ImagePullPolicy = "Always";
                (serviceResource.Workload as Deployment)!.Spec.RevisionHistoryLimit = 5;
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
    public async Task PublishAsync_CustomWorkloadAndResourceType()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, "default", outputPath: tempDir.Path);

        builder.AddKubernetesEnvironment("env");

        // Add a container to the application
        var api = builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsKubernetesService(serviceResource => {
                serviceResource.Workload = new ArgoRollout
                {
                    Metadata = { Name = "myapp-rollout", Labels = serviceResource.Labels.ToDictionary() },
                    Spec = { Template = serviceResource.Workload!.PodTemplate, Selector = { MatchLabels = serviceResource.Labels.ToDictionary() } }
                };
                serviceResource.AdditionalResources.Add(new KedaScaledObject
                {
                    Metadata = { Name = "myapp-scaler" },
                    Spec = { ScaleTargetRef = { Kind = serviceResource.Workload.Kind!, Name = serviceResource.Workload.Metadata.Name }, MaxReplicaCount = 3 }
                });
            });

        builder.AddProject<TestProject>("project1", launchProfileName: null)
            .WithReference(api.GetEndpoint("http"));

        var app = builder.Build();

        app.Run();

        // Assert
        var expectedFiles = new[]
        {
            "Chart.yaml",
            "values.yaml",
            "templates/myapp/rollout.yaml",
            "templates/myapp/service.yaml",
            "templates/myapp/config.yaml",
            "templates/myapp/scaler.yaml"
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
    public async Task PublishAsync_HandlesSpecialResourceName()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, "default", outputPath: tempDir.Path);

        builder.AddKubernetesEnvironment("env")
                   .WithProperties(k => k.HelmChartName = "my-chart");

        var param0 = builder.AddParameter("param0");
        var param1 = builder.AddParameter("param1", secret: true);
        var cs = builder.AddConnectionString("api-cs", ReferenceExpression.Create($"Url={param0}, Secret={param1}"));
        var csPlain = builder.AddConnectionString("api-cs2", ReferenceExpression.Create($"host.local:80"));

        var param3 = builder.AddResource(ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, "param3"));
        builder.AddProject<TestProject>("SpeciaL-ApP", launchProfileName: null)
            .WithEnvironment("param3", param3)
            .WithReference(cs)
            .WithReference(csPlain);

        var app = builder.Build();

        app.Run();

        // Assert
        var expectedFiles = new[]
        {
            "Chart.yaml",
            "values.yaml",
            "templates/SpeciaL-ApP/deployment.yaml",
            "templates/SpeciaL-ApP/config.yaml",
            "templates/SpeciaL-ApP/secrets.yaml"
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

    private sealed class KedaScaledObject() : BaseKubernetesResource("keda.sh/v1alpha1", "ScaledObject")
    {
        [YamlMember(Alias = "spec")]
        public KedaScaledObjectSpec Spec { get; set; } = new();

        public sealed class KedaScaledObjectSpec
        {
            [YamlMember(Alias = "scaleTargetRef")]
            public ScaleTargetRefSpec ScaleTargetRef { get; set; } = new();

            [YamlMember(Alias = "minReplicaCount")]
            public int MinReplicaCount { get; set; } = 1;

            [YamlMember(Alias = "maxReplicaCount")]
            public int MaxReplicaCount { get; set; } = 1;

            public sealed class ScaleTargetRefSpec
            {
                [YamlMember(Alias = "name")]
                public string Name { get; set; } = null!;
                [YamlMember(Alias = "kind")]
                public string Kind { get; set; } = "Deployment";
            }

            // Omitted other properties for brevity
        }
    }

    private sealed class ArgoRollout() : Workload("argoproj.io/v1alpha1", "Rollout")
    {
        public ArgoRolloutSpec Spec { get; set; } = new();

        public sealed class ArgoRolloutSpec
        {
            [YamlMember(Alias = "replicas")]
            public int Replicas { get; set; } = 1;

            [YamlMember(Alias = "template")]
            public PodTemplateSpecV1 Template { get; set; } = new();

            [YamlMember(Alias = "selector")]
            public LabelSelectorV1 Selector { get; set; } = new();

            // Omitted other properties for brevity
        }

        [YamlIgnore]
        public override PodTemplateSpecV1 PodTemplate => Spec.Template;
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
