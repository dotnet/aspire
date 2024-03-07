// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Nats;

public class AddNatsTests
{
    [Fact]
    public void AddNatsContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddNats("nats");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<NatsServerResource>());
        Assert.Equal("nats", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(4222, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("2", containerAnnotation.Tag);
        Assert.Equal("nats", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void AddNatsContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddNats("nats", 1234).WithJetStream(srcMountPath: "/tmp/dev-data");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<NatsServerResource>());
        Assert.Equal("nats", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var mountAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal("/tmp/dev-data", mountAnnotation.Source);
        Assert.Equal("/data", mountAnnotation.Target);

        var argsAnnotation = Assert.Single(containerResource.Annotations.OfType<CommandLineArgsCallbackAnnotation>());
        Assert.NotNull(argsAnnotation.Callback);
        var args = new List<string>();
        argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        Assert.Equal("-js -sd /data".Split(' '), args);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(4222, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(1234, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("2", containerAnnotation.Tag);
        Assert.Equal("nats", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void WithNatsContainerOnMultipleResources()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNats("nats1");
        builder.AddNats("nats2");

        Assert.Equal(2, builder.Resources.OfType<NatsServerResource>().Count());
    }

    [Fact]
    public async Task VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var nats = appBuilder.AddNats("nats");

        var manifest = await ManifestUtils.GetManifest(nats.Resource);

        var expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "nats://{nats.bindings.tcp.host}:{nats.bindings.tcp.port}",
              "image": "nats:2",
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 4222
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
