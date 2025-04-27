// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server database that is a child of a SQL Server container resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The parent SQL Server server resource.</param>
public class SqlServerDatabaseResource(string name, string databaseName, SqlServerServerResource parent)
    : Resource(name), IResourceWithParent<SqlServerServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent SQL Server container resource.
    /// </summary>
    public SqlServerServerResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the connection string expression for the SQL Server database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                ["Database"] = DatabaseName
            };

            return ReferenceExpression.Interpolate($"{Parent};{connectionStringBuilder.ToString()}");
        }
    }

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNullOrEmpty(databaseName);

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
