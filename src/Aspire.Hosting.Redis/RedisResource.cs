// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis resource independent of the hosting model.
/// </summary>
public class RedisResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <param name="name">The name of the resource.</param>
    public RedisResource(string name) : base(name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="resourceAnnotations">The annotations associated with the resource.</param>
    public RedisResource(string name, ResourceAnnotationCollection resourceAnnotations) : base(name, resourceAnnotations)
    {
    }

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    private ReferenceExpression ConnectionString =>
        ReferenceExpression.Create(
            $"{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");

    /// <summary>
    /// Gets the connection string expression for the Redis server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return ConnectionString;
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

        return ConnectionString.GetValueAsync(cancellationToken);
    }
}
