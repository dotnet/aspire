// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.Milvus;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Milvus;
public class AddMilvusTests
{
    private const int MilvusPortGrpc = 19530;

    [Fact]
    public void AddMilvusWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:apikey"] = "pass";

        var pass = appBuilder.AddParameter("apikey");
        appBuilder.AddMilvus("my-milvus", apiKey: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("my-milvus", containerResource.Name);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MilvusContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MilvusContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MilvusContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = containerResource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "grpc");
        Assert.NotNull(endpoint);
        Assert.Equal(MilvusPortGrpc, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("grpc", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
    }

    [Fact]
    public void AddMilvusAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:apikey"] = "pass";

        var pass = appBuilder.AddParameter("apikey");
        appBuilder.AddMilvus("my-milvus", apiKey: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("my-milvus", containerResource.Name);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MilvusContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MilvusContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MilvusContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = containerResource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "grpc");
        Assert.NotNull(endpoint);
        Assert.Equal(MilvusPortGrpc, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("grpc", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
    }

    [Fact]
    public async Task MilvusCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Configuration["Parameters:apikey"] = "pass";
        var pass = appBuilder.AddParameter("apikey");

        var milvus = appBuilder.AddMilvus("my-milvus", pass)
                                 .WithEndpoint("grpc", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", MilvusPortGrpc));

        var connectionStringResource = milvus.Resource as IResourceWithConnectionString;

        var connectionString = await connectionStringResource.GetConnectionStringAsync();
        Assert.Equal($"Endpoint=http://localhost:19530;Key=pass", connectionString);
    }

    [Fact]
    public async Task MilvusClientAppWithReferenceContainsConnectionStrings()
    {
        using var testProgram = CreateTestProgram();
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Configuration["Parameters:apikey"] = "pass";
        var pass = appBuilder.AddParameter("apikey");

        var milvus = appBuilder.AddMilvus("my-milvus", pass)
            .WithEndpoint("grpc", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", MilvusPortGrpc));

        var projectA = appBuilder.AddProject<ProjectA>("projecta")
            .WithReference(milvus);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);

        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__my-milvus" && kvp.Value == "Endpoint=http://localhost:19530;Key=pass");

        var container1 = appBuilder.AddContainer("container1", "fake")
            .WithReference(milvus);

        // Call environment variable callbacks.
        var containerConfig = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(container1.Resource);

        var containerServicesKeysCount = containerConfig.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, containerServicesKeysCount);

        Assert.Contains(containerConfig, kvp => kvp.Key == "ConnectionStrings__my-milvus" && kvp.Value == "Endpoint=http://localhost:19530;Key=pass");
    }

    [Fact]
    public async Task VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions() { Args = new string[] { "--publisher", "manifest" } });
        appBuilder.Configuration["Parameters:apikey"] = "pass";
        var pass = appBuilder.AddParameter("apikey");
        var milvus = appBuilder.AddMilvus("milvus", pass);
        var db1 = milvus.AddDatabase("db1");

        var serverManifest = await ManifestUtils.GetManifest(milvus.Resource); // using this method does not get any ExecutionContext.IsPublishMode changes
        var dbManifest = await ManifestUtils.GetManifest(db1.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Endpoint={milvus.bindings.grpc.url};Key={apikey.value}",
              "image": "{{MilvusContainerImageTags.Registry}}/{{MilvusContainerImageTags.Image}}:{{MilvusContainerImageTags.Tag}}",
              "args": [
                "milvus",
                "run",
                "standalone"
              ],
              "env": {
                "COMMON_STORAGETYPE": "local",
                "ETCD_USE_EMBED": "true",
                "ETCD_DATA_DIR": "/var/lib/milvus/etcd",
                "COMMON_SECURITY_AUTHORIZATIONENABLED": "true"
              },
              "bindings": {
                "grpc": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http2",
                  "targetPort": 19530
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, serverManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{milvus.connectionString};Database=db1"
            }
            """;
        Assert.Equal(expectedManifest, dbManifest.ToString());
    }

    [Fact]
    public void AddMilvusWithSpecifyingPorts()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["Parameters:apikey"] = "pass";
        var pass = builder.AddParameter("apikey");

        var milvus = builder.AddMilvus("my-milvus", grpcPort: 5503, apiKey: pass);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var milvusResource = Assert.Single(appModel.Resources.OfType<MilvusServerResource>());
        Assert.Equal("my-milvus", milvusResource.Name);

        Assert.Single(milvusResource.Annotations.OfType<EndpointAnnotation>());

        var grpcEndpoint = milvusResource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "grpc");
        Assert.Equal(MilvusPortGrpc, grpcEndpoint.TargetPort);
        Assert.False(grpcEndpoint.IsExternal);
        Assert.Equal(5503, grpcEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, grpcEndpoint.Protocol);
        Assert.Equal("http2", grpcEndpoint.Transport);
        Assert.Equal("http", grpcEndpoint.UriScheme);
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<AddMilvusTests>(args);

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }
}
