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
public class OracleDatabaseResource(string name, string databaseName, OracleDatabaseServerResource parent) : Resource(ThrowIfNullOrEmpty(name)), IResourceWithParent<OracleDatabaseServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent Oracle container resource.
    /// </summary>
    public OracleDatabaseServerResource Parent { get; } = ThrowIfNullOrEmpty(parent);

    /// <summary>
    /// Gets the connection string expression for the Oracle Database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create($"{Parent}/{DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNullOrEmpty(databaseName);

    private static T ThrowIfNullOrEmpty<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);

        if (argument is not string str)
        {
            return argument;
        }

        ArgumentException.ThrowIfNullOrEmpty(str, paramName);
        return argument;

    }
}
