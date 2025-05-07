// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents an Azure Storage queue.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="queueName">The name of the queue.</param>
/// <param name="parent">The <see cref="AzureQueueStorageResource"/> that the resource is stored in.</param>
public class AzureQueueStorageQueueResource(string name, string queueName, AzureQueueStorageResource parent) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureQueueStorageResource>
{
    /// <summary>
    /// Gets the queue name.
    /// </summary>
    public string QueueName { get; } = ThrowIfNullOrEmpty(queueName);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Storage queue resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.GetConnectionString(QueueName);

    /// <summary>
    /// Gets the parent <see cref="AzureQueueStorageResource"/> of this <see cref="AzureQueueStorageQueueResource"/>.
    /// </summary>
    public AzureQueueStorageResource Parent => parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.Storage.StorageQueue"/> instance.</returns>
    internal global::Azure.Provisioning.Storage.StorageQueue ToProvisioningEntity()
    {
        global::Azure.Provisioning.Storage.StorageQueue queue = new(Infrastructure.NormalizeBicepIdentifier(Name))
        {
            Name = QueueName
        };

        return queue;
    }

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
