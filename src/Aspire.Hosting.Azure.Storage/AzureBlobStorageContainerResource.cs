// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents an Azure Blob Storage container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="blobStorage">The <see cref="AzureBlobStorageResource"/> that the resource is stored in.</param>
public class AzureBlobStorageContainerResource(string name, AzureBlobStorageResource blobStorage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBlobStorageResource>,
    IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Blob Storage container resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.GetConnectionString(Name);

    /// <summary>
    /// Gets the parent <see cref="AzureBlobStorageResource"/> of this <see cref="AzureBlobStorageContainerResource"/>.
    /// </summary>
    public AzureBlobStorageResource Parent => blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));

    internal void ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
        => Parent.ApplyAzureFunctionsConfiguration(target, connectionName, Name);

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.Storage.BlobContainer"/> instance.</returns>
    internal global::Azure.Provisioning.Storage.BlobContainer ToProvisioningEntity()
    {
        global::Azure.Provisioning.Storage.BlobContainer blobContainer = new(Infrastructure.NormalizeBicepIdentifier(Name));

        if (Name is not null)
        {
            blobContainer.Name = Name;
        }

        return blobContainer;
    }

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
        => ApplyAzureFunctionsConfiguration(target, connectionName);
}
