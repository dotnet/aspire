// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Table Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureTableStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureTableStorageResource.
    /// </summary>
    public AzureStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string for the Azure Table Storage resource.
    /// </summary>
    /// <returns>The connection string for the Azure Table Storage resource.</returns>
    public string? GetConnectionString() => Parent.GetTableConnectionString();
}
