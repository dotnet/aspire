// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis connection.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="connectionString">The connection string for the resource.</param>
public class RedisResource(string name, string? connectionString) : Resource(name), IRedisResource
{
    /// <summary>
    /// Gets the connection string for the Redis server.
    /// </summary>
    /// <returns>The specified connection string.</returns>
    public string? GetConnectionString() => connectionString;
}
