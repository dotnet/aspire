// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureConstruct">Callback to populate the construct with Azure resources.</param>
public class AzureStorageResource(string name, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(name, configureConstruct),
    IResourceWithEndpoints,
    IResourceWithAzureFunctionsConfig
{
    private EndpointReference EmulatorBlobEndpoint => new(this, "blob");
    private EndpointReference EmulatorQueueEndpoint => new(this, "queue");
    private EndpointReference EmulatorTableEndpoint => new(this, "table");

    internal const string BlobsConnectionKeyPrefix = "Aspire__Azure__Storage__Blobs";
    internal const string QueuesConnectionKeyPrefix = "Aspire__Azure__Storage__Queues";

    /// <summary>
    /// Gets the "blobEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference BlobEndpoint => new("blobEndpoint", this);

    /// <summary>
    /// Gets the "queueEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference QueueEndpoint => new("queueEndpoint", this);

    /// <summary>
    /// Gets the "tableEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference TableEndpoint => new("tableEndpoint", this);

    /// <summary>
    /// Gets a value indicating whether the Azure Storage resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the connection string for the Azure Storage emulator.
    /// </summary>
    /// <returns></returns>
    internal ReferenceExpression GetEmulatorConnectionString() => IsEmulator
       ? AzureStorageEmulatorConnectionString.Create(blobEndpoint: EmulatorBlobEndpoint, queueEndpoint: EmulatorQueueEndpoint, tableEndpoint: EmulatorTableEndpoint)
       : throw new InvalidOperationException("The Azure Storage resource is not running in the local emulator.");

    internal ReferenceExpression GetTableConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(tableEndpoint: EmulatorTableEndpoint)
        : ReferenceExpression.Create($"{TableEndpoint}");

    internal ReferenceExpression GetQueueConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(queueEndpoint: EmulatorQueueEndpoint)
        : ReferenceExpression.Create($"{QueueEndpoint}");

    internal ReferenceExpression GetBlobConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(blobEndpoint: EmulatorBlobEndpoint)
        : ReferenceExpression.Create($"{BlobEndpoint}");

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (IsEmulator)
        {
            // Injected to support Azure Functions listener initialization.
            var connectionString = GetEmulatorConnectionString();
            target[connectionName] = connectionString;
            // Injected to support Aspire client integration for Azure Storage.
            target[$"{BlobsConnectionKeyPrefix}__{connectionName}__ConnectionString"] = connectionString;
            target[$"{QueuesConnectionKeyPrefix}__{connectionName}__ConnectionString"] = connectionString;
        }
        else
        {
            // Injected to support Azure Functions listener initialization.
            target[$"{connectionName}__blobServiceUri"] = BlobEndpoint;
            target[$"{connectionName}__queueServiceUri"] = QueueEndpoint;
            // Injected to support Aspire client integration for Azure Storage.
            target[$"{BlobsConnectionKeyPrefix}__{connectionName}__ServiceUri"] = BlobEndpoint;
            target[$"{QueuesConnectionKeyPrefix}__{connectionName}__ServiceUri"] = QueueEndpoint;
        }
    }
}
