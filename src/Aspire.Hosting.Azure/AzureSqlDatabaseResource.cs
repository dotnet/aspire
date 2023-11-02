// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure SQL Database resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="server">The <see cref="AzureSqlServerResource"/> that the resource is stored in.</param>
public class AzureSqlDatabaseResource(string name, AzureSqlServerResource server) : Resource(name),
    IAzureResource,
    IResourceWithConnectionString,
    IResourceWithParent<AzureSqlServerResource>
{
    /// <summary>
    /// Gets the parent AzureSqlServerResource of this AzureSqlDatabaseResource.
    /// </summary>
    public AzureSqlServerResource Parent => server;

    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the connection string for the Azure SQL Database resource.
    /// </summary>
    /// <returns>The connection string for the Azure SQL Database resource.</returns>
    public string? GetConnectionString() => ConnectionString ?? Parent.GetConnectionString(Name);
}
