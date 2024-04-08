// Assembly 'Aspire.StackExchange.Redis.OutputCaching'

using System;
using Aspire.StackExchange.Redis;
using Microsoft.AspNetCore.OutputCaching.StackExchangeRedis;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

public static class AspireRedisOutputCacheExtensions
{
    public static void AddRedisOutputCache(this IHostApplicationBuilder builder, string connectionName, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
    public static void AddKeyedRedisOutputCache(this IHostApplicationBuilder builder, string name, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
}
