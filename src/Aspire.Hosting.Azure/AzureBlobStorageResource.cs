// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Blob Storage account.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureBlobStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Blob Storage resource.
    /// </summary>
    public string ConnectionStringExpression => Parent.BlobEndpoint.ValueExpression;

    /// <summary>
    /// Gets the connection string for the Azure Blob Storage resource.
    /// </summary>
    /// <returns>The connection string for the Azure Blob Storage resource.</returns>
    public string? GetConnectionString() => Parent.GetBlobConnectionString();

    /// <summary>
    /// Called by manifest publisher to write manifest resource.
    /// </summary>
    /// <param name="context">The context for the manifest publishing operation.</param>
    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "value.v0");
        context.WriteConnectionString(this);
    }
}
