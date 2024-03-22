// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;
using Xunit;

namespace Aspire.Hosting.Tests.Kafka;
public class AddKafkaTests
{
    [Fact]
    public void AddKafkaContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddKafka("kafka");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<KafkaServerResource>());
        Assert.Equal("kafka", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(9092, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("7.6.0", containerAnnotation.Tag);
        Assert.Equal("confluentinc/confluent-local", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public async Task KafkaCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddKafka("kafka")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<KafkaServerResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal("localhost:27017", connectionString);
        Assert.Equal("{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var appBuilder = TestDistrubtedApplicationBuilder.Create();

        var kafka = appBuilder.AddKafka("kafka");

        var manifest = await ManifestUtils.GetManifest(kafka.Resource);

        var expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port}",
              "image": "confluentinc/confluent-local:7.6.0",
              "env": {
                "KAFKA_ADVERTISED_LISTENERS": "PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:9092"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 9092
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
