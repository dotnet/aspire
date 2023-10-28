// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AddRedisTests
{
    [Fact]
    public void AddRedisContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedisContainer("myRedis");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisContainerResource>());
        Assert.Equal("myRedis", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<ServiceBindingAnnotation>());
        Assert.Equal(6379, serviceBinding.ContainerPort);
        Assert.False(serviceBinding.IsExternal);
        Assert.Equal("tcp", serviceBinding.Name);
        Assert.Null(serviceBinding.Port);
        Assert.Equal(ProtocolType.Tcp, serviceBinding.Protocol);
        Assert.Equal("tcp", serviceBinding.Transport);
        Assert.Equal("tcp", serviceBinding.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("redis", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void AddRedisContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedisContainer("myRedis", 9813);

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisContainerResource>());
        Assert.Equal("myRedis", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<ServiceBindingAnnotation>());
        Assert.Equal(6379, serviceBinding.ContainerPort);
        Assert.False(serviceBinding.IsExternal);
        Assert.Equal("tcp", serviceBinding.Name);
        Assert.Equal(9813, serviceBinding.Port);
        Assert.Equal(ProtocolType.Tcp, serviceBinding.Protocol);
        Assert.Equal("tcp", serviceBinding.Transport);
        Assert.Equal("tcp", serviceBinding.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("redis", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void RedisCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedisContainer("myRedis")
            .WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = connectionStringResource.GetConnectionString();
        Assert.StartsWith("localhost:2000", connectionString);
    }

    [Fact]
    public void AddRedisAddsMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis", "endpoint");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.Equal("endpoint", connectionStringResource.GetConnectionString());
        Assert.Equal("myRedis", connectionStringResource.Name);

        var manifestAnnotation = Assert.Single(connectionStringResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);
    }
}
