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
       ? ReferenceExpression.Create($"{AzureStorageEmulatorConnectionString.Create(blobPort: EmulatorBlobEndpoint.Port, queuePort: EmulatorQueueEndpoint.Port, tablePort: EmulatorTableEndpoint.Port)}")
       : throw new InvalidOperationException("The Azure Storage resource is not running in the local emulator.");

    internal ReferenceExpression GetTableConnectionString() => IsEmulator
        ? ReferenceExpression.Create($"{AzureStorageEmulatorConnectionString.Create(tablePort: EmulatorTableEndpoint.Port)}")
        : ReferenceExpression.Create($"{TableEndpoint}");

    internal ReferenceExpression GetQueueConnectionString() => IsEmulator
        ? ReferenceExpression.Create($"{AzureStorageEmulatorConnectionString.Create(queuePort: EmulatorQueueEndpoint.Port)}")
        : ReferenceExpression.Create($"{QueueEndpoint}");

    internal ReferenceExpression GetBlobConnectionString() => IsEmulator
        ? ReferenceExpression.Create($"{AzureStorageEmulatorConnectionString.Create(blobPort: EmulatorBlobEndpoint.Port)}")
        : ReferenceExpression.Create($"{BlobEndpoint}");

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (IsEmulator)
        {
            target[connectionName] = GetEmulatorConnectionString();
        }
        else
        {
            target[$"{connectionName}__blobServiceUri"] = BlobEndpoint;
            target[$"{connectionName}__queueServiceUri"] = QueueEndpoint;
        }
    }
}
