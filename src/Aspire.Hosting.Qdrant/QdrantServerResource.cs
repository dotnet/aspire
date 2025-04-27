// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Qdrant database.
/// </summary>
public class QdrantServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "grpc";
    internal const string HttpEndpointName = "http";

    /// <summary>
    /// Initializes a new instance of the <see cref="QdrantServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="apiKey">A <see cref="ParameterResource"/> that contains the API Key</param>
    public QdrantServerResource(string name, ParameterResource apiKey) : base(name)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        ApiKeyParameter = apiKey;
    }

    private EndpointReference? _primaryEndpoint;
    private EndpointReference? _httpEndpoint;

    /// <summary>
    /// Gets the parameter that contains the Qdrant API key.
    /// </summary>
    public ParameterResource ApiKeyParameter { get; }

    /// <summary>
    /// Gets the gRPC endpoint for the Qdrant database.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the HTTP endpoint for the Qdrant database.
    /// </summary>
    public EndpointReference HttpEndpoint => _httpEndpoint ??= new(this, HttpEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Qdrant gRPC endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Interpolate(
            $"Endpoint={PrimaryEndpoint.Property(EndpointProperty.Url)};Key={ApiKeyParameter}");

    /// <summary>
    /// Gets the connection string expression for the Qdrant HTTP endpoint.
    /// </summary>
    public ReferenceExpression HttpConnectionStringExpression =>
        ReferenceExpression.Interpolate(
            $"Endpoint={HttpEndpoint.Property(EndpointProperty.Url)};Key={ApiKeyParameter}");
}
