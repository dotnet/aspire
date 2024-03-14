// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server database that is a child of a SQL Server container resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The parent SQL Server server resource.</param>
public class SqlServerDatabaseResource(string name, string databaseName, SqlServerServerResource parent) : Resource(name), IResourceWithParent<SqlServerServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent SQL Server container resource.
    /// </summary>
    public SqlServerServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string expression for the SQL Server database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "value.v0");
        context.WriteConnectionString(this);
    }
}
