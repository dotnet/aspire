// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Queue Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureQueueStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>,
    IResourceWithAzureFunctionsConfig
{
    // NOTE: if ever these contants are changed, the AzureStorageQueueSettings in Aspire.Azure.Storage.Queues class should be updated as well.
    private const string Endpoint = nameof(Endpoint);
    private const string QueueName = nameof(QueueName);

    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureQueueStorageResource.
    /// </summary>
    public AzureStorageResource Parent => storage ?? throw new ArgumentNullException(nameof(storage));

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Queue Storage resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Parent.GetQueueConnectionString();

    internal ReferenceExpression GetConnectionString(string? queueName)
    {
        if (string.IsNullOrEmpty(queueName))
        {
            return ConnectionStringExpression;
        }

        ReferenceExpressionBuilder builder = new();
        builder.Append($"{Endpoint}=\"{ConnectionStringExpression}\";{QueueName}={queueName};");
        return builder.Build();
    }

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (Parent.IsEmulator)
        {
            var connectionString = Parent.GetEmulatorConnectionString();
            target[connectionName] = connectionString;
            target[$"{AzureStorageResource.QueuesConnectionKeyPrefix}__{connectionName}__ConnectionString"] = connectionString;
        }
        else
        {
            // Injected to support Azure Functions listener.
            target[$"{connectionName}__queueServiceUri"] = Parent.QueueEndpoint;
            // Injected to support Aspire client integration for Azure Storage Queues.
            target[$"{AzureStorageResource.QueuesConnectionKeyPrefix}__{connectionName}__ServiceUri"] = Parent.QueueEndpoint;
        }
    }

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.Storage.QueueService"/> instance.</returns>
    internal global::Azure.Provisioning.Storage.QueueService ToProvisioningEntity()
    {
        global::Azure.Provisioning.Storage.QueueService service = new(Infrastructure.NormalizeBicepIdentifier(Name));
        return service;
    }
}
