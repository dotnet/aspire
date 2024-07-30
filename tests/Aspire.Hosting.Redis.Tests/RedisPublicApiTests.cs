// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Redis.Tests;

public class RedisPublicApiTests
{
    #region RedisBuilderExtensions

    [Fact]
    public void AddRedisContainerShouldThrowsWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Redis";

        var action = () => builder.AddRedis(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void AddRedisContainerShouldThrowsWhenNameIsNull()
    {
        IDistributedApplicationBuilder builder = new DistributedApplicationBuilder([]);
        string name = null!;

        var action = () => builder.AddRedis(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Fact]
    public void WithRedisCommanderShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () => builder.WithRedisCommander();

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithHostPortShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<RedisCommanderResource> builder = null!;
        const int port = 777;

        var action = () => builder.WithHostPort(port);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataVolumeShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () => builder.WithDataVolume();

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataBindMountShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;
        const string source = "/data";

        var action = () => builder.WithDataBindMount(source);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataBindMountShouldThrowsWhenNameIsNull()
    {
        var distributedApplicationBuilder = new DistributedApplicationBuilder([]);
        const string name = "Redis";
        var resource = new RedisResource(name);
        var builder = distributedApplicationBuilder.AddResource(resource);
        string source = null!;

        var action = () => builder.WithDataBindMount(source);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(source), exception.ParamName);
        });
    }

    [Fact]
    public void WithPersistenceShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () => builder.WithPersistence();

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    #endregion

    #region RedisCommanderConfigWriterHook

    [Fact]
    public async Task AfterEndpointsAllocatedAsyncShouldThrowsWhenDistributedApplicationModelIsNull()
    {
        DistributedApplicationModel appModel = null!;
        var cancellationToken = CancellationToken.None;

        var instance = (RedisCommanderConfigWriterHook)Activator.CreateInstance(typeof(RedisCommanderConfigWriterHook), true)!;

        async Task Action() => await instance.AfterEndpointsAllocatedAsync(appModel, cancellationToken);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(Action);
        Assert.Equal(nameof(appModel), exception.ParamName);
    }

    #endregion

    #region RedisCommanderResource

    [Fact]
    public void CtorRedisCommanderResourceShouldThrowsWhenNameIsNull()
    {
        string name = null!;

        var action = () => new RedisCommanderResource(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    #endregion

    #region RedisResource

    [Fact]
    public void CtorRedisResourceShouldThrowsWhenNameIsNull()
    {
        string name = null!;

        var action = () => new RedisResource(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    #endregion
}
