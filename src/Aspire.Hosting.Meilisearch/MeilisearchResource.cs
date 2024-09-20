// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Meilisearch
/// </summary>
public class MeilisearchResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";

    /// <param name="name">The name of the resource.</param>
    /// <param name="masterKey">A parameter that contains the Meilisearch master key.</param>
    public MeilisearchResource(string name, ParameterResource masterKey) : base(name)
    {
        ArgumentNullException.ThrowIfNull(masterKey);
        MasterKeyParameter = masterKey;
    }

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Meilisearch. This endpoint is used for all API calls over HTTP.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the parameter that contains the Meilisearch superuser password.
    /// </summary>
    public ParameterResource MasterKeyParameter { get; }

    /// <summary>
    /// Gets the connection string expression for the Meilisearch
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint=http://{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)};MasterKey={MasterKeyParameter}");
}

