// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure SQL Database resource.
/// </summary>
public class AzureSqlDatabaseResource : Resource,
    IResourceWithConnectionString,
    IResourceWithParent<AzureSqlServerResource>
{
    /// <summary>
    /// Represents an Azure SQL Database resource.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="server">The <see cref="AzureSqlServerResource"/> that the resource is stored in.</param>
    public AzureSqlDatabaseResource(string name, AzureSqlServerResource server) : base(name)
    {
        Parent = server;
        Parent.AddDatabase(this);
    }

    /// <summary>
    /// Gets the parent AzureSqlServerResource of this AzureSqlDatabaseResource.
    /// </summary>
    public AzureSqlServerResource Parent { get; }

    /// <summary>
    /// Gets or sets the connection string for the Azure SQL Database resource.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the connection string for the Azure SQL Database resource.
    /// </summary>
    /// <returns>The connection string for the Azure SQL Database resource.</returns>
    public string? GetConnectionString() => ConnectionString
                                            ?? $"Server=tcp:{Parent.Hostname},1433;Initial Catalog={Name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";
}
