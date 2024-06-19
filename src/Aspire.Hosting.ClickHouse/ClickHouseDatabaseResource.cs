// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a ClickHouse database. This is a child resource of a <see cref="ClickHouseServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="clickHouseParentResource">The ClickHouse parent resource associated with this database.</param>
public class ClickHouseDatabaseResource(string name, string databaseName, ClickHouseServerResource clickHouseParentResource) : Resource(name), IResourceWithParent<ClickHouseServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent ClickHouse container resource.
    /// </summary>
    public ClickHouseServerResource Parent { get; } = clickHouseParentResource;

    /// <summary>
    /// Gets the connection string expression for the ClickHouse database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;
}

