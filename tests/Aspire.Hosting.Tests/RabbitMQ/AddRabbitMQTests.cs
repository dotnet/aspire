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
    public async Task RabbitMQCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:pass"] = "pass1";

        var pass = appBuilder.AddParameter("pass");
        appBuilder
            .AddRabbitMQ("rabbit", password: pass)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27011));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var rabbitMqResource = Assert.Single(appModel.Resources.OfType<RabbitMQServerResource>());
        var connectionStringResource = rabbitMqResource as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);

        Assert.Equal("amqp://guest:pass1@localhost:27011", connectionString);
        Assert.Equal("amqp://guest:{pass.value}@{rabbit.bindings.tcp.host}:{rabbit.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistrubtedApplicationBuilder.Create();
        var rabbit = builder.AddRabbitMQ("rabbit");

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
                      "minLength": 22,
                      "special": false
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithParameters()
    {
        using var builder = TestDistrubtedApplicationBuilder.Create();

        var userNameParameter = builder.AddParameter("user");
        var passwordParameter = builder.AddParameter("pass");

        var rabbit = builder.AddRabbitMQ("rabbit", userNameParameter, passwordParameter);
        var manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        var expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "amqp://{user.value}:{pass.value}@{rabbit.bindings.tcp.host}:{rabbit.bindings.tcp.port}",
              "image": "rabbitmq:3",
              "env": {
                "RABBITMQ_DEFAULT_USER": "{user.value}",
                "RABBITMQ_DEFAULT_PASS": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 5672
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        rabbit = builder.AddRabbitMQ("rabbit2", userNameParameter);
        manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "amqp://{user.value}:{rabbit2.inputs.password}@{rabbit2.bindings.tcp.host}:{rabbit2.bindings.tcp.port}",
              "image": "rabbitmq:3",
              "env": {
                "RABBITMQ_DEFAULT_USER": "{user.value}",
                "RABBITMQ_DEFAULT_PASS": "{rabbit2.inputs.password}"
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
                      "minLength": 22,
                      "special": false
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        rabbit = builder.AddRabbitMQ("rabbit3", password: passwordParameter);
        manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "amqp://guest:{pass.value}@{rabbit3.bindings.tcp.host}:{rabbit3.bindings.tcp.port}",
              "image": "rabbitmq:3",
              "env": {
                "RABBITMQ_DEFAULT_USER": "guest",
                "RABBITMQ_DEFAULT_PASS": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 5672
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
