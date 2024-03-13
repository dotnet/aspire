// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ProjectResourceTests
{
    [Fact]
    public async Task AddProjectAddsEnvironmentVariablesAndServiceMetadata()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        Assert.Equal("projectName", resource.Name);
        Assert.Equal(7, resource.Annotations.Count);

        var serviceMetadata = Assert.Single(resource.Annotations.OfType<IProjectMetadata>());
        Assert.IsType<TestProject>(serviceMetadata);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

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
                Assert.Equal("service.instance.id={{- .Name -}}", env.Value);
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

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null)
            .WithReplicas(5);
        using var app = appBuilder.Build();

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

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: "http");
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        // LaunchProfileAnnotation isn't public, so we just check the type name
        Assert.Contains(resource.Annotations, a => a.GetType().Name == "LaunchProfileAnnotation");
    }

    [Fact]
    public void WithLaunchProfile_ApplicationUrlTrailingSemiColon_Ignore()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: "https");
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        Assert.Collection(
            resource.Annotations.OfType<EndpointAnnotation>(),
            a =>
            {
                Assert.Equal("https", a.Name);
                Assert.Equal("https", a.UriScheme);
                Assert.Equal(7123, a.Port);
            },
            a =>
            {
                Assert.Equal("http", a.Name);
                Assert.Equal("http", a.UriScheme);
                Assert.Equal(5156, a.Port);
            });
    }

    [Fact]
    public void AddProjectFailsIfFileDoesNotExist()
    {
        var appBuilder = CreateBuilder();

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddProject<TestProject>("projectName"));
        Assert.Equal("Project file 'another-path' was not found.", ex.Message);
    }

    [Fact]
    public void SpecificLaunchProfileFailsIfProfileDoesNotExist()
    {
        var appBuilder = CreateBuilder();

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: "not-exist"));
        Assert.Equal("Launch settings file does not contain 'not-exist' profile.", ex.Message);
    }

    [Fact]
    public void ExcludeLaunchProfileAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        
        Assert.Contains(resource.Annotations, a => a is ExcludeLaunchProfileAnnotation);
    }

    [Fact]
    public void DisabledForwadedHeadersAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<Projects.ServiceA>("projectName").DisableForwadedHeaders();
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        Assert.Contains(resource.Annotations, a => a is DisableForwardedHeadersAnnotation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VerifyManifest(bool disableForwardedHeaders)
    {
        var appBuilder = CreateBuilder();

        var project = appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName");
        if (disableForwardedHeaders)
        {
            project.DisableForwadedHeaders();
        }

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var manifest = await ManifestUtils.GetManifest(resource);

        var fordwardedHeadersEnvVar = disableForwardedHeaders
            ? ""
            : $",{Environment.NewLine}    \"ASPNETCORE_FORWARDEDHEADERS_ENABLED\": \"true\"";

        var expectedManifest = $$"""
            {
              "type": "project.v0",
              "path": "another-path",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true"{{fordwardedHeadersEnvVar}}
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http"
                },
                "https": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http"
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    private static IDistributedApplicationBuilder CreateBuilder(DistributedApplicationOperation operation = DistributedApplicationOperation.Publish)
    {
        var args = operation == DistributedApplicationOperation.Publish ? new[] { "--publisher", "manifest" } : Array.Empty<string>();
        var appBuilder = DistributedApplication.CreateBuilder(args);
        // Block DCP from actually starting anything up as we don't need it for this test.
        appBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, NoopPublisher>("manifest");

        return appBuilder;
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }

    private sealed class TestProjectWithLaunchSettings : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; } =
            new LaunchSettings
            {
                Profiles = new()
                {
                    ["http"] = new()
                    {
                        CommandName = "Project",
                        LaunchBrowser = true,
                        ApplicationUrl = "http://localhost:5031",
                        EnvironmentVariables = new()
                        {
                            ["ASPNETCORE_ENVIRONMENT"] = "Development"
                        }
                    }
                }
            };
    }
}
