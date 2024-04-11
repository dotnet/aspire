// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Tests.Utils;
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

    [Fact]
    public void WithRedisCommanderAddsRedisCommanderResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis1").WithRedisCommander();
        builder.AddRedis("myredis2").WithRedisCommander();

        Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task SingleRedisInstanceProducesCorrectRedisHostsVariable(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("myredis1").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.Equal($"myredis1:{containerHost}:5001:0", config["REDIS_HOSTS"]);
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task MultipleRedisInstanceProducesCorrectRedisHostsVariable(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis1 = builder.AddRedis("myredis1").WithRedisCommander();
        var redis2 = builder.AddRedis("myredis2").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));
        redis2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.Equal($"myredis1:{containerHost}:5001:0,myredis2:host2:5002:0", config["REDIS_HOSTS"]);
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

        Assert.Equal("testhost-myRedis-data", volumeAnnotation.Source);
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

        var persistenceAnnotation = redis.Resource.Annotations.OfType<RedisPersistenceCommandLineArgsCallbackAnnotation>().Single();

        Assert.Equal(TimeSpan.FromSeconds(60), persistenceAnnotation.Interval);
        Assert.Equal(1, persistenceAnnotation.KeysChangedThreshold);
    }

    [Fact]
    public void WithDataVolumeDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume(isReadOnly: true);

        var persistenceAnnotation = redis.Resource.Annotations.OfType<RedisPersistenceCommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata");

        var persistenceAnnotation = redis.Resource.Annotations.OfType<RedisPersistenceCommandLineArgsCallbackAnnotation>().Single();

        Assert.Equal(TimeSpan.FromSeconds(60), persistenceAnnotation.Interval);
        Assert.Equal(1, persistenceAnnotation.KeysChangedThreshold);
    }

    [Fact]
    public void WithDataBindMountDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata", isReadOnly: true);

        var persistenceAnnotation = redis.Resource.Annotations.OfType<RedisPersistenceCommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume()
                           .WithPersistence(TimeSpan.FromSeconds(10), 2);

        var persistenceAnnotation = redis.Resource.Annotations.OfType<RedisPersistenceCommandLineArgsCallbackAnnotation>().Single();

        Assert.Equal(TimeSpan.FromSeconds(10), persistenceAnnotation.Interval);
        Assert.Equal(2, persistenceAnnotation.KeysChangedThreshold);
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
