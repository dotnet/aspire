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

        var host = builder.Build();
        var cache = host.Services.GetRequiredService<IDistributedCache>();

        Assert.IsAssignableFrom<RedisCache>(cache);
    }
}
