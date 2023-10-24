// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a PostgreSQL database. This is a child resource of a <see cref="PostgresContainerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="postgresContainer">The PostgreSQL server resource associated with this database.</param>
public class PostgresDatabaseResource(string name, PostgresContainerResource postgresContainer) : Resource(name), IPostgresResource, IResourceWithParent<PostgresContainerResource>
{
    public PostgresContainerResource Parent { get; } = postgresContainer;

    /// <summary>
    /// Gets the connection string for the Postgres database.
    /// </summary>
    /// <returns>A connection string for the Postgres database.</returns>
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
