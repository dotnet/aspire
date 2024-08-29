// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Redis.Tests;

public class AddRedisStackStackTests
{
    [Fact]
    public void AddRedisStackContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedisStack("myRedis").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisStackResource>());
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
        Assert.Equal(RedisContainerImageTags.RedisStackServerTag, containerAnnotation.Tag);
        Assert.Equal(RedisContainerImageTags.RedisStackServerImage, containerAnnotation.Image);
        Assert.Equal(RedisContainerImageTags.RedisStackServerRegistry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddRedisStackContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedisStack("myRedis", port: 9813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisStackResource>());
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
        Assert.Equal(RedisContainerImageTags.RedisStackServerTag, containerAnnotation.Tag);
        Assert.Equal(RedisContainerImageTags.RedisStackServerImage, containerAnnotation.Image);
        Assert.Equal(RedisContainerImageTags.RedisStackServerRegistry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task RedisStackCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedisStack("myRedis")
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
        var redis = builder.AddRedisStack("redis");

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port}",
              "image": "{{RedisContainerImageTags.RedisStackServerRegistry}}/{{RedisContainerImageTags.RedisStackServerImage}}:{{RedisContainerImageTags.RedisStackServerTag}}",
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
        builder.AddRedisStack("myredis1").WithRedisCommander();
        builder.AddRedisStack("myredis2").WithRedisCommander();

        Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
    }

    [Fact]
    public void WithRedisCommanderSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedisStack("myredis").WithRedisCommander(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
        var containerAnnotation = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("example.mycompany.com", containerAnnotation.Registry);
        Assert.Equal("customrediscommander", containerAnnotation.Image);
        Assert.Equal("someothertag", containerAnnotation.Tag);
    }

    [Fact]
    public void WithRedisCommanderSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedisStack("myredis").WithRedisCommander(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task SingleRedisStackInstanceProducesCorrectRedisStackHostsVariable(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedisStack("myredis1").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            commander,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

        Assert.Equal($"myredis1:{containerHost}:5001:0", config["REDIS_HOSTS"]);
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task MultipleRedisStackInstanceProducesCorrectRedisStackHostsVariable(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis1 = builder.AddRedisStack("myredis1").WithRedisCommander();
        var redis2 = builder.AddRedisStack("myredis2").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));
        redis2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            commander,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

        Assert.Equal($"myredis1:{containerHost}:5001:0,myredis2:host2:5002:0", config["REDIS_HOSTS"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedisStack("myRedis");
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
        var redis = builder.AddRedisStack("myRedis");
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
    public async Task WithDataVolumeAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddRedisStack("myRedis")
               .WithDataVolume();

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redis = Assert.Single(appModel.Resources.OfType<RedisStackResource>());

        var config = await redis.GetEnvironmentVariableValuesAsync();

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("REDIS_ARGS", env.Key);
                Assert.Equal("--save 60 1", env.Value);
            });
    }

    [Fact]
    public async Task WithDataVolumeDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddRedisStack("myRedis")
               .WithDataVolume(isReadOnly: true);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redis = Assert.Single(appModel.Resources.OfType<RedisStackResource>());

        var config = await redis.GetEnvironmentVariableValuesAsync();

        Assert.Empty(config);
    }

    [Fact]
    public async Task WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddRedisStack("myRedis")
               .WithDataBindMount("myredisdata");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redis = Assert.Single(appModel.Resources.OfType<RedisStackResource>());

        var config = await redis.GetEnvironmentVariableValuesAsync();

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("REDIS_ARGS", env.Key);
                Assert.Equal("--save 60 1", env.Value);
            });
    }

    [Fact]
    public async Task WithDataBindMountDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddRedisStack("myRedis")
               .WithDataBindMount("myredisdata", isReadOnly: true);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redis = Assert.Single(appModel.Resources.OfType<RedisStackResource>());

        var config = await redis.GetEnvironmentVariableValuesAsync();

        Assert.Empty(config);
    }

    [Fact]
    public async Task WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddRedisStack("myRedis")
               .WithDataVolume()
               .WithPersistence(TimeSpan.FromSeconds(10), 2);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redis = Assert.Single(appModel.Resources.OfType<RedisStackResource>());

        var config = await redis.GetEnvironmentVariableValuesAsync();

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("REDIS_ARGS", env.Key);
                Assert.Equal("--save 10 2", env.Value);
            });
    }

    [Fact]
    public async Task WithPersistenceAddsEnvironmentVariable()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddRedisStack("myRedis")
               .WithPersistence(TimeSpan.FromSeconds(60));
        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redis = Assert.Single(appModel.Resources.OfType<RedisStackResource>());

        var config = await redis.GetEnvironmentVariableValuesAsync();

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("REDIS_ARGS", env.Key);
                Assert.Equal("--save 60 1", env.Value);
            });
    }
}
