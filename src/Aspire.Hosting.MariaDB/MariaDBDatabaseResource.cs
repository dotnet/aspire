// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MariaDB database. This is a child resource of a <see cref="MariaDBServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The MariaDB parent resource associated with this database.</param>
public class MariaDBDatabaseResource(string name, string databaseName, MariaDBServerResource parent) : Resource(name), IResourceWithParent<MariaDBServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent MariaDB container resource.
    /// </summary>
    public MariaDBServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string expression for the MariaDB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;
}
