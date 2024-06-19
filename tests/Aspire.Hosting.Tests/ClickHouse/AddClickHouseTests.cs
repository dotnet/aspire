// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ClickHouse;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.ClickHouse;

public class AddClickHouseTests
{
    [Fact]
    public void AddClickHouseContainerWithDefaultsAddsAnnotationMetadata()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddClickHouse("clickhouse");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<ClickHouseServerResource>());
        Assert.Equal("clickhouse", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(8123, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("http", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ClickHouseContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(ClickHouseContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(ClickHouseContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddClickHouseContainerAddsAnnotationMetadata()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddClickHouse("clickhouse", port: 12345);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<ClickHouseServerResource>());
        Assert.Equal("clickhouse", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(8123, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("http", endpoint.Name);
        Assert.Equal(12345, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ClickHouseContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(ClickHouseContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(ClickHouseContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task ClickHouseCreatesConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();
        var clickhouse = builder.AddClickHouse("clickhouse")
                                 .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 1234));

        var connectionStringResource = clickhouse.Resource as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal("Host={clickhouse.bindings.http.host};Protocol=http;Port={clickhouse.bindings.http.port};Username=default;Password={clickhouse-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal($"Host=localhost;Protocol=http;Port=1234;Username=default;Password={clickhouse.Resource.PasswordParameter.Value}", connectionString);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        var builder = DistributedApplication.CreateBuilder();
        var clickHouse = builder.AddClickHouse("clickhouse");
        var db = clickHouse.AddDatabase("mydatabase");

        var clickHouseManifest = await ManifestUtils.GetManifest(clickHouse.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var actualManifest = clickHouseManifest.ToString();

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={clickhouse.bindings.http.host};Protocol=http;Port={clickhouse.bindings.http.port};Username=default;Password={clickhouse-password.value}",
              "image": "docker.io/clickhouse/clickhouse-server:24.4",
              "env": {
                "CLICKHOUSE_USER": "default",
                "CLICKHOUSE_PASSWORD": "{clickhouse-password.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8123
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, actualManifest);

        var actualDbManifest = dbManifest.ToString();

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{clickhouse.connectionString};Database=mydatabase"
            }
            """;
        Assert.Equal(expectedManifest, actualDbManifest);
    }
}
