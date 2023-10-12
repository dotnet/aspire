// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureStorageResource(string name) : DistributedApplicationResource(name), IAzureResource
{
    public Uri? TableUri { get; set; }
    public Uri? QueueUri { get; set; }
    public Uri? BlobUri { get; set; }
}

public class AzureTableStorageResource(string name, AzureStorageResource storage) : DistributedApplicationResource(name),
    IAzureResource,
    IDistributedApplicationResourceWithConnectionString,
    IDistributedApplicationResourceWithParent<AzureStorageResource>
{
    public AzureStorageResource Parent => storage;

    public string? GetConnectionString(IDistributedApplicationResource? targetResource) => Parent.TableUri?.ToString();
}

public class AzureBlobStorageResource(string name, AzureStorageResource storage) : DistributedApplicationResource(name),
    IAzureResource,
    IDistributedApplicationResourceWithConnectionString,
    IDistributedApplicationResourceWithParent<AzureStorageResource>
{
    public AzureStorageResource Parent => storage;

    public string? GetConnectionString(IDistributedApplicationResource? targetResource) => Parent.BlobUri?.ToString();
}

public class AzureQueueStorageResource(string name, AzureStorageResource storage) : DistributedApplicationResource(name),
    IAzureResource,
    IDistributedApplicationResourceWithConnectionString,
    IDistributedApplicationResourceWithParent<AzureStorageResource>
{
    public AzureStorageResource Parent => storage;

    public string? GetConnectionString(IDistributedApplicationResource? targetResource) => Parent.QueueUri?.ToString();
}
