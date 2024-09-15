// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SurrealDB database that is a child of a SurrealDB container resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="namespaceName">The namespace name.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The parent SurrealDB server resource.</param>
public class SurrealDbDatabaseResource(string name, string namespaceName, string databaseName, SurrealDbServerResource parent) : Resource(name), IResourceWithParent<SurrealDbServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent SurrealDB container resource.
    /// </summary>
    public SurrealDbServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string expression for the SurrealDB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Namespace={NamespaceName};Database={DatabaseName}");

    /// <summary>
    /// Gets the namespace name.
    /// </summary>
    public string NamespaceName { get; } = namespaceName;

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;
}
