// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SurrealDB database that is a child of a SurrealDB namespace resource.
/// </summary>
public class SurrealDbDatabaseResource : Resource, IResourceWithParent<SurrealDbNamespaceResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent SurrealDB namespace resource.
    /// </summary>
    public SurrealDbNamespaceResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the SurrealDB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurrealDbDatabaseResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="parent">The parent SurrealDB namespace resource.</param>
    public SurrealDbDatabaseResource(string name, string databaseName, SurrealDbNamespaceResource parent) : base(name)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNull(parent);

        DatabaseName = databaseName;
        Parent = parent;
    }
}
