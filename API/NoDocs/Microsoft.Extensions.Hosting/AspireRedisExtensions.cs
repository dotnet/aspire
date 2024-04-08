// Assembly 'Aspire.StackExchange.Redis'

using System;
using System.Collections.Generic;
using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Configuration;

namespace Microsoft.Extensions.Hosting;

public static class AspireRedisExtensions
{
    public static void AddRedisClient(this IHostApplicationBuilder builder, string connectionName, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
    public static void AddKeyedRedisClient(this IHostApplicationBuilder builder, string name, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
}
