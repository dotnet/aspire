// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Oracle Database database. This is a child resource of a <see cref="OracleDatabaseContainerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="oracleParentResource">The Oracle Database parent resource associated with this database.</param>
public class OracleDatabaseResource(string name, IOracleDatabaseParentResource oracleParentResource) : Resource(name), IResourceWithParent<IOracleDatabaseParentResource>, IResourceWithConnectionString
{
    public IOracleDatabaseParentResource Parent { get; } = oracleParentResource;

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
