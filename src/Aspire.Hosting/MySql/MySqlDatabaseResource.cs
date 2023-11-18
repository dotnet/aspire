// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL database. This is a child resource of a <see cref="MySqlContainerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="mySqlContainer">The MySQL server resource associated with this database.</param>
public class MySqlDatabaseResource(string name, MySqlContainerResource mySqlContainer) : Resource(name), IMySqlResource, IResourceWithParent<MySqlContainerResource>
{
    public MySqlContainerResource Parent { get; } = mySqlContainer;

    /// <summary>
    /// Gets the connection string for the MySQL database.
    /// </summary>
    /// <returns>A connection string for the MySQL database.</returns>
    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return $"{connectionString}Database={Name}";
        }
        else
        {
            throw new DistributedApplicationException("Parent resource connection string was null.");
        }
    }
}
