// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Redis;

public class AddRedisTests
{
    [Fact]
    public void AddRedisContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisResource>());
        Assert.Equal("myRedis", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(RedisContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(RedisContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(RedisContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddRedisContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis", port: 9813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisResource>());
        Assert.Equal("myRedis", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(9813, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(RedisContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(RedisContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(RedisContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task RedisCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.Equal("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith("localhost:2000", connectionString);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("redis");

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 6379
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis");
        if (isReadOnly.HasValue)
        {
            redis.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            redis.WithDataVolume();
        }

        var volumeAnnotation = redis.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal("Aspire.Hosting.Tests-myRedis-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis");
        if (isReadOnly.HasValue)
        {
            redis.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            redis.WithDataBindMount("mydata");
        }

        var volumeAnnotation = redis.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void WithDataVolumeAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                              .WithDataVolume();

        Assert.True(redis.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.Equal("--save 60 1".Split(" "), args);
    }

    [Fact]
    public void WithDataVolumeDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume(isReadOnly: true);

        var persistenceAnnotation = redis.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata");

        Assert.True(redis.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.Equal("--save 60 1".Split(" "), args);
    }

    [Fact]
    public void WithDataBindMountDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata", isReadOnly: true);

        var persistenceAnnotation = redis.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume()
                           .WithPersistence(TimeSpan.FromSeconds(10), 2);

        Assert.True(redis.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.Equal("--save 10 2".Split(" "), args);
    }

    [Fact]
    public void WithPersistenceAddsCommandLineArgsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithPersistence(TimeSpan.FromSeconds(60));

        Assert.True(redis.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsAnnotations));
        Assert.NotNull(argsAnnotations.SingleOrDefault());
    }
}
