// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Meilisearch.Tests;
public class AddMeilisearchTests
{
    [Fact]
    public async Task AddMeilisearchContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var meilisearch = appBuilder.AddMeilisearch("meilisearch");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MeilisearchResource>());
        Assert.Equal("meilisearch", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.Single(endpoints);

        var primaryEndpoint = Assert.Single(endpoints, e => e.Name == "http");
        Assert.Equal(7700, primaryEndpoint.TargetPort);
        Assert.False(primaryEndpoint.IsExternal);
        Assert.Equal("http", primaryEndpoint.Name);
        Assert.Null(primaryEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.Equal("http", primaryEndpoint.Transport);
        Assert.Equal("http", primaryEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MeilisearchContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MeilisearchContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MeilisearchContainerImageTags.Registry, containerAnnotation.Registry);

        var config = await meilisearch.Resource.GetEnvironmentVariableValuesAsync();

        var env = Assert.Single(config);
        Assert.Equal("MEILI_MASTER_KEY", env.Key);
        Assert.False(string.IsNullOrEmpty(env.Value));
    }

    [Fact]
    public async Task AddMeilisearchContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var masterKey = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(appBuilder, $"masterKey");

        appBuilder.Configuration["Parameters:masterkey"] = masterKey.Value;
        var masterKeyParameter = appBuilder.AddParameter(masterKey.Name);
        var meilisearch = appBuilder.AddMeilisearch("meilisearch", masterKeyParameter);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MeilisearchResource>());
        Assert.Equal("meilisearch", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.Single(endpoints);

        var primaryEndpoint = Assert.Single(endpoints, e => e.Name == "http");
        Assert.Equal(7700, primaryEndpoint.TargetPort);
        Assert.False(primaryEndpoint.IsExternal);
        Assert.Equal("http", primaryEndpoint.Name);
        Assert.Null(primaryEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.Equal("http", primaryEndpoint.Transport);
        Assert.Equal("http", primaryEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MeilisearchContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MeilisearchContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MeilisearchContainerImageTags.Registry, containerAnnotation.Registry);

        var config = await meilisearch.Resource.GetEnvironmentVariableValuesAsync();

        var env = Assert.Single(config);
        Assert.Equal("MEILI_MASTER_KEY", env.Key);
        Assert.False(string.IsNullOrEmpty(env.Value));
    }

    [Fact]
    public async Task MeilisearchCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var meilisearch = appBuilder
            .AddMeilisearch("meilisearch")
            .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27020));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<MeilisearchResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal($"Endpoint=http://localhost:27020;MasterKey={meilisearch.Resource.MasterKeyParameter.Value}", connectionString);
        Assert.Equal("Endpoint=http://{meilisearch.bindings.http.host}:{meilisearch.bindings.http.port};MasterKey={meilisearch-masterKey.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task VerifyManifestWithDefaultsAddsAnnotationMetadata()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var meilisearch = appBuilder.AddMeilisearch("meilisearch");

        var manifest = await ManifestUtils.GetManifest(meilisearch.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Endpoint=http://{meilisearch.bindings.http.host}:{meilisearch.bindings.http.port};MasterKey={meilisearch-masterKey.value}",
              "image": "{{MeilisearchContainerImageTags.Registry}}/{{MeilisearchContainerImageTags.Image}}:{{MeilisearchContainerImageTags.Tag}}",
              "env": {
                "MEILI_MASTER_KEY": "{meilisearch-masterKey.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 7700
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

        var meilisearch = appBuilder.AddMeilisearch("meilisearch")
            .WithDataVolume("data");

        var manifest = await ManifestUtils.GetManifest(meilisearch.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Endpoint=http://{meilisearch.bindings.http.host}:{meilisearch.bindings.http.port};MasterKey={meilisearch-masterKey.value}",
              "image": "{{MeilisearchContainerImageTags.Registry}}/{{MeilisearchContainerImageTags.Image}}:{{MeilisearchContainerImageTags.Tag}}",
              "volumes": [
                {
                  "name": "data",
                  "target": "/meili_data",
                  "readOnly": false
                }
              ],
              "env": {
                "MEILI_MASTER_KEY": "{meilisearch-masterKey.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 7700
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
