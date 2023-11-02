// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure App Configuration resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureAppConfigurationResource(string name) : Resource(name), IAzureResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the endpoint for the Azure App Configuration resource.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets the connection string for the Azure App Configuration resource.
    /// </summary>
    /// <returns>The connection string for the Azure App Configuration resource.</returns>
    public string? GetConnectionString() => Endpoint;
}
