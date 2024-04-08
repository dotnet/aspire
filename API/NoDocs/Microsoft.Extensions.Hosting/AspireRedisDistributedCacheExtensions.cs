// Assembly 'Aspire.StackExchange.Redis.DistributedCaching'

using System;
using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

public static class AspireRedisDistributedCacheExtensions
{
    public static void AddRedisDistributedCache(this IHostApplicationBuilder builder, string connectionName, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
    public static void AddKeyedRedisDistributedCache(this IHostApplicationBuilder builder, string name, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
}
