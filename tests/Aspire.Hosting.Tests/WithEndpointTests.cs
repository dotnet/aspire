// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithEndpointTests
{
    // copied from /src/Shared/StringComparers.cs to avoid ambiguous reference since StringComparers exists internally in multiple Hosting assemblies.
    private static StringComparison EndpointAnnotationName => StringComparison.OrdinalIgnoreCase;

    [Fact]
    public void WithEndpointInvokesCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint(3000, 1000, name: "mybinding")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  endpoint.Port = 2000;
                              });

        var endpoint = projectA.Resource.Annotations.OfType<EndpointAnnotation>()
            .Where(e => string.Equals(e.Name, "mybinding", EndpointAnnotationName)).Single();
        Assert.Equal(2000, endpoint.Port);
    }

    [Fact]
    public void WithEndpointMakesTargetPortEqualToPortIfProxyless()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  endpoint.Port = 2000;
                                  endpoint.IsProxied = false;
                              });

        var endpoint = projectA.Resource.Annotations.OfType<EndpointAnnotation>()
            .Where(e => string.Equals(e.Name, "mybinding", EndpointAnnotationName)).Single();

        // It should fall back to the Port value since TargetPort was not set
        Assert.Equal(2000, endpoint.TargetPort);

        // In Proxy mode, the fallback should not happen
        endpoint.IsProxied = true;
        Assert.Null(endpoint.TargetPort);

        // Back in proxy-less mode, it should fall back again
        endpoint.IsProxied = false;
        Assert.Equal(2000, endpoint.TargetPort);

        // Setting it to null explicitly should disable the override mechanism
        endpoint.TargetPort = null;
        Assert.Null(endpoint.TargetPort);

        // No fallback when setting TargetPort explicitly
        endpoint.TargetPort = 2001;
        Assert.Equal(2001, endpoint.TargetPort);
    }

    [Fact]
    public void WithEndpointMakesPortEqualToTargetPortIfProxyless()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  endpoint.TargetPort = 2000;
                                  endpoint.IsProxied = false;
                              });

        var endpoint = projectA.Resource.Annotations.OfType<EndpointAnnotation>()
            .Where(e => string.Equals(e.Name, "mybinding", EndpointAnnotationName)).Single();

        // It should fall back to the TargetPort value since Port was not set
        Assert.Equal(2000, endpoint.Port);

        // In Proxy mode, the fallback should not happen
        endpoint.IsProxied = true;
        Assert.Null(endpoint.Port);

        // Back in proxy-less mode, it should fall back again
        endpoint.IsProxied = false;
        Assert.Equal(2000, endpoint.Port);

        // Setting it to null explicitly should disable the override mechanism
        endpoint.Port = null;
        Assert.Null(endpoint.Port);

        // No fallback when setting Port explicitly
        endpoint.Port = 2001;
        Assert.Equal(2001, endpoint.Port);
    }

    [Fact]
    public void WithEndpointCallbackDoesNotRunIfEndpointDoesntExistAndCreateIfNotExistsIsFalse()
    {
        var executed = false;

        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  executed = true;
                              },
                              createIfNotExists: false);

        Assert.False(executed);
        Assert.False(projectA.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var annotations));
    }

    [Fact]
    public void WithEndpointCallbackRunsIfEndpointDoesntExistAndCreateIfNotExistsIsDefault()
    {
        var executed = false;

        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  executed = true;
                              });

        Assert.True(executed);
        Assert.True(projectA.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out _));
    }

    [Fact]
    public void WithEndpointCallbackRunsIfEndpointDoesntExistAndCreateIfNotExistsIsTrue()
    {
        var executed = false;

        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta").WithEndpoint("mybinding", endpoint =>
        {
            executed = true;
        },
        createIfNotExists: true);

        Assert.True(executed);
        Assert.True(projectA.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out _));
    }

    [Fact]
    public void EndpointsWithTwoPortsSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();

            builder.AddProject<ProjectA>("projecta")
                    .WithHttpsEndpoint(3000, 1000, name: "mybinding")
                    .WithHttpsEndpoint(3000, 2000, name: "mybinding");
        });

        Assert.Equal("Endpoint with name 'mybinding' already exists. Endpoint name may not have been explicitly specified and was derived automatically from scheme argument (e.g. 'http', 'https', or 'tcp'). Multiple calls to WithEndpoint (and related methods) may result in a conflict if name argument is not specified. Each endpoint must have a unique name. For more information on networking in .NET Aspire see: https://aka.ms/dotnet/aspire/networking", ex.Message);
    }

    [Fact]
    public void AddingTwoEndpointsWithDefaultNames()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();

            builder.AddProject<ProjectA>("projecta")
                    .WithHttpsEndpoint(3000, 1000)
                    .WithHttpsEndpoint(3000, 2000);
        });

        Assert.Equal("Endpoint with name 'https' already exists. Endpoint name may not have been explicitly specified and was derived automatically from scheme argument (e.g. 'http', 'https', or 'tcp'). Multiple calls to WithEndpoint (and related methods) may result in a conflict if name argument is not specified. Each endpoint must have a unique name. For more information on networking in .NET Aspire see: https://aka.ms/dotnet/aspire/networking", ex.Message);
    }

    [Fact]
    public void EndpointsWithSinglePortSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            builder.AddProject<ProjectB>("projectb")
                   .WithHttpsEndpoint(1000, name: "mybinding")
                   .WithHttpsEndpoint(2000, name: "mybinding");
        });

        Assert.Equal("Endpoint with name 'mybinding' already exists. Endpoint name may not have been explicitly specified and was derived automatically from scheme argument (e.g. 'http', 'https', or 'tcp'). Multiple calls to WithEndpoint (and related methods) may result in a conflict if name argument is not specified. Each endpoint must have a unique name. For more information on networking in .NET Aspire see: https://aka.ms/dotnet/aspire/networking", ex.Message);
    }

    [Fact]
    public async Task CanAddEndpointsWithContainerPortAndEnv()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddExecutable("foo", "foo", ".")
               .WithHttpEndpoint(targetPort: 3001, name: "mybinding", env: "PORT");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var exeResources = appModel.GetExecutableResources();

        var resource = Assert.Single(exeResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource);

        Assert.Equal("foo", resource.Name);
        var endpoints = resource.Annotations.OfType<EndpointAnnotation>().ToArray();
        Assert.Single(endpoints);
        Assert.Equal("mybinding", endpoints[0].Name);
        Assert.Equal(3001, endpoints[0].TargetPort);
        Assert.Equal("http", endpoints[0].UriScheme);
        Assert.Equal("3001", config["PORT"]);
    }

    [Fact]
    public void GettingContainerHostNameFailsIfNoContainerHostNameSet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
            .WithEndpoint("ep", e =>
            {
                e.AllocatedEndpoint = new(e, "localhost", 8031);
            });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            return container.GetEndpoint("ep").ContainerHost;
        });

        Assert.Equal("The endpoint \"ep\" has no associated container host name.", ex.Message);
    }

    [Fact]
    public void WithExternalHttpEndpointsMarkExistingHttpEndpointsAsExternal()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithEndpoint(name: "ep0")
                               .WithHttpEndpoint(name: "ep1")
                               .WithHttpsEndpoint(name: "ep2")
                               .WithExternalHttpEndpoints();

        var ep0 = container.GetEndpoint("ep0");
        var ep1 = container.GetEndpoint("ep1");
        var ep2 = container.GetEndpoint("ep2");

        Assert.False(ep0.EndpointAnnotation.IsExternal);
        Assert.True(ep1.EndpointAnnotation.IsExternal);
        Assert.True(ep2.EndpointAnnotation.IsExternal);
    }

    // Existing code...

    [Fact]
    public async Task VerifyManifestWithBothDifferentPortAndTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithEndpoint(name: "ep0", port: 8080, targetPort: 3000);

        var manifest = await ManifestUtils.GetManifest(container.Resource);
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "ep0": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "port": 8080,
                  "targetPort": 3000
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithHttpPortWithTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpEndpoint(name: "h1", targetPort: 3001);

        var manifest = await ManifestUtils.GetManifest(container.Resource);
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h1": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 3001
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithHttpsAndTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpsEndpoint(name: "h2", targetPort: 3001);

        var manifest = await ManifestUtils.GetManifest(container.Resource);
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h2": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 3001
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestContainerWithHttpEndpointAndNoPortsAllocatesPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpEndpoint(name: "h3");

        var manifest = await ManifestUtils.GetManifest(container.Resource);
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h3": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8000
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestContainerWithHttpsEndpointAllocatesPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpsEndpoint(name: "h4");

        var manifest = await ManifestUtils.GetManifest(container.Resource);
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h4": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8000
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithHttpEndpointAndPortOnlySetsTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpEndpoint(name: "otlp", port: 1004);

        var manifest = await ManifestUtils.GetManifest(container.Resource);
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "otlp": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 1004
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithTcpEndpointAndNoPortAllocatesPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithEndpoint(name: "custom");

        var manifest = await ManifestUtils.GetManifest(container.Resource);
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "custom": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8000
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestProjectWithHttpEndpointDoesNotAllocatePort()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var project = builder.AddProject<TestProject>("proj")
            .WithHttpEndpoint(name: "hp")
            .WithHttpsEndpoint(name: "hps");

        var manifest = await ManifestUtils.GetManifest(project.Resource);

        var expectedManifest =
            """
            {
              "type": "project.v0",
              "path": "projectpath",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "HTTP_PORTS": "{proj.bindings.hp.targetPort}",
                "HTTPS_PORTS": "{proj.bindings.hps.targetPort}"
              },
              "bindings": {
                "hp": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http"
                },
                "hps": {
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
    public async Task VerifyManifestProjectWithEndpointsSetsPortsEnvVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var project = builder.AddProject<TestProject>("proj")
            .WithHttpEndpoint()
            .WithHttpEndpoint(name: "hp1", port: 5001)
            .WithHttpEndpoint(name: "hp2", port: 5002, targetPort: 5003)
            .WithHttpEndpoint(name: "hp3", targetPort: 5004)
            .WithHttpEndpoint(name: "hp4")
            .WithHttpsEndpoint()
            .WithHttpsEndpoint(name: "hps1", port: 7001)
            .WithHttpsEndpoint(name: "hps2", port: 7002, targetPort: 7003)
            .WithHttpsEndpoint(name: "hps3", targetPort: 7004)
            .WithHttpsEndpoint(name: "hps4", targetPort: 7005);

        var manifest = await ManifestUtils.GetManifest(project.Resource);

        var expectedEnv =
            """
            {
              "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
              "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
              "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
              "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
              "HTTP_PORTS": "{proj.bindings.http.targetPort};{proj.bindings.hp1.targetPort};{proj.bindings.hp2.targetPort};{proj.bindings.hp3.targetPort};{proj.bindings.hp4.targetPort}",
              "HTTPS_PORTS": "{proj.bindings.https.targetPort};{proj.bindings.hps1.targetPort};{proj.bindings.hps2.targetPort};{proj.bindings.hps3.targetPort};{proj.bindings.hps4.targetPort}"
            }
            """;

        Assert.Equal(expectedEnv, manifest["env"]!.ToString());
    }

    [Fact]
    public async Task VerifyManifestPortAllocationIsGlobal()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container0 = builder.AddContainer("app0", "image")
                               .WithEndpoint(name: "custom");

        var container1 = builder.AddContainer("app1", "image")
                               .WithEndpoint(name: "custom");

        var manifests = await ManifestUtils.GetManifests([container0.Resource, container1.Resource]);
        var expectedManifest0 =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "custom": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8000
                }
              }
            }
            """;

        var expectedManifest1 =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "custom": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8001
                }
              }
            }
            """;

        Assert.Equal(expectedManifest0, manifests[0].ToString());
        Assert.Equal(expectedManifest1, manifests[1].ToString());
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "projectpath";

        public LaunchSettings? LaunchSettings { get; } = new();
    }
    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class ProjectB : IProjectMetadata
    {
        public string ProjectPath => "projectB";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}
