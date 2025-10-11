// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a PostgreSQL database. This is a child resource of a <see cref="PostgresServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="postgresParentResource">The PostgreSQL parent resource associated with this database.</param>
public class PostgresDatabaseResource(string name, string databaseName, PostgresServerResource postgresParentResource)
    : Resource(name), IResourceWithParent<PostgresServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent PostgresSQL container resource.
    /// </summary>
    public PostgresServerResource Parent { get; } = postgresParentResource ?? throw new ArgumentNullException(nameof(postgresParentResource));

    /// <summary>
    /// Gets the connection string expression for the Postgres database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var connectionStringBuilder = new DbConnectionStringBuilder
            {
                ["Database"] = DatabaseName
            };

            return ReferenceExpression.Create($"{Parent};{connectionStringBuilder.ToString()}");
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

    /// <summary>
    /// Gets the connection URI expression for the PostgreSQL database.
    /// </summary>
    /// <remarks>
    /// Format: <c>postgresql://{user}:{password}@{host}:{port}/{database}</c>.
    /// </remarks>
    public ReferenceExpression UriExpression =>
        ReferenceExpression.Create($"{Parent.UriExpression}/{DatabaseName:uri}");

    /// <summary>
    /// Gets the JDBC connection string for the PostgreSQL database.
    /// </summary>
    /// <remarks>
    /// Format: <c>jdbc:postgresql://{host}:{port}/{database}?user={user}&amp;password={password}</c>.
    /// </remarks>
    public ReferenceExpression JdbcConnectionString => Parent.BuildJdbcConnectionString(DatabaseName);

    IEnumerable<KeyValuePair<string, ReferenceExpression>> IResourceWithConnectionString.GetConnectionProperties() =>
        ((IResourceWithConnectionString)Parent).GetConnectionProperties()
            .Union([
                new ("Database", ReferenceExpression.Create($"{DatabaseName}")),
                new ("Uri", UriExpression),
                new ("JdbcConnectionString", JdbcConnectionString),
            ]);
}
