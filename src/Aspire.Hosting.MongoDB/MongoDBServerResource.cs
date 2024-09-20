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

    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var builder = new ReferenceExpressionBuilder();

            AppendConnectionString(builder);
            AppendSuffix(builder);

            return builder.Build();
        }
    }

    internal void AppendConnectionString(ReferenceExpressionBuilder builder)
    {
        builder.AppendLiteral("mongodb://");
        builder.AppendFormatted(PrimaryEndpoint.Property(EndpointProperty.Host));
        builder.AppendLiteral(":");
        builder.AppendFormatted(PrimaryEndpoint.Property(EndpointProperty.Port));
    }

    /// <summary>
    /// Handles adding the rest of the connection string.
    ///
    /// - If a database name is provided, it will be appended to the connection string.
    /// - If a replica set name is provided, it will be appended to the connection string.
    /// - If no database but a replica set is provided, a '/' must be inserted before the '?'
    /// </summary>
    internal bool AppendSuffix(ReferenceExpressionBuilder builder, string? dbName = null)
    {
        if (dbName is { })
        {
            builder.AppendLiteral("/");
            builder.AppendFormatted(dbName);
        }

        if (Annotations.OfType<MongoDbReplicaSetAnnotation>().FirstOrDefault() is { ReplicaSetName: { } replicaSetName })
        {
            if (dbName is null)
            {
                builder.AppendLiteral("/");
            }

            builder.AppendLiteral("?");
            builder.AppendLiteral(MongoDbReplicaSetAnnotation.QueryName);
            builder.AppendLiteral("=");
            builder.AppendLiteral(replicaSetName);

            return true;
        }

        return false;
    }

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
