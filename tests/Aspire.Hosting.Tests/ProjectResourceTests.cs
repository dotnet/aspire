// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ProjectResourceTests
{
    [Fact]
    public void AddProjectAddsEnvironmentVariablesAndServiceMetadata()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<TestProject>("projectName");
        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        Assert.Equal("projectName", resource.Name);
        Assert.Equal(5, resource.Annotations.Count);

        var serviceMetadata = Assert.Single(resource.Annotations.OfType<IServiceMetadata>());
        Assert.IsType<TestProject>(serviceMetadata);

        var annotations = resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_EXPORTER_OTLP_ENDPOINT", env.Key);
                Assert.Equal("http://localhost:18889", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_RESOURCE_ATTRIBUTES", env.Key);
                Assert.Equal("service.instance.id={{- .UID -}}", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_SERVICE_NAME", env.Key);
                Assert.Equal("{{- index .Annotations \"otel-service-name\" -}}", env.Value);
            },
            env =>
            {
                Assert.Equal("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("LOGGING__CONSOLE__FORMATTERNAME", env.Key);
                Assert.Equal("simple", env.Value);
            },
            env =>
            {
                Assert.Equal("LOGGING__CONSOLE__FORMATTEROPTIONS__TIMESTAMPFORMAT", env.Key);
                Assert.Equal("yyyy-MM-ddTHH:mm:ss.fffffff ", env.Value);
            });
    }

    [Fact]
    public void WithReplicasAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<TestProject>("projectName")
            .WithReplicas(5);
        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        var replica = Assert.Single(resource.Annotations.OfType<ReplicaAnnotation>());

        Assert.Equal(5, replica.Replicas);
    }

    [Fact]
    public void WithLaunchProfileAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<Projects.ServiceA>("projectName")
            .WithLaunchProfile("http");
        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        // LaunchProfileAnnotation isn't public, so we just check the type name
        Assert.Contains(resource.Annotations, a => a.GetType().Name == "LaunchProfileAnnotation");
    }

    [Fact]
    public void WithLaunchProfileFailsIfProfileDoesNotExist()
    {
        var appBuilder = CreateBuilder();

        var project = appBuilder.AddProject<Projects.ServiceA>("projectName");
        var ex = Assert.Throws<DistributedApplicationException>(() => project.WithLaunchProfile("not-exist"));
        Assert.Equal("Launch settings file does not contain 'not-exist' profile.", ex.Message);
    }

    [Fact]
    public void WithLaunchProfileFailsIfFileDoesNotExist()
    {
        var appBuilder = CreateBuilder();

        var project = appBuilder.AddProject<TestProject>("projectName");
        var ex = Assert.Throws<DistributedApplicationException>(() => project.WithLaunchProfile("not-exist"));
        Assert.Equal("Project file 'another-path' was not found.", ex.Message);
    }

    [Fact]
    public void ProjectWithoutServiceMetadataFailsWithLaunchProfile()
    {
        var appBuilder = CreateBuilder();

        var project = new ProjectResource("projectName");
        var projectResource = appBuilder.AddResource(project);

        var ex = Assert.Throws<DistributedApplicationException>(() => projectResource.WithLaunchProfile("not-exist"));
        Assert.Equal("Project does not contain service metadata.", ex.Message);
    }

    private static IDistributedApplicationBuilder CreateBuilder()
    {
        var appBuilder = DistributedApplication.CreateBuilder(["--publisher", "manifest"]);
        // Block DCP from actually starting anything up as we don't need it for this test.
        appBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, NoopPublisher>("manifest");

        return appBuilder;
    }

    private sealed class TestProject : IServiceMetadata
    {
        public string ProjectPath => "another-path";
    }
}
