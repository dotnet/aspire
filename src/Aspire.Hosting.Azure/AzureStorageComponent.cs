// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureStorageComponent(string name) : DistributedApplicationComponent(name), IAzureComponent
{
    public string? TableUri { get; set; }
    public string? QueueUri { get; set; }
    public string? BlobUri { get; set; }
}

public class AzureTableStorageComponent(string name, AzureStorageComponent storage) : DistributedApplicationComponent(name),
    IAzureComponent,
    IDistributedApplicationComponentWithConnectionString,
    IDistributedApplicationComponentWithParent<AzureStorageComponent>
{
    public AzureStorageComponent Parent => storage;

    public string? GetConnectionString() => Parent.TableUri;
}

public class AzureBlobStorageComponent(string name, AzureStorageComponent storage) : DistributedApplicationComponent(name),
    IAzureComponent,
    IDistributedApplicationComponentWithConnectionString,
    IDistributedApplicationComponentWithParent<AzureStorageComponent>
{
    public AzureStorageComponent Parent => storage;

    public string? GetConnectionString() => Parent.BlobUri;
}

public class AzureQueueStorageComponent(string name, AzureStorageComponent storage) : DistributedApplicationComponent(name),
    IAzureComponent,
    IDistributedApplicationComponentWithConnectionString,
    IDistributedApplicationComponentWithParent<AzureStorageComponent>
{
    public AzureStorageComponent Parent => storage;

    public string? GetConnectionString() => Parent.QueueUri;
}
