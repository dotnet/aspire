// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server database that is a child of a SQL Server container resource.
/// </summary>
public class SqlServerDatabaseResource : Resource, IResourceWithParent<SqlServerServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent SQL Server container resource.
    /// </summary>
    public SqlServerServerResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the SQL Server database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; }

    /// <param name="name">The name of the resource.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="parent">The parent SQL Server server resource.</param>
    public SqlServerDatabaseResource(string name, string databaseName, SqlServerServerResource parent) : base(name)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNull(parent);

        DatabaseName = databaseName;
        Parent = parent;

    }
}
