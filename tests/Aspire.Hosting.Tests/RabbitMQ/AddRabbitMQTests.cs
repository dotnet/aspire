// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;
using Xunit;

namespace Aspire.Hosting.Tests.RabbitMQ;

public class AddRabbitMQTests
{
    [Fact]
    public void AddRabbitMQContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddRabbitMQ("rabbit");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RabbitMQServerResource>());
        Assert.Equal("rabbit", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5672, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("3", containerAnnotation.Tag);
        Assert.Equal("rabbitmq", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void RabbitMQCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddRabbitMQ("rabbit")
            .WithAnnotation(
                new AllocatedEndpointAnnotation("mybinding",
                ProtocolType.Tcp,
                "localhost",
                27011,
                "tcp"
            ));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<RabbitMQServerResource>());
        var connectionString = connectionStringResource.GetConnectionString();
        var password = connectionStringResource.Password;

        Assert.Equal($"amqp://guest:{password}@localhost:27011", connectionString);
        Assert.Equal("amqp://guest:{rabbit.inputs.password}@{rabbit.bindings.tcp.host}:{rabbit.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var rabbit = appBuilder.AddRabbitMQ("rabbit");

        var manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        var expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "amqp://guest:{rabbit.inputs.password}@{rabbit.bindings.tcp.host}:{rabbit.bindings.tcp.port}",
              "image": "rabbitmq:3",
              "env": {
                "RABBITMQ_DEFAULT_USER": "guest",
                "RABBITMQ_DEFAULT_PASS": "{rabbit.inputs.password}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 5672
                }
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 10
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
