// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a NATS server container.
/// </summary>
/// <param name="name">The name of the resource.</param>

public class NatsServerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string expression for the NATS server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression => $"nats://{{{Name}.bindings.tcp.host}}:{{{Name}.bindings.tcp.port}}";

    /// <summary>
    /// Gets the connection string (NATS_URL) for the NATS server.
    /// </summary>
    /// <returns>A connection string for the NATS server in the form "nats://host:port".</returns>

    public string GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var endpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        return string.Join(",", endpoints.Select(e => $"nats://{e.EndPointString}"));
    }
}
