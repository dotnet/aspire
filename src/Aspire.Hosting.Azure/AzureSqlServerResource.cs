// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Sql Server resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureSqlServerResource(string name) : Resource(name), IAzureResource
{
    private readonly Collection<AzureSqlDatabaseResource> _databases = new();

    /// <summary>
    /// Gets or sets the hostname of the Azure SQL Server resource.
    /// </summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// Gets the list of databases of the Azure SQL Server resource.
    /// </summary>
    public IReadOnlyCollection<AzureSqlDatabaseResource> Databases => _databases;

    internal void AddDatabase(AzureSqlDatabaseResource database)
    {
        if (database.Parent != this)
        {
            throw new ArgumentException("Database belongs to another server", nameof(database));
        }
        _databases.Add(database);
    }
}
