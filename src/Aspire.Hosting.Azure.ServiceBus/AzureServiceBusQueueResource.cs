// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Xml;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a Service Bus Queue.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureServiceBusQueueResource : Resource, IResourceWithParent<AzureServiceBusResource>, IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBusQueueResource"/> class.
    /// </summary>
    public AzureServiceBusQueueResource(string name, string queueName, AzureServiceBusResource parent) : base(name)
    {
        QueueName = queueName;
        Parent = parent;
    }

    /// <summary>
    /// The queue name.
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// Gets the parent Azure Service Bus resource.
    /// </summary>
    public AzureServiceBusResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Service Bus Queue.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;

    /// <summary>
    /// A value that indicates whether this queue has dead letter support when
    /// a message expires.
    /// </summary>
    public bool? DeadLetteringOnMessageExpiration { get; set; }

    /// <summary>
    /// ISO 8601 default message timespan to live value. This is the duration
    /// after which the message expires, starting from when the message is
    /// sent to Service Bus. This is the default value used when TimeToLive is
    /// not set on a message itself.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>
    /// ISO 8601 timeSpan structure that defines the duration of the duplicate
    /// detection history. The default value is 10 minutes.
    /// </summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }

    /// <summary>
    /// Queue/Topic name to forward the Dead Letter message.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>
    /// Queue/Topic name to forward the messages.
    /// </summary>
    public string? ForwardTo { get; set; }

    /// <summary>
    /// ISO 8601 timespan duration of a peek-lock; that is, the amount of time
    /// that the message is locked for other receivers. The maximum value for
    /// LockDuration is 5 minutes; the default value is 1 minute.
    /// </summary>
    public TimeSpan? LockDuration { get; set; }

    /// <summary>
    /// The maximum delivery count. A message is automatically dead-lettered
    /// after this number of deliveries.
    /// </summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>
    /// A value indicating if this queue requires duplicate detection.
    /// </summary>
    public bool? RequiresDuplicateDetection { get; set; }

    /// <summary>
    /// A value that indicates whether the queue supports the concept of
    /// sessions.
    /// </summary>
    public bool? RequiresSession { get; set; }

    // ensure Azure Functions projects can WithReference a ServiceBus queue
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
        => ((IResourceWithAzureFunctionsConfig)Parent).ApplyAzureFunctionsConfiguration(target, connectionName);

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.ServiceBus.ServiceBusQueue"/> instance.</returns>
    internal global::Azure.Provisioning.ServiceBus.ServiceBusQueue ToProvisioningEntity()
    {
        var queue = new global::Azure.Provisioning.ServiceBus.ServiceBusQueue(Infrastructure.NormalizeBicepIdentifier(Name));

        queue.Name = QueueName;

        if (DeadLetteringOnMessageExpiration.HasValue)
        {
            queue.DeadLetteringOnMessageExpiration = DeadLetteringOnMessageExpiration.Value;
        }
        if (DefaultMessageTimeToLive.HasValue)
        {
            queue.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
        }
        if (DuplicateDetectionHistoryTimeWindow.HasValue)
        {
            queue.DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;
        }
        if (ForwardDeadLetteredMessagesTo != null)
        {
            queue.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo;
        }
        if (ForwardTo != null)
        {
            queue.ForwardTo = ForwardTo;
        }
        if (LockDuration.HasValue)
        {
            queue.LockDuration = LockDuration.Value;
        }
        if (MaxDeliveryCount.HasValue)
        {
            queue.MaxDeliveryCount = MaxDeliveryCount.Value;
        }
        if (RequiresDuplicateDetection.HasValue)
        {
            queue.RequiresDuplicateDetection = RequiresDuplicateDetection.Value;
        }
        if (RequiresSession.HasValue)
        {
            queue.RequiresSession = RequiresSession.Value;
        }
        return queue;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    internal void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        var queue = this;

        writer.WriteString(nameof(Name), queue.QueueName);

        writer.WriteStartObject("Properties");

        if (queue.DeadLetteringOnMessageExpiration.HasValue)
        {
            writer.WriteBoolean(nameof(DeadLetteringOnMessageExpiration), queue.DeadLetteringOnMessageExpiration.Value);
        }
        if (queue.DefaultMessageTimeToLive.HasValue)
        {
            writer.WriteString(nameof(DefaultMessageTimeToLive), XmlConvert.ToString(queue.DefaultMessageTimeToLive.Value));
        }
        if (queue.DuplicateDetectionHistoryTimeWindow.HasValue)
        {
            writer.WriteString(nameof(DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(queue.DuplicateDetectionHistoryTimeWindow.Value));
        }
        if (queue.ForwardDeadLetteredMessagesTo != null)
        {
            writer.WriteString(nameof(ForwardDeadLetteredMessagesTo), queue.ForwardDeadLetteredMessagesTo);
        }
        if (queue.ForwardTo != null)
        {
            writer.WriteString(nameof(ForwardTo), queue.ForwardTo);
        }
        if (queue.LockDuration.HasValue)
        {
            writer.WriteString(nameof(LockDuration), XmlConvert.ToString(queue.LockDuration.Value));
        }
        if (queue.MaxDeliveryCount.HasValue)
        {
            writer.WriteNumber(nameof(MaxDeliveryCount), queue.MaxDeliveryCount.Value);
        }
        if (queue.RequiresDuplicateDetection.HasValue)
        {
            writer.WriteBoolean(nameof(RequiresDuplicateDetection), queue.RequiresDuplicateDetection.Value);
        }
        if (queue.RequiresSession.HasValue)
        {
            writer.WriteBoolean(nameof(RequiresSession), queue.RequiresSession.Value);
        }
        writer.WriteEndObject();
    }
}
