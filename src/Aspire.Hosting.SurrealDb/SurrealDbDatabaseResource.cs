// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SurrealDB database that is a child of a SurrealDB container resource.
/// </summary>
public class SurrealDbDatabaseResource : Resource, IResourceWithParent<SurrealDbServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent SurrealDB container resource.
    /// </summary>
    public SurrealDbServerResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the SurrealDB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Namespace={NamespaceName};Database={DatabaseName}");

    /// <summary>
    /// Gets the namespace name.
    /// </summary>
    public string NamespaceName { get; }

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurrealDbDatabaseResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="parent">The parent SQL Server server resource.</param>
    public SurrealDbDatabaseResource(string name, string namespaceName, string databaseName, SurrealDbServerResource parent) : base(name)
    {
        ArgumentException.ThrowIfNullOrEmpty(namespaceName);
        ArgumentException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNull(parent);

        NamespaceName = namespaceName;
        DatabaseName = databaseName;
        Parent = parent;
    }
}
