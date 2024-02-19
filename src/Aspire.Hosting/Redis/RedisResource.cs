// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string for the Redis server.
    /// </summary>
    /// <returns>A connection string for the redis server in the form "host:port".</returns>
    public string? GetConnectionString()
    {
        var annotation = this.Annotations.OfType<ConnectionStringCallbackAnnotation>().Single();
        return annotation.Callback();
    }
}
