// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class MongoDBServerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the MongoDB server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    private ReferenceExpression ConnectionString =>
        ReferenceExpression.Create(
            $"mongodb://{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");

    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ConnectionString;

    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns>A connection string for the MongoDB server in the form "mongodb://host:port".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken) =>
        ConnectionString.GetValueAsync(cancellationToken);

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
