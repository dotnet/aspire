// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Oracle Database database. This is a child resource of a <see cref="OracleDatabaseServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The Oracle Database parent resource associated with this database.</param>
public class OracleDatabaseResource(string name, OracleDatabaseServerResource parent) : Resource(name), IResourceWithParent<OracleDatabaseServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent Oracle container resource.
    /// </summary>
    public OracleDatabaseServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string for the Oracle Database.
    /// </summary>
    /// <returns>A connection string for the Oracle Database.</returns>
    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return $"{connectionString}/{Name}";
        }
        else
        {
            throw new DistributedApplicationException("Parent resource connection string was null.");
        }
    }
}
