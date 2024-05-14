// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Milvus;

/// <summary>
/// A resource that represents a Milvus database.
/// </summary>
public class MilvusServerResource : ContainerResource, IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="apiKey">A <see cref="ParameterResource"/> that contains the authentication apiKey/token</param>
    public MilvusServerResource(string name, ParameterResource apiKey) : base(name)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        ApiKeyParameter = apiKey;
    }

    internal const string PrimaryEndpointName = "grpc";
    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the parameter that contains the Qdrant API key.
    /// </summary>
    public ParameterResource ApiKeyParameter { get; }

    /// <summary>
    /// Gets the gRPC endpoint for the Milvus database.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Milvus gRPC endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create(
            $"Endpoint={PrimaryEndpoint.Property(EndpointProperty.Url)};Key={ApiKeyParameter}");
}
