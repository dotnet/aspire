// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Nats;
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

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(4222, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(NatsContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(NatsContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(NatsContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddNatsContainerAddsAnnotationMetadata()
    {
        var path = OperatingSystem.IsWindows() ? @"C:\tmp\dev-data" : "/tmp/dev-data";

        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddNats("nats", 1234).WithJetStream().WithDataBindMount(path);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<NatsServerResource>());
        Assert.Equal("nats", containerResource.Name);

        var mountAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal(path, mountAnnotation.Source);
        Assert.Equal("/var/lib/nats", mountAnnotation.Target);

        var argsAnnotations = containerResource.Annotations.OfType<CommandLineArgsCallbackAnnotation>();

        var args = new List<object>();
        foreach (var argsAnnotation in argsAnnotations)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));

        }
        Assert.Equal("-js -sd /var/lib/nats".Split(' '), args);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(4222, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(1234, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(NatsContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(NatsContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(NatsContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void WithNatsContainerOnMultipleResources()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddNats("nats1");
        builder.AddNats("nats2");

        Assert.Equal(2, builder.Resources.OfType<NatsServerResource>().Count());
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var nats = builder.AddNats("nats");

        var manifest = await ManifestUtils.GetManifest(nats.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "nats://{nats.bindings.tcp.host}:{nats.bindings.tcp.port}",
              "image": "{{NatsContainerImageTags.Registry}}/{{NatsContainerImageTags.Image}}:{{NatsContainerImageTags.Tag}}",
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 4222
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
