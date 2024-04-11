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
        // Explicitly specify development environment and other config so it is constant.
        var appBuilder = CreateBuilder(args: ["--environment", "Development", "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:18889"],
            DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        Assert.Equal("projectName", resource.Name);

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
                Assert.Equal("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", env.Key);
                Assert.Equal("in_memory", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_EXPORTER_OTLP_ENDPOINT", env.Key);
                Assert.Equal("http://localhost:18889", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_EXPORTER_OTLP_PROTOCOL", env.Key);
                Assert.Equal("grpc", env.Value);
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
                Assert.Equal("OTEL_EXPORTER_OTLP_HEADERS", env.Key);
                var parts = env.Value.Split('=');
                Assert.Equal("x-otlp-api-key", parts[0]);
                Assert.True(Guid.TryParse(parts[1], out _));
            },
            env =>
            {
                Assert.Equal("OTEL_BLRP_SCHEDULE_DELAY", env.Key);
                Assert.Equal("1000", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_BSP_SCHEDULE_DELAY", env.Key);
                Assert.Equal("1000", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_METRIC_EXPORT_INTERVAL", env.Key);
                Assert.Equal("1000", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_TRACES_SAMPLER", env.Key);
                Assert.Equal("always_on", env.Value);
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

    [Theory]
    [InlineData("true", false)]
    [InlineData("1", false)]
    [InlineData("false", true)]
    [InlineData("0", true)]
    [InlineData(null, true)]
    public async Task AddProjectAddsEnvironmentVariablesAndServiceMetadata_OtlpAuthDisabledSetting(string? value, bool hasHeader)
    {
        var appBuilder = CreateBuilder(args: [$"DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS={value}"], DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        Assert.Equal("projectName", resource.Name);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

        if (hasHeader)
        {
            Assert.True(config.ContainsKey("OTEL_EXPORTER_OTLP_HEADERS"), "Config should have 'OTEL_EXPORTER_OTLP_HEADERS' header and doesn't.");
        }
        else
        {
            Assert.False(config.ContainsKey("OTEL_EXPORTER_OTLP_HEADERS"), "Config shouldn't have 'OTEL_EXPORTER_OTLP_HEADERS' header and does.");
        }
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
        Assert.Contains(resource.Annotations, a => a is LaunchProfileAnnotation);
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
    public async Task AspNetCoreUrlsNotInjectedInPublishMode()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Publish);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null)
                  .WithHttpEndpoint(port: 5000, name: "http")
                  .WithHttpsEndpoint(port: 5001, name: "https");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

        Assert.False(config.ContainsKey("ASPNETCORE_URLS"));
        Assert.False(config.ContainsKey("ASPNETCORE_HTTPS_PORT"));
    }

    [Fact]
    public async Task ExcludeLaunchProfileAddsHttpOrHttpsEndpointAddsToEnv()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null)
                  .WithHttpEndpoint(port: 5000, name: "http")
                  .WithHttpsEndpoint(port: 5001, name: "https")
                  .WithHttpEndpoint(port: 5002, name: "http2", env: "SOME_ENV")
                  .WithEndpoint("http", e =>
                  {
                      e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p0");
                  })
                  .WithEndpoint("https", e =>
                  {
                      e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p1");
                  })
                  .WithEndpoint("http2", e =>
                   {
                       e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p2");
                   });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

        Assert.Equal("http://localhost:p0;https://localhost:p1", config["ASPNETCORE_URLS"]);
        Assert.Equal("5001", config["ASPNETCORE_HTTPS_PORT"]);
        Assert.Equal("p2", config["SOME_ENV"]);
    }

    [Fact]
    public async Task NoEndpointsDoesNotAddAspNetCoreUrls()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

        Assert.False(config.ContainsKey("ASPNETCORE_URLS"));
        Assert.False(config.ContainsKey("ASPNETCORE_HTTPS_PORT"));
    }

    [Fact]
    public async Task ProjectWithLaunchProfileAddsHttpOrHttpsEndpointAddsToEnv()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName")
                  .WithEndpoint("http", e =>
                  {
                      e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p0");
                  });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

        Assert.Equal("http://localhost:p0", config["ASPNETCORE_URLS"]);
        Assert.False(config.ContainsKey("ASPNETCORE_HTTPS_PORT"));
    }

    [Fact]
    public void DisabledForwardedHeadersAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<Projects.ServiceA>("projectName").DisableForwardedHeaders();
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
            project.DisableForwardedHeaders();
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
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory"{{fordwardedHeadersEnvVar}}
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

    [Fact]
    public async Task VerifyManifestWithArgs()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName")
            .WithArgs("one", "two");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var manifest = await ManifestUtils.GetManifest(resource);

        var expectedManifest = $$"""
            {
              "type": "project.v0",
              "path": "another-path",
              "args": [
                "one",
                "two"
              ],
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true"
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

    [Fact]
    public async Task AddProjectWithArgs()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var c1 = appBuilder.AddContainer("c1", "image2")
            .WithEndpoint("ep", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 1234);
            });

        var project = appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName")
             .WithArgs(context =>
             {
                 context.Args.Add("arg1");
                 context.Args.Add(c1.GetEndpoint("ep"));
             });

        using var app = appBuilder.Build();

        var args = await ArgumentEvaluator.GetArgumentListAsync(project.Resource);

        Assert.Collection(args,
            arg => Assert.Equal("arg1", arg),
            arg => Assert.Equal("http://localhost:1234", arg));
    }

    private static IDistributedApplicationBuilder CreateBuilder(string[]? args = null, DistributedApplicationOperation operation = DistributedApplicationOperation.Publish)
    {
        var resolvedArgs = new List<string>();
        if (args != null)
        {
            resolvedArgs.AddRange(args);
        }
        if (operation == DistributedApplicationOperation.Publish)
        {
            resolvedArgs.AddRange(["--publisher", "manifest"]);
        }
        var appBuilder = DistributedApplication.CreateBuilder(resolvedArgs.ToArray());
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
                        CommandLineArgs = "arg1 arg2",
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
