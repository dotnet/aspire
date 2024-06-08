// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class KestrelConfigTests
{
    [Fact]
    public async Task SingleKestrelHttpEndpointIsNamedHttpAndOverridesProfile()
    {
        var resource = CreateTestProjectResource<ProjectWithProfileEndpointAndKestrelHttpEndpoint>(operation: DistributedApplicationOperation.Run);

        Assert.Collection(
            resource.Annotations.OfType<EndpointAnnotation>(),
            a =>
            {
                // Endpoint is named "http", because there is only one Kestrel http endpoint
                Assert.Equal("http", a.Name);
                Assert.Equal("http", a.UriScheme);
                Assert.Equal(5002, a.Port);
            }
            );

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

        // When using Kestrel, we should not be setting ASPNETCORE_URLS at all
        Assert.False(config.ContainsKey("ASPNETCORE_URLS"));
    }

    [Fact]
    public void SingleKestrelHttpsEndpointIsNamedHttps()
    {
        var resource = CreateTestProjectResource<ProjectWithKestrelHttpsEndpoint>(operation: DistributedApplicationOperation.Run);

        Assert.Collection(
            resource.Annotations.OfType<EndpointAnnotation>(),
            a =>
            {
                // Endpoint is named "https", because there is only one Kestrel https endpoint
                Assert.Equal("https", a.Name);
                Assert.Equal("https", a.UriScheme);
                Assert.Equal(7002, a.Port);
            }
            );
    }

    [Fact]
    public void MultipleKestrelHttpEndpointsKeepTheirNames()
    {
        var resource = CreateTestProjectResource<ProjectWithMultipleHttpKestrelEndpoints>(operation: DistributedApplicationOperation.Run);

        Assert.Collection(
            resource.Annotations.OfType<EndpointAnnotation>(),
            a =>
            {
                // Endpoints keep their config names because there are multiple Kestrel http endpoints
                Assert.Equal("FirstHttpEndpoint", a.Name);
                Assert.Equal("http", a.UriScheme);
                Assert.Equal(5002, a.Port);
            },
            a =>
            {
                Assert.Equal("SecondHttpEndpoint", a.Name);
                Assert.Equal("http", a.UriScheme);
                Assert.Equal(5003, a.Port);
            }
            );
    }

    [Fact]
    public async Task VerifyKestrelEndpointManifestGeneration()
    {
        var resource = CreateTestProjectResource<ProjectWithOnlyKestrelHttpEndpoint>();

        var manifest = await ManifestUtils.GetManifest(resource);

        var expectedManifest = $$"""
            {
              "type": "project.v0",
              "path": "another-path",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "Kestrel__Endpoints__http__Url": "http://*:{projectName.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 5002
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
    public async Task VerifyMultipleKestrelEndpointsManifestGeneration()
    {
        var resource = CreateTestProjectResource<ProjectWithMultipleHttpKestrelEndpoints>(operation: DistributedApplicationOperation.Publish);

        var manifest = await ManifestUtils.GetManifest(resource);

        var expectedManifest = $$"""
            {
              "type": "project.v0",
              "path": "another-path",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "Kestrel__Endpoints__FirstHttpEndpoint__Url": "http://*:{projectName.bindings.FirstHttpEndpoint.targetPort}",
                "Kestrel__Endpoints__SecondHttpEndpoint__Url": "http://*:{projectName.bindings.SecondHttpEndpoint.targetPort}"
              },
              "bindings": {
                "FirstHttpEndpoint": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http2",
                  "targetPort": 5002
                },
                "SecondHttpEndpoint": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http2",
                  "targetPort": 5003
                },
                "https": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http2"
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    private static ProjectResource CreateTestProjectResource<TProject>(DistributedApplicationOperation operation = DistributedApplicationOperation.Publish) where TProject : IProjectMetadata, new()
    {
        var appBuilder = ProjectResourceTests.CreateBuilder(operation: operation);
        appBuilder.AddProject<TProject>("projectName");
        DistributedApplication app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();
        return Assert.Single(projectResources);
    }

    private sealed class ProjectWithOnlyKestrelHttpEndpoint : ProjectResourceTests.BaseProjectWithProfileAndConfig
    {
        public ProjectWithOnlyKestrelHttpEndpoint()
        {
            JsonConfigString = """
            {
              "Kestrel": {
                "Endpoints": {
                  "SomeHttpEndpoint": { "Url": "http://*:5002" }
                }
              }
            }
            """;
        }
    }

    private sealed class ProjectWithProfileEndpointAndKestrelHttpEndpoint : ProjectResourceTests.BaseProjectWithProfileAndConfig
    {
        public ProjectWithProfileEndpointAndKestrelHttpEndpoint()
        {
            Profiles = new()
            {
                ["OnlyHttp"] = new ()
                {
                    ApplicationUrl = "http://localhost:5031",
                }
            };
            JsonConfigString = """
            {
              "Kestrel": {
                "Endpoints": {
                  "SomeHttpEndpoint": { "Url": "http://*:5002" }
                }
              }
            }
            """;
        }
    }

    private sealed class ProjectWithKestrelHttpsEndpoint : ProjectResourceTests.BaseProjectWithProfileAndConfig
    {
        public ProjectWithKestrelHttpsEndpoint()
        {
            JsonConfigString = """
            {
              "Kestrel": {
                "Endpoints": {
                  "SomeHttpsEndpoint": { "Url": "https://*:7002" }
                }
              }
            }
            """;
        }
    }

    private sealed class ProjectWithMultipleHttpKestrelEndpoints : ProjectResourceTests.BaseProjectWithProfileAndConfig
    {
        public ProjectWithMultipleHttpKestrelEndpoints()
        {
            JsonConfigString = """
            {
              "Kestrel": {
                "EndpointDefaults": {
                  "Protocols": "Http2"
                },
                "Endpoints": {
                  "FirstHttpEndpoint": { "Url": "http://*:5002" },
                  "SecondHttpEndpoint": { "Url": "http://localhost:5003" }
                }
              }
            }
            """;
        }
    }
}
