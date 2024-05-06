// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ActiveMQ;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.ActiveMQ;

public class AddActiveMQTests
{
    [Fact]
    public void AddActiveMQContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = TestDistributedApplicationBuilder.Create();

        appBuilder.AddActiveMQ("activemq");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<ActiveMQServerResource>());
        Assert.Equal("activemq", containerResource.Name);

        var primaryEndpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>().Where(e => e.Name == "tcp"));
        Assert.Equal(61616, primaryEndpoint.TargetPort);
        Assert.False(primaryEndpoint.IsExternal);
        Assert.Equal("tcp", primaryEndpoint.Name);
        Assert.Null(primaryEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.Equal("tcp", primaryEndpoint.Transport);
        Assert.Equal("tcp", primaryEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ActiveMQContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(ActiveMQContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task ActiveMQCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:pass"] = "p@ssw0rd1";

        var pass = appBuilder.AddParameter("pass");
        appBuilder
            .AddActiveMQ("activemq", password: pass)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27011));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var activeMqResource = Assert.Single(appModel.Resources.OfType<ActiveMQServerResource>());
        var connectionStringResource = activeMqResource as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);

        Assert.Equal("tcp://admin:p@ssw0rd1@localhost:27011", connectionString);
        Assert.Equal("tcp://admin:{pass.value}@{activemq.bindings.tcp.host}:{activemq.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var rabbit = builder.AddActiveMQ("activemq");
        var manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        var expectedTag = ActiveMQContainerImageTags.Tag;
        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "tcp://admin:{activemq-password.value}@{activemq.bindings.tcp.host}:{activemq.bindings.tcp.port}",
              "image": "{{ActiveMQContainerImageTags.Registry}}/{{ActiveMQContainerImageTags.Image}}:{{expectedTag}}",
              "env": {
                "ACTIVEMQ_CONNECTION_USER": "admin",
                "ACTIVEMQ_CONNECTION_PASSWORD": "{activemq-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 61616
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var userNameParameter = builder.AddParameter("user");
        var passwordParameter = builder.AddParameter("pass");

        var activemq = builder.AddActiveMQ("activemq", userNameParameter, passwordParameter);
        var manifest = await ManifestUtils.GetManifest(activemq.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "tcp://{user.value}:{pass.value}@{activemq.bindings.tcp.host}:{activemq.bindings.tcp.port}",
              "image": "{{ActiveMQContainerImageTags.Registry}}/{{ActiveMQContainerImageTags.Image}}:{{ActiveMQContainerImageTags.Tag}}",
              "env": {
                "ACTIVEMQ_CONNECTION_USER": "{user.value}",
                "ACTIVEMQ_CONNECTION_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 61616
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        activemq = builder.AddActiveMQ("activemq2", userNameParameter);
        manifest = await ManifestUtils.GetManifest(activemq.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "tcp://{user.value}:{activemq2-password.value}@{activemq2.bindings.tcp.host}:{activemq2.bindings.tcp.port}",
              "image": "{{ActiveMQContainerImageTags.Registry}}/{{ActiveMQContainerImageTags.Image}}:{{ActiveMQContainerImageTags.Tag}}",
              "env": {
                "ACTIVEMQ_CONNECTION_USER": "{user.value}",
                "ACTIVEMQ_CONNECTION_PASSWORD": "{activemq2-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 61616
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        activemq = builder.AddActiveMQ("activemq3", password: passwordParameter);
        manifest = await ManifestUtils.GetManifest(activemq.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "tcp://admin:{pass.value}@{activemq3.bindings.tcp.host}:{activemq3.bindings.tcp.port}",
              "image": "{{ActiveMQContainerImageTags.Registry}}/{{ActiveMQContainerImageTags.Image}}:{{ActiveMQContainerImageTags.Tag}}",
              "env": {
                "ACTIVEMQ_CONNECTION_USER": "admin",
                "ACTIVEMQ_CONNECTION_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 61616
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
