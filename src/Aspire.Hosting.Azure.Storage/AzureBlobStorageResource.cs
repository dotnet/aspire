// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Blob Storage account.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureBlobStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>,
    IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureStorageResource Parent => storage ?? throw new ArgumentNullException(nameof(storage));

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Blob Storage resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       Parent.GetBlobConnectionString();

    internal ReferenceExpression GetConnectionString(string? blobContainerName)
    {
        if (string.IsNullOrEmpty(blobContainerName))
        {
            return ConnectionStringExpression;
        }

        ReferenceExpressionBuilder builder = new();

        if (Parent.IsEmulator)
        {
            builder.AppendFormatted(ConnectionStringExpression);
        }
        else
        {
            builder.Append($"Endpoint={ConnectionStringExpression}");
        }

        builder.Append($";ContainerName={blobContainerName}");

        return builder.Build();
    }

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (Parent.IsEmulator)
        {
            var connectionString = Parent.GetBlobConnectionString();
            target[connectionName] = connectionString;
            // Injected to support Aspire client integration for Azure Storage.
            target[$"{AzureStorageResource.BlobsConnectionKeyPrefix}__{connectionName}__ConnectionString"] = connectionString;
        }
        else
        {
            // Injected to support Azure Functions listener initialization and bookkeeping.
            // We inject both the blob service and queue service URIs since Functions
            // uses the queue service for its internal bookkeeping on blob triggers.
            target[$"{connectionName}__blobServiceUri"] = Parent.BlobEndpoint;
            target[$"{connectionName}__queueServiceUri"] = Parent.QueueEndpoint;

            // Injected to support Aspire client integration for Azure Storage.
            // We don't inject the queue resource here since we on;y want it to
            // be accessible by the Functions host.
            target[$"{AzureStorageResource.BlobsConnectionKeyPrefix}__{connectionName}__ServiceUri"] = Parent.BlobEndpoint;
        }
    }

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.Storage.BlobService"/> instance.</returns>
    internal global::Azure.Provisioning.Storage.BlobService ToProvisioningEntity()
    {
        global::Azure.Provisioning.Storage.BlobService service = new(Infrastructure.NormalizeBicepIdentifier(Name));
        return service;
    }
}
