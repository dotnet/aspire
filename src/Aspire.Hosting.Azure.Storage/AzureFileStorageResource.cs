// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure File Storage service within a storage account.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureFileStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IResourceWithParent<AzureStorageResource>,
    IAzurePrivateEndpointTarget
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureFileStorageResource.
    /// </summary>
    public AzureStorageResource Parent => storage ?? throw new ArgumentNullException(nameof(storage));

    BicepOutputReference IAzurePrivateEndpointTarget.Id => Parent.Id;

    IEnumerable<string> IAzurePrivateEndpointTarget.GetPrivateLinkGroupIds() => ["file"];

    string IAzurePrivateEndpointTarget.GetPrivateDnsZoneName() => "privatelink.file.core.windows.net";
}
