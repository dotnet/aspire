// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Queue Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureQueueStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureQueueStorageResource.
    /// </summary>
    public AzureStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Queue Storage resource.
    /// </summary>
    public string ConnectionStringExpression => Parent.QueueEndpoint.ValueExpression;

    /// <summary>
    /// Gets the connection string for the Azure Queue Storage resource.
    ///</summary>
    ///<returns> The connection string for the Azure Queue Storage resource.</returns>
    public string? GetConnectionString() => Parent.GetQueueConnectionString();

    /// <summary>
    /// Gets the connection string for the Azure Queue Storage resource.
    ///</summary>
    ///<param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    ///<returns> The connection string for the Azure Queue Storage resource.</returns>
    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (Parent.ProvisioningTaskCompletionSource is not null)
        {
            await Parent.ProvisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return GetConnectionString();
    }

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "value.v0");
        context.WriteConnectionString(this);
    }
}

/// <summary>
/// Represents an Azure Queue Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureQueueStorageConstructResource(string name, AzureStorageConstructResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageConstructResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureQueueStorageResource.
    /// </summary>
    public AzureStorageConstructResource Parent => storage;

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Queue Storage resource.
    /// </summary>
    public string ConnectionStringExpression => Parent.QueueEndpoint.ValueExpression;

    /// <summary>
    /// Gets the connection string for the Azure Queue Storage resource.
    ///</summary>
    ///<returns> The connection string for the Azure Queue Storage resource.</returns>
    public string? GetConnectionString() => Parent.GetQueueConnectionString();

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "value.v0");
        context.WriteConnectionString(this);
    }
}
