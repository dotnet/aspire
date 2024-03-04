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
    /// Gets the connection string for the Azure Blob Storage resource.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string for the Azure Blob Storage resource.</returns>
    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (Parent.ProvisioningTaskCompletionSource is not null)
        {
            await Parent.ProvisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return GetConnectionString();
    }

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

/// <summary>
/// A resource that represents an Azure Blob Storage account.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureBlobStorageConstructResource(string name, AzureStorageConstructResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageConstructResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureStorageConstructResource Parent => storage;

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
    /// Gets the connection string for the Azure Blob Storage resource.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string for the Azure Blob Storage resource.</returns>
    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (Parent.ProvisioningTaskCompletionSource is not null)
        {
            await Parent.ProvisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return GetConnectionString();
    }

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
