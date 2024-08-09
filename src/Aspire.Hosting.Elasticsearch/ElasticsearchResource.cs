// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Elasticsearch
/// </summary>
public class ElasticsearchResource : ContainerResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the Elasticsearch container superuser name.
    /// </summary>
    private const string UserName = "elastic";

    // this endpoint is used for all API calls over HTTP.
    // This includes search and aggregations, monitoring and anything else that uses a HTTP request.
    // All client libraries will use this port to talk to Elasticsearch
    internal const string PrimaryEndpointName = "http";

    //this endpoint is a custom binary protocol used for communications between nodes in a cluster.
    //For things like cluster updates, master elections, nodes joining/leaving, shard allocation
    internal const string InternalEndpointName = "internal";

    /// <param name="name">The name of the resource.</param>
    /// <param name="password">A parameter that contains the Elasticsearch superuser password.</param>
    public ElasticsearchResource(string name, ParameterResource password) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);
        PasswordParameter = password;
    }

    private EndpointReference? _primaryEndpoint;
    private EndpointReference? _internalEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Elasticsearch. This endpoint is used for all API calls over HTTP.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the internal endpoint for the Elasticsearch. This endpoint used for communications between nodes in a cluster
    /// </summary>
    public EndpointReference InternalEndpoint => _internalEndpoint ??= new(this, InternalEndpointName);

    /// <summary>
    /// Gets the parameter that contains the Elasticsearch superuser password.
    /// </summary>
    public ParameterResource PasswordParameter { get; }

    /// <summary>
    /// Gets the connection string expression for the Elasticsearch
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"http://{UserName}:{PasswordParameter}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");
}

