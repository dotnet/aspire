// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Oracle Database database. This is a child resource of a <see cref="OracleDatabaseServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The Oracle Database parent resource associated with this database.</param>
public class OracleDatabaseResource(string name, string databaseName, OracleDatabaseServerResource parent) : Resource(name), IResourceWithParent<OracleDatabaseServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent Oracle container resource.
    /// </summary>
    public OracleDatabaseServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string expression for the Oracle Database.
    /// </summary>
    public string ConnectionStringExpression => $"{{{Parent.Name}.connectionString}}/{DatabaseName}";

    /// <summary>
    /// Gets the connection string for the Oracle Database.
    /// </summary>
    /// <returns>A connection string for the Oracle Database.</returns>
    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return $"{connectionString}/{DatabaseName}";
        }
        else
        {
            throw new DistributedApplicationException("Parent resource connection string was null.");
        }
    }

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
