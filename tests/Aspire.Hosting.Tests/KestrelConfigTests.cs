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
        var resource = CreateTestProjectResource<ProjectWithProfileEndpointAndKestrelHttpEndpoint>(
            operation: DistributedApplicationOperation.Run,
            callback: builder =>
            {
                builder.WithHttpEndpoint(5017, name: "ExplicitHttp");
            });

        Assert.Collection(
            resource.Annotations.OfType<EndpointAnnotation>(),
            a =>
            {
                // Endpoint is named "http", because there is only one Kestrel http endpoint
                Assert.Equal("http", a.Name);
                Assert.Equal("http", a.UriScheme);
                Assert.Equal(5002, a.Port);
            },
            a =>
            {
                Assert.Equal("ExplicitHttp", a.Name);
            }
            );

        // Even though we're in Run mode, we need to get the env in Publish mode since ports don't get allocated
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Publish);

        // When using Kestrel, we should not be setting ASPNETCORE_URLS at all
        Assert.False(config.ContainsKey("ASPNETCORE_URLS"));

        // Instead, we should be setting the Kestrel override
        Assert.Equal("http://*:{projectName.bindings.http.targetPort}", config["Kestrel__Endpoints__http__Url"]);
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
    public async Task ExplicitEndpointsResultInKestrelOverridesAtRuntime()
    {
        var resource = CreateTestProjectResource<ProjectWithMultipleHttpKestrelEndpoints>(
            operation: DistributedApplicationOperation.Run,
            callback: builder =>
            {
                builder.WithHttpEndpoint(5017, name: "ExplicitProxiedHttp");
                builder.WithHttpEndpoint(5018, name: "ExplicitNoProxyHttp", isProxied: false);
            });

        // Even though we're in Run mode, we need to get the env in Publish mode since ports don't get allocated
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Publish);

        Assert.Collection(
            config.Where(envVar => envVar.Key.StartsWith("Kestrel__")),
            envVar =>
            {
                Assert.Equal("Kestrel__Endpoints__FirstHttpEndpoint__Url", envVar.Key);
                Assert.Equal("http://*:{projectName.bindings.FirstHttpEndpoint.targetPort}", envVar.Value);
            },
            envVar =>
            {
                Assert.Equal("Kestrel__Endpoints__SecondHttpEndpoint__Url", envVar.Key);
                // Note that localhost (from Kestrel config) is preserved at runtime
                Assert.Equal("http://localhost:{projectName.bindings.SecondHttpEndpoint.targetPort}", envVar.Value);
            },
            envVar =>
            {
                Assert.Equal("Kestrel__Endpoints__ExplicitProxiedHttp__Url", envVar.Key);
                Assert.Equal("http://*:{projectName.bindings.ExplicitProxiedHttp.targetPort}", envVar.Value);
            },
            envVar =>
            {
                // Proxy vs no proxy doesn't make a difference here, since we're always dealing with the target port
                Assert.Equal("Kestrel__Endpoints__ExplicitNoProxyHttp__Url", envVar.Key);
                Assert.Equal("http://*:{projectName.bindings.ExplicitNoProxyHttp.targetPort}", envVar.Value);
            }
            );
    }

    [Fact]
    public async Task VerifyKestrelEndpointManifestGeneration()
    {
        var resource = CreateTestProjectResource<ProjectWithOnlyKestrelHttpEndpoint>();

        var manifest = await ManifestUtils.GetManifest(resource);

        var expectedManifest = """
            {
              "type": "project.v0",
              "path": "another-path",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "Kestrel__Endpoints__http__Url": "http://*:{projectName.bindings.http.targetPort}",
                "Kestrel__Endpoints__https__Url": "https://*:{projectName.bindings.https.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 5002
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
        var resource = CreateTestProjectResource<ProjectWithMultipleHttpKestrelEndpoints>(
            operation: DistributedApplicationOperation.Publish,
            callback: builder =>
            {
                builder.WithHttpEndpoint(5017, name: "ExplicitProxiedHttp");
                builder.WithHttpEndpoint(5018, name: "ExplicitNoProxyHttp", isProxied: false);
            });

        var manifest = await ManifestUtils.GetManifest(resource);

        // Note that unlike in Run mode, SecondHttpEndpoint is using host * instead of localhost
        var expectedManifest = """
            {
              "type": "project.v0",
              "path": "another-path",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "Kestrel__Endpoints__FirstHttpEndpoint__Url": "http://*:{projectName.bindings.FirstHttpEndpoint.targetPort}",
                "Kestrel__Endpoints__SecondHttpEndpoint__Url": "http://*:{projectName.bindings.SecondHttpEndpoint.targetPort}",
                "Kestrel__Endpoints__https__Url": "https://*:{projectName.bindings.https.targetPort}",
                "Kestrel__Endpoints__ExplicitProxiedHttp__Url": "http://*:{projectName.bindings.ExplicitProxiedHttp.targetPort}",
                "Kestrel__Endpoints__ExplicitNoProxyHttp__Url": "http://*:{projectName.bindings.ExplicitNoProxyHttp.targetPort}"
              },
              "bindings": {
                "FirstHttpEndpoint": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http2",
                  "port": 5002
                },
                "SecondHttpEndpoint": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http2",
                  "port": 5003
                },
                "https": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http2"
                },
                "ExplicitProxiedHttp": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "port": 5017
                },
                "ExplicitNoProxyHttp": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 5018
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    private static ProjectResource CreateTestProjectResource<TProject>(
        DistributedApplicationOperation operation = DistributedApplicationOperation.Publish,
        Action<IResourceBuilder<ProjectResource>>? callback = null) where TProject : IProjectMetadata, new()
    {
        var appBuilder = ProjectResourceTests.CreateBuilder(operation: operation);
        var projectBuilder = appBuilder.AddProject<TProject>("projectName");
        if (callback != null)
        {
            callback(projectBuilder);
        }
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
