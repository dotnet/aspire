// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class ProjectResourceTests
{
    [Fact]
    public async Task AddProjectWithInvalidLaunchSettingsShouldThrowSpecificError()
    {
        var projectDetails = await PrepareProjectWithMalformedLaunchSettingsAsync().DefaultTimeout();

        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var appBuilder = CreateBuilder();
            appBuilder.AddProject("project", projectDetails.ProjectFilePath);
        });

        var expectedMessage = $"Failed to get effective launch profile for project resource 'project'. There is malformed JSON in the project's launch settings file at '{projectDetails.LaunchSettingsFilePath}'.";
        Assert.Equal(expectedMessage, ex.Message);

        async static Task<(string ProjectFilePath, string LaunchSettingsFilePath)> PrepareProjectWithMalformedLaunchSettingsAsync()
        {
            var csProjContent = """
                                <Project Sdk="Microsoft.NET.Sdk.Web">
                                <!-- Not a real project, just a stub for testing -->
                                </Project>
                                """;

            var launchSettingsContent = """
                                        this { is } { mal formed! >
                                        """;

            var projectDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var projectFilePath = Path.Combine(projectDirectoryPath, "Project.csproj");
            var propertiesDirectoryPath = Path.Combine(projectDirectoryPath, "Properties");
            var launchSettingsFilePath = Path.Combine(propertiesDirectoryPath, "launchSettings.json");

            Directory.CreateDirectory(projectDirectoryPath);
            await File.WriteAllTextAsync(projectFilePath, csProjContent).DefaultTimeout();

            Directory.CreateDirectory(propertiesDirectoryPath);
            await File.WriteAllTextAsync(launchSettingsFilePath, launchSettingsContent).DefaultTimeout();

            return (projectFilePath, launchSettingsFilePath);
        }
    }

    [Theory]
    [InlineData(KnownConfigNames.DashboardOtlpGrpcEndpointUrl)]
    [InlineData(KnownConfigNames.Legacy.DashboardOtlpGrpcEndpointUrl)]
    public async Task AddProjectAddsEnvironmentVariablesAndServiceMetadata(string dashboardOtlpGrpcEndpointUrlKey)
    {
        // Explicitly specify development environment and other config so it is constant.
        var appBuilder = CreateBuilder(args: ["--environment", "Development", $"{dashboardOtlpGrpcEndpointUrlKey}=http://localhost:18889"],
            DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        Assert.Equal("projectName", resource.Name);

        var serviceMetadata = Assert.Single(resource.Annotations.OfType<IProjectMetadata>());
        Assert.IsType<TestProject>(serviceMetadata);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

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
                Assert.Equal("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", env.Key);
                Assert.Equal("true", env.Value);
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
                Assert.Equal("service.instance.id={{- index .Annotations \"otel-service-instance-id\" -}}", env.Value);
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
                Assert.Equal("OTEL_METRICS_EXEMPLAR_FILTER", env.Key);
                Assert.Equal("trace_based", env.Value);
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
        var appBuilder = CreateBuilder(args: [$"{KnownConfigNames.DashboardUnsecuredAllowAnonymous}={value}"], DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);
        Assert.Equal("projectName", resource.Name);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

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
    [UseDefaultXunitCulture]
    public void AddProjectFailsIfFileDoesNotExist()
    {
        var appBuilder = CreateBuilder();

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddProject<TestProject>("projectName"));
        Assert.Equal("Project file 'another-path' was not found.", ex.Message);
    }

    [Fact]
    [UseDefaultXunitCulture]
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

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Publish).DefaultTimeout();

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
                  .WithHttpEndpoint(port: 5003, name: "dontinjectme")
                  // Should not be included in ASPNETCORE_URLS
                  .WithEndpointsInEnvironment(filter: e => e.Name != "dontinjectme")
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
                   })
                  .WithEndpoint("dontinjectme", e =>
                   {
                       e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p3");
                   });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

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

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

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

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.Equal("http://localhost:p0", config["ASPNETCORE_URLS"]);
        Assert.False(config.ContainsKey("ASPNETCORE_HTTPS_PORT"));
    }

    [Fact]
    public async Task ProjectWithMultipleLaunchProfileAppUrlsGetsAllUrls()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        var builder = appBuilder.AddProject<TestProjectWithManyAppUrlsInLaunchSettings>("projectName");

        // Need to allocated all the endpoints we get from the launch profile applicationUrl
        var index = 0;
        foreach (var q in new[] { "http", "http2", "https", "https2", "https3" })
        {
            builder.WithEndpoint(q, e =>
            {
                e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: $"p{index++}");
            });
        }

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();
        var resource = Assert.Single(projectResources);
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.Equal("https://localhost:p2;http://localhost:p0;http://localhost:p1;https://localhost:p3;https://localhost:p4", config["ASPNETCORE_URLS"]);

        // The first https port is the one that should be used for ASPNETCORE_HTTPS_PORT
        Assert.Equal("7144", config["ASPNETCORE_HTTPS_PORT"]);
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

        var manifest = await ManifestUtils.GetManifest(resource).DefaultTimeout();

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
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory"{{fordwardedHeadersEnvVar}},
                "HTTP_PORTS": "{projectName.bindings.http.targetPort}"
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

        var manifest = await ManifestUtils.GetManifest(resource).DefaultTimeout();

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
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "HTTP_PORTS": "{projectName.bindings.http.targetPort}"
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
             .WithEndpoint("ep", e =>
             {
                 e.UriScheme = "http";
                 e.AllocatedEndpoint = new(e, "localhost", 8000);
             })
             .WithArgs(context =>
             {
                 context.Args.Add("arg1");
                 context.Args.Add(c1.GetEndpoint("ep"));
                 context.Args.Add(((IResourceWithEndpoints)context.Resource).GetEndpoint("ep"));
             });

        using var app = appBuilder.Build();

        var args = await ArgumentEvaluator.GetArgumentListAsync(project.Resource).DefaultTimeout();

        Assert.Collection(args,
            arg => Assert.Equal("arg1", arg),
            arg => Assert.Equal("http://localhost:1234", arg),
            arg => Assert.Equal("http://localhost:8000", arg));

        // We don't yet process relationships set via the callbacks
        Assert.False(project.Resource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddProjectWithWildcardUrlInLaunchSettings(bool isProxied)
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProjectWithWildcardUrlInLaunchSettings>("projectName")
            .WithEndpoint("http", e =>
            {
                Assert.Equal("*", e.TargetHost);
                e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p0");
                e.IsProxied = isProxied;
            })
            .WithEndpoint("https", e =>
            {
                Assert.Equal("*", e.TargetHost);
                e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p1");
                e.IsProxied = isProxied;
            });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.Single(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        var http = resource.GetEndpoint("http");
        var https = resource.GetEndpoint("https");

        if (isProxied)
        {
            // When the end point is proxied, the host should be localhost and the port should match the targetPortExpression
            Assert.Equal("http://*:p0;https://*:p1", config["ASPNETCORE_URLS"]);
        }
        else
        {
            Assert.Equal($"http://*:{http.TargetPort};https://*:{https.TargetPort}", config["ASPNETCORE_URLS"]);
        }

        Assert.Equal(https.Port.ToString(), config["ASPNETCORE_HTTPS_PORT"]);
    }

    internal static IDistributedApplicationBuilder CreateBuilder(string[]? args = null, DistributedApplicationOperation operation = DistributedApplicationOperation.Publish)
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

    private sealed class TestProject2 : IProjectMetadata
    {
        public string ProjectPath => "another-path-2";

        public LaunchSettings? LaunchSettings { get; set; }
    }

    internal abstract class BaseProjectWithProfileAndConfig : IProjectMetadata
    {
        protected Dictionary<string, LaunchProfile>? Profiles { get; set; } = new();
        protected string? JsonConfigString { get; set; }

        public string ProjectPath => "another-path";
        public LaunchSettings? LaunchSettings => new LaunchSettings { Profiles = Profiles! };
        public IConfiguration? Configuration => JsonConfigString == null ? null : new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonConfigString)))
            .Build();
    }

    private sealed class TestProjectWithLaunchSettings : BaseProjectWithProfileAndConfig
    {
        public TestProjectWithLaunchSettings()
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
            };
        }
    }

    private sealed class TestProjectWithManyAppUrlsInLaunchSettings : BaseProjectWithProfileAndConfig
    {
        public TestProjectWithManyAppUrlsInLaunchSettings()
        {
            Profiles = new()
            {
                ["https"] = new()
                {
                    CommandName = "Project",
                    CommandLineArgs = "arg1 arg2",
                    LaunchBrowser = true,
                    ApplicationUrl = "https://localhost:7144;http://localhost:5193;http://localhost:5194;https://localhost:7145;https://localhost:7146",
                    EnvironmentVariables = new()
                    {
                        ["ASPNETCORE_ENVIRONMENT"] = "Development"
                    }
                }
            };
        }
    }

    private sealed class TestProjectWithWildcardUrlInLaunchSettings : BaseProjectWithProfileAndConfig
    {
        public TestProjectWithWildcardUrlInLaunchSettings()
        {
            Profiles = new()
            {
                ["https"] = new()
                {
                    CommandName = "Project",
                    CommandLineArgs = "arg1 arg2",
                    LaunchBrowser = true,
                    ApplicationUrl = "http://*:5031;https://*:5033",
                    EnvironmentVariables = new()
                    {
                        ["ASPNETCORE_ENVIRONMENT"] = "Development"
                    }
                }
            };
        }
    }

    [Fact]
    public void WithVolumeAddsContainerMountAnnotationToProjectResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject")
            .WithVolume("shared-data", "/app/shared");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("shared-data", annotation.Source);
        Assert.Equal("/app/shared", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeAnonymousAddsContainerMountAnnotationToProjectResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject")
            .WithVolume("/tmp/cache");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Null(annotation.Source);
        Assert.Equal("/tmp/cache", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.False(annotation.IsReadOnly);
    }

    [Fact]
    public void WithVolumeThrowsArgumentNullExceptionForNullBuilder()
    {
        IResourceBuilder<ProjectResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("vol", "/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeThrowsArgumentNullExceptionForNullTarget()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject");
        
        var ex = Assert.Throws<ArgumentNullException>(() => project.WithVolume("vol", null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullBuilder()
    {
        IResourceBuilder<ProjectResource>? builder = null;
        
        var ex = Assert.Throws<ArgumentNullException>(() => builder!.WithVolume("/data"));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void WithVolumeAnonymousThrowsArgumentNullExceptionForNullTarget()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject");
        
        var ex = Assert.Throws<ArgumentNullException>(() => project.WithVolume(null!));
        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void MultipleWithVolumeCallsAddMultipleAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<TestProject>("myproject")
            .WithVolume("volume1", "/data1")
            .WithVolume("volume2", "/data2", isReadOnly: true)
            .WithVolume("/anonymous");

        var annotations = project.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Equal(3, annotations.Count);
        
        Assert.Equal("volume1", annotations[0].Source);
        Assert.Equal("/data1", annotations[0].Target);
        Assert.False(annotations[0].IsReadOnly);
        
        Assert.Equal("volume2", annotations[1].Source);
        Assert.Equal("/data2", annotations[1].Target);
        Assert.True(annotations[1].IsReadOnly);
        
        Assert.Null(annotations[2].Source);
        Assert.Equal("/anonymous", annotations[2].Target);
        Assert.False(annotations[2].IsReadOnly);
    }

    [Fact]
    public void WithVolumeWorksWithSharedVolumeNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project1 = builder.AddProject<TestProject>("project1")
            .WithVolume("shared", "/data");
        var project2 = builder.AddProject<TestProject2>("project2") 
            .WithVolume("shared", "/backup");

        var annotations1 = project1.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation1 = Assert.Single(annotations1);
        
        var annotations2 = project2.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation2 = Assert.Single(annotations2);
        
        // Both should have the same volume name
        Assert.Equal("shared", annotation1.Source);
        Assert.Equal("shared", annotation2.Source);
        
        // But different mount points
        Assert.Equal("/data", annotation1.Target);
        Assert.Equal("/backup", annotation2.Target);
    }

    [Fact] 
    public void ContainerResourcesStillUseExistingExtensions()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        
        // Container resources should use ContainerResourceBuilderExtensions.WithVolume as before
        var container = builder.AddContainer("mycontainer", "nginx")
            .WithVolume("myvolume", "/app/data", isReadOnly: true);

        var annotations = container.Resource.Annotations.OfType<ContainerMountAnnotation>();
        var annotation = Assert.Single(annotations);
        
        Assert.Equal("myvolume", annotation.Source);
        Assert.Equal("/app/data", annotation.Target);
        Assert.Equal(ContainerMountType.Volume, annotation.Type);
        Assert.True(annotation.IsReadOnly);
    }
}
