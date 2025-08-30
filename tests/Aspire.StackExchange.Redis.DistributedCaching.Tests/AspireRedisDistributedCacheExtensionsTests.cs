// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.StackExchange.Redis.DistributedCaching.Tests;

public class AspireRedisDistributedCacheExtensionsTests
{
    [Fact]
    public void AddsRedisDistributedCacheCorrectly()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddRedisDistributedCache("redis");

        using var host = builder.Build();
        var cache = host.Services.GetRequiredService<IDistributedCache>();

        Assert.IsAssignableFrom<RedisCache>(cache);
    }

    [Fact]
    public void AddKeyedRedisDistributedCache_WithName_RegistersKeyedService()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddKeyedRedisDistributedCache("redis2");

        using var host = builder.Build();
        var keyedCache = host.Services.GetRequiredKeyedService<IDistributedCache>("redis2");

        Assert.IsAssignableFrom<RedisCache>(keyedCache);
    }

    [Fact]
    public void AddKeyedRedisDistributedCache_WithServiceKey_RegistersKeyedServiceWithPrefix()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddKeyedRedisDistributedCache("redis2", "redis3");

        using var host = builder.Build();
        var keyedCache = host.Services.GetRequiredKeyedService<IDistributedCache>("redis3");

        Assert.IsAssignableFrom<RedisCache>(keyedCache);
    }

    [Fact]
    public void CanRegisterMultipleDistributedCacheInstances()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        // Register non-keyed service
        builder.AddRedisDistributedCache("redis1");
        // Register keyed service with name as service key
        builder.AddKeyedRedisDistributedCache("redis2");
        // Register keyed service with custom service key
        builder.AddKeyedRedisDistributedCache("redis2", "redis3");

        using var host = builder.Build();

        // Verify non-keyed service
        var cache1 = host.Services.GetRequiredService<IDistributedCache>();
        Assert.IsAssignableFrom<RedisCache>(cache1);

        // Verify keyed service with name
        var cache2 = host.Services.GetRequiredKeyedService<IDistributedCache>("redis2");
        Assert.IsAssignableFrom<RedisCache>(cache2);

        // Verify keyed service with custom service key
        var cache3 = host.Services.GetRequiredKeyedService<IDistributedCache>("redis3");
        Assert.IsAssignableFrom<RedisCache>(cache3);

        // Verify they are different instances
        Assert.NotSame(cache1, cache2);
        Assert.NotSame(cache1, cache3);
        Assert.NotSame(cache2, cache3);
    }

    [Fact]
    public void KeyedRedisDistributedCache_WithServiceKey_ThrowsWhenServiceKeyIsNull()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        Assert.Throws<ArgumentNullException>(() =>
            builder.AddKeyedRedisDistributedCache("redis", serviceKey: null!));
    }

    [Fact]
    public void KeyedRedisDistributedCache_WithServiceKey_ThrowsWhenServiceKeyIsEmpty()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        Assert.Throws<ArgumentException>(() =>
            builder.AddKeyedRedisDistributedCache("redis", serviceKey: ""));
    }
}
