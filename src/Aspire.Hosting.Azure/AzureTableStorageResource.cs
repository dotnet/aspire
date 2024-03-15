// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

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
    /// Gets the connection string template for the manifest for the Azure Table Storage resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Parent.GetTableConnectionString();

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "value.v0");
        context.WriteConnectionString(this);
    }
}

/// <summary>
/// Represents an Azure Table Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureTableStorageConstructResource(string name, AzureStorageConstructResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageConstructResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureTableStorageResource.
    /// </summary>
    public AzureStorageConstructResource Parent => storage;

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Table Storage resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Parent.GetTableConnectionString();

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "value.v0");
        context.WriteConnectionString(this);
    }
}
