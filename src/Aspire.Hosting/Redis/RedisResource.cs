// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Redis server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return $"{PrimaryEndpoint.GetValueExpression(EndpointProperty.Host)}:{PrimaryEndpoint.GetValueExpression(EndpointProperty.Port)}";
        }
    }

    /// <summary>
    /// Gets the connection string for the Redis server.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the redis server in the form "host:port".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        return new(GetConnectionString());
    }

    /// <summary>
    /// Gets the connection string for the Redis server.
    /// </summary>
    /// <returns>A connection string for the redis server in the form "host:port".</returns>
    public string? GetConnectionString()
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionString();
        }

        return $"{PrimaryEndpoint.GetValue(EndpointProperty.Host)}:{PrimaryEndpoint.GetValue(EndpointProperty.Port)}";
    }
}
