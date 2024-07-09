// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias AspireHostingShared;

using Aspire.Hosting.Redis;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

//using static AspireHostingShared::Aspire.Hosting.Utils.RedisCommander;
//using Xunit;

namespace Aspire.Hosting.Tests.Utils;

public class RedisCommanderTests
{
    [Fact]
    public void WithRedisCommanderSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisCommander(c => {
            c.WithHostPort(1000);
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Fact]
    public void WithRedisCommanderSupportsChangingHostPortForGarnet()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddGarnet("mygarnet").WithRedisCommander(c => {
            c.WithHostPort(1000);
        });

        var resource = Assert.Single(builder.Resources.OfType<Hosting.Garnet.RedisCommanderResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Fact]
    public void WithRedisCommanderSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisCommander(c => {
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
    public void WithRedisCommanderSupportsChangingContainerImageValuesForGarnet()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddGarnet("mygarnet").WithRedisCommander(c => {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.Single(builder.Resources.OfType<Hosting.Garnet.RedisCommanderResource>());
        var containerAnnotation = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("example.mycompany.com", containerAnnotation.Registry);
        Assert.Equal("customrediscommander", containerAnnotation.Image);
        Assert.Equal("someothertag", containerAnnotation.Tag);
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

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal($"myredis1:{containerHost}:5001:0,myredis2:host2:5002:0", config["REDIS_HOSTS"]);
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task MultipleRedisInstanceProducesCorrectRedisHostsVariableForGarnet(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var garnet1 = builder.AddGarnet("mygarnet1").WithRedisCommander();
        var garnet2 = builder.AddGarnet("mygarnet2").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        garnet1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));
        garnet2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new Hosting.Garnet.RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal($"mygarnet1:{containerHost}:5001:0,mygarnet2:host2:5002:0", config["GARNET_HOSTS"]);
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

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal($"myredis1:{containerHost}:5001:0", config["REDIS_HOSTS"]);
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task SingleRedisInstanceProducesCorrectRedisHostsVariableForGarnet(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var garnet = builder.AddGarnet("mygarnet1").WithRedisCommander();
        await using var app = builder.Build();

        // Add fake allocated endpoints.
        garnet.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new Hosting.Garnet.RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal($"mygarnet1:{containerHost}:5001:0", config["GARNET_HOSTS"]);
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
    public void WithRedisCommanderAddsRedisCommanderResourceForGarnet()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddGarnet("mygarnet1").WithRedisCommander();
        builder.AddGarnet("mygarnet2").WithRedisCommander();

        Assert.Single(builder.Resources.OfType<Hosting.Garnet.RedisCommanderResource>());
    }
}
