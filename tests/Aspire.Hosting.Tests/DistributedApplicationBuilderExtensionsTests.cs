// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests;

public class DistributedApplicationBuilderExtensionsTests
{
    [Fact]
    public void CreateResourceBuilderByNameRequiresExistingResource()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var missingException = Assert.Throws<InvalidOperationException>(() => appBuilder.CreateResourceBuilder<RedisResource>("non-existent-resource"));
        Assert.Contains("not found", missingException.Message);
    }

    [Fact]
    public void CreateResourceBuilderByNameRequiresCompatibleType()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var originalRedis = appBuilder.AddRedis("redis");
        var incorrectTypeException = Assert.Throws<InvalidOperationException>(() => appBuilder.CreateResourceBuilder<PostgresServerResource>("redis"));
        Assert.Contains("not assignable", incorrectTypeException.Message);
    }

    [Fact]
    public void CreateResourceBuilderByNameSupportsUpCast()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var originalRedis = appBuilder.AddRedis("redis");

        // RedisResource implements ContainerResource, so this is acceptable.
        var newRedisBuilder = appBuilder.CreateResourceBuilder<ContainerResource>("redis");
        Assert.Same(originalRedis.Resource, newRedisBuilder.Resource);
    }

    [Fact]
    public void CreateResourceBuilderByReturnsSameResourceInstance()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var originalRedis = appBuilder.AddRedis("redis");
        var newRedisBuilder = appBuilder.CreateResourceBuilder<RedisResource>("redis");
        Assert.Same(originalRedis.Resource, newRedisBuilder.Resource);
    }
}
