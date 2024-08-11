// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Redis.Tests;

public class RedisPublicApiTests
{
    [Fact]
    public void AddRedisContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Redis";

        var action = () => builder.AddRedis(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddRedisContainerShouldThrowWhenNameIsNull()
    {
        IDistributedApplicationBuilder builder = new DistributedApplicationBuilder([]);
        string name = null!;

        var action = () => builder.AddRedis(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithRedisCommanderShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () => builder.WithRedisCommander();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithHostPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RedisCommanderResource> builder = null!;
        const int port = 777;

        var action = () => builder.WithHostPort(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;
        const string source = "/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenNameIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("Redis");
        string source = null!;

        var action = () => redis.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithPersistenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () => builder.WithPersistence();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public async Task AfterEndpointsAllocatedAsyncShouldThrowWhenDistributedApplicationModelIsNull()
    {
        DistributedApplicationModel appModel = null!;
        var cancellationToken = CancellationToken.None;

        var instance = (RedisCommanderConfigWriterHook)Activator.CreateInstance(typeof(RedisCommanderConfigWriterHook), true)!;

        async Task Action() => await instance.AfterEndpointsAllocatedAsync(appModel, cancellationToken);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(Action);
        Assert.Equal(nameof(appModel), exception.ParamName);
    }

    [Fact]
    public void CtorRedisCommanderResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;

        var action = () => new RedisCommanderResource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorRedisResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;

        var action = () => new RedisResource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
