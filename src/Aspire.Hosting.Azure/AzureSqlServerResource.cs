// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Sql Server resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureSqlServerResource(string name) : Resource(name), IAzureResource
{
    /// <summary>
    /// Gets or sets the hostname of the Azure SQL Server resource.
    /// </summary>
    public string? Hostname { get; set; }

    internal string? GetConnectionString(string database) =>
        $"Server=tcp:{Hostname},1433;Initial Catalog={database};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";
}