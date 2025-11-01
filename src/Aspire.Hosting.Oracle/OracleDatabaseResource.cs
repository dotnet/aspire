// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Oracle Database database. This is a child resource of a <see cref="OracleDatabaseServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The Oracle Database parent resource associated with this database.</param>
public class OracleDatabaseResource(string name, string databaseName, OracleDatabaseServerResource parent)
    : Resource(name), IResourceWithParent<OracleDatabaseServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent Oracle container resource.
    /// </summary>
    public OracleDatabaseServerResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the connection string expression for the Oracle Database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create($"{Parent}/{DatabaseName}");

    /// <summary>
    /// Gets the connection URI expression for the Oracle database.
    /// </summary>
    /// <remarks>
    /// Format: <c>oracle://{user}:{password}@{host}:{port}/{database}</c>.
    /// </remarks>
    public ReferenceExpression UriExpression =>
        ReferenceExpression.Create($"{Parent.UriExpression}/{DatabaseName:uri}");

    /// <summary>
    /// Gets the JDBC connection string for the Oracle Database.
    /// </summary>
    /// <remarks>
    /// Format: <c>jdbc:oracle:thin:{user}/{password}@//{host}:{port}/{database}</c>.
    /// </remarks>
    public ReferenceExpression JdbcConnectionString => Parent.BuildJdbcConnectionString(DatabaseName);

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNullOrEmpty(databaseName);

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }

    IEnumerable<KeyValuePair<string, ReferenceExpression>> IResourceWithConnectionString.GetConnectionProperties() =>
        Parent.CombineProperties([
            new("Database", ReferenceExpression.Create($"{DatabaseName}")),
            new("Uri", ReferenceExpression.Create($"{UriExpression}")),
            new("JdbcConnectionString", JdbcConnectionString),
        ]);
}
