// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Elasticsearch;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Elasticsearch;
public class AddElasticsearchTests
{
    [Fact]
    public async Task AddElasticsearchContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddElasticsearch("elasticsearch");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<ElasticsearchResource>());
        Assert.Equal("elasticsearch", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.Equal(2, endpoints.Count());

        var primaryEndpoint = Assert.Single(endpoints, e => e.Name == "http");
        Assert.Equal(9200, primaryEndpoint.TargetPort);
        Assert.False(primaryEndpoint.IsExternal);
        Assert.Equal("http", primaryEndpoint.Name);
        Assert.Null(primaryEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.Equal("http", primaryEndpoint.Transport);
        Assert.Equal("http", primaryEndpoint.UriScheme);

        var internalEndpoint = Assert.Single(endpoints, e => e.Name == "internal");
        Assert.Equal(9300, internalEndpoint.TargetPort);
        Assert.False(internalEndpoint.IsExternal);
        Assert.Equal("internal", internalEndpoint.Name);
        Assert.Null(internalEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, internalEndpoint.Protocol);
        Assert.Equal("tcp", internalEndpoint.Transport);
        Assert.Equal("tcp", internalEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ElasticsearchContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(ElasticsearchContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(ElasticsearchContainerImageTags.Registry, containerAnnotation.Registry);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource);

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("discovery.type", env.Key);
                Assert.Equal("single-node", env.Value);
            },
            env =>
            {
                Assert.Equal("xpack.security.enabled", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("ELASTIC_PASSWORD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public async Task AddElasticsearchContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddElasticsearch("elasticsearch",pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<ElasticsearchResource>());
        Assert.Equal("elasticsearch", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.Equal(2, endpoints.Count());

        var primaryEndpoint = Assert.Single(endpoints, e => e.Name == "http");
        Assert.Equal(9200, primaryEndpoint.TargetPort);
        Assert.False(primaryEndpoint.IsExternal);
        Assert.Equal("http", primaryEndpoint.Name);
        Assert.Null(primaryEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.Equal("http", primaryEndpoint.Transport);
        Assert.Equal("http", primaryEndpoint.UriScheme);

        var internalEndpoint = Assert.Single(endpoints, e => e.Name == "internal");
        Assert.Equal(9300, internalEndpoint.TargetPort);
        Assert.False(internalEndpoint.IsExternal);
        Assert.Equal("internal", internalEndpoint.Name);
        Assert.Null(internalEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, internalEndpoint.Protocol);
        Assert.Equal("tcp", internalEndpoint.Transport);
        Assert.Equal("tcp", internalEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ElasticsearchContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(ElasticsearchContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(ElasticsearchContainerImageTags.Registry, containerAnnotation.Registry);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource);

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("discovery.type", env.Key);
                Assert.Equal("single-node", env.Value);
            },
            env =>
            {
                Assert.Equal("xpack.security.enabled", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("ELASTIC_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public async Task ElasticsearchCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var elasticsearch = appBuilder
            .AddElasticsearch("elasticsearch")
            .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27020));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<ElasticsearchResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal($"http://elastic:{elasticsearch.Resource.PasswordParameter.Value}@localhost:27020", connectionString);
        Assert.Equal("http://elastic:{elasticsearch-password.value}@{elasticsearch.bindings.http.host}:{elasticsearch.bindings.http.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task VerifyManifestWithDefaultsAddsAnnotationMetadata()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var elasticsearch = appBuilder.AddElasticsearch("elasticsearch");

        var manifest = await ManifestUtils.GetManifest(elasticsearch.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "http://elastic:{elasticsearch-password.value}@{elasticsearch.bindings.http.host}:{elasticsearch.bindings.http.port}",
              "image": "{{ElasticsearchContainerImageTags.Registry}}/{{ElasticsearchContainerImageTags.Image}}:{{ElasticsearchContainerImageTags.Tag}}",
              "env": {
                "discovery.type": "single-node",
                "xpack.security.enabled": "true",
                "ELASTIC_PASSWORD": "{elasticsearch-password.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 9200
                },
                "internal": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9300
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithDataVolumeAddsAnnotationMetadata()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var elasticsearch = appBuilder.AddElasticsearch("elasticsearch")
            .WithDataVolume("data");

        var manifest = await ManifestUtils.GetManifest(elasticsearch.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "http://elastic:{elasticsearch-password.value}@{elasticsearch.bindings.http.host}:{elasticsearch.bindings.http.port}",
              "image": "{{ElasticsearchContainerImageTags.Registry}}/{{ElasticsearchContainerImageTags.Image}}:{{ElasticsearchContainerImageTags.Tag}}",
              "volumes": [
                {
                  "name": "data",
                  "target": "/usr/share/elasticsearch/data",
                  "readOnly": false
                }
              ],
              "env": {
                "discovery.type": "single-node",
                "xpack.security.enabled": "true",
                "ELASTIC_PASSWORD": "{elasticsearch-password.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 9200
                },
                "internal": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9300
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
