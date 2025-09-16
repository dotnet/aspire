// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Aspire.StackExchange.Redis;

/// <summary>
/// Provides a builder for configuring Redis client services using StackExchange.Redis in an Aspire application.
/// </summary>
/// <param name="hostBuilder">The <see cref="IHostApplicationBuilder"/> with which services are being registered.</param>
/// <param name="settings">The <see cref="StackExchangeRedisSettings"/> to configure the Redis client.</param>
/// <param name="serviceKey">The service key used to register the <see cref="IConnectionMultiplexer"/> service, if any.</param>
public sealed class AspireRedisClientBuilder(IHostApplicationBuilder hostBuilder, StackExchangeRedisSettings settings, string? serviceKey)
{
    /// <summary>
    /// Gets the <see cref="IHostApplicationBuilder"/> with which services are being registered.
    /// </summary>
    public IHostApplicationBuilder HostBuilder { get; } = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

    /// <summary>
    /// Gets the <see cref="StackExchangeRedisSettings"/> used to configure the Redis client.
    /// </summary>
    public StackExchangeRedisSettings Settings { get; } = settings;

    /// <summary>
    /// Gets the service key used to register the <see cref="IConnectionMultiplexer"/> service, if any.
    /// </summary>
    public string? ServiceKey { get; } = serviceKey;
}
