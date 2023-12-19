// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a SQL Server database resource that is a child of a SQL Server container resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="sqlServerContainer">The parent SQL Server container resource.</param>
public class SqlServerDatabaseResource(string name, ISqlServerParentResource sqlServerContainer) : Resource(name), ISqlServerParentResource, IResourceWithParent<ISqlServerParentResource>
{
    /// <summary>
    /// Gets the parent SQL Server container resource.
    /// </summary>
    public ISqlServerParentResource Parent { get; } = sqlServerContainer;

    /// <summary>
    /// Gets the connection string for the database resource.
    /// </summary>
    /// <returns>The connection string for the database resource.</returns>
    /// <exception cref="DistributedApplicationException">Thrown when the parent resource connection string is null.</exception>
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
