    // Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Milvus;

/// <summary>
/// A resource that represents a Milvus database.
/// </summary>
public class MilvusServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "grpc";

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

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the parameter that contains the Milvus API key.
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
       ReferenceExpression.Interpolate(
            $"Endpoint={PrimaryEndpoint.Property(EndpointProperty.Url)};Key=root:{ApiKeyParameter}");

    private readonly Dictionary<string, string> _databases = new Dictionary<string, string>(StringComparers.ResourceName);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }
}
