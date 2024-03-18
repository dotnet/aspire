// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Components.Common;
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
        Assert.Equal(6379, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ContainerImageTags.Redis.Tag, containerAnnotation.Tag);
        Assert.Equal(ContainerImageTags.Redis.Image, containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
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
        Assert.Equal(6379, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(9813, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ContainerImageTags.Redis.Tag, containerAnnotation.Tag);
        Assert.Equal(ContainerImageTags.Redis.Image, containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
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
        var appBuilder = DistributedApplication.CreateBuilder();
        var redis = appBuilder.AddRedis("redis");

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port}",
              "image": "{{ContainerImageTags.Redis.Image}}:{{ContainerImageTags.Redis.Tag}}",
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 6379
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

    [Fact]
    public async Task SingleRedisInstanceProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("myredis1").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "host.docker.internal", 5001));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.Equal("myredis1:host.docker.internal:5001:0", config["REDIS_HOSTS"]);
    }

    [Fact]
    public async Task MultipleRedisInstanceProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis1 = builder.AddRedis("myredis1").WithRedisCommander();
        var redis2 = builder.AddRedis("myredis2").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "host.docker.internal", 5001));
        redis2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "host.docker.internal", 5002));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.Equal("myredis1:host.docker.internal:5001:0,myredis2:host.docker.internal:5002:0", config["REDIS_HOSTS"]);
    }
}
