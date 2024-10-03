// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents a PostgreSQL database. This is a child resource of a <see cref="AzurePostgresFlexibleServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="postgresParentResource">The Azure PostgreSQL parent resource associated with this database.</param>
public class AzurePostgresFlexibleServerDatabaseResource(string name, string databaseName, AzurePostgresFlexibleServerResource postgresParentResource)
    : Resource(ThrowIfNull(name)), IResourceWithParent<AzurePostgresFlexibleServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent Azure PostgresSQL resource.
    /// </summary>
    public AzurePostgresFlexibleServerResource Parent { get; } = ThrowIfNull(postgresParentResource);

    /// <summary>
    /// Gets the connection string expression for the Postgres database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.GetDatabaseConnectionString(Name, databaseName);

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNull(databaseName);

    private static T ThrowIfNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        => argument ?? throw new ArgumentNullException(paramName);
}
