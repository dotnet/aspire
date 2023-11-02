// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Redis resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureRedisResource(string name) : Resource(name), IAzureResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the connection string for the Azure Redis resource.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the connection string for the Azure Redis resource.
    /// </summary>
    /// <returns>The connection string for the Azure Redis resource.</returns>
    public string? GetConnectionString() => ConnectionString;
}
