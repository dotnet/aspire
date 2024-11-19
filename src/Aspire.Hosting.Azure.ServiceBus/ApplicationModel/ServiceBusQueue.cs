// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Xml;

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Represents a Service Bus Queue.
/// </summary>
public class ServiceBusQueue
{
    private readonly OptionalValue<string> _name = new();
    private readonly OptionalValue<TimeSpan> _autoDeleteOnIdle = new();
    private readonly OptionalValue<bool> _deadLetteringOnMessageExpiration = new();
    private readonly OptionalValue<TimeSpan> _defaultMessageTimeToLive = new();
    private readonly OptionalValue<TimeSpan> _duplicateDetectionHistoryTimeWindow = new();
    private readonly OptionalValue<bool> _enableBatchedOperations = new();
    private readonly OptionalValue<bool> _enableExpress = new();
    private readonly OptionalValue<bool> _enablePartitioning = new();
    private readonly OptionalValue<string> _forwardDeadLetteredMessagesTo = new();
    private readonly OptionalValue<string> _forwardTo = new();
    private readonly OptionalValue<TimeSpan> _lockDuration = new();
    private readonly OptionalValue<int> _maxDeliveryCount = new();
    private readonly OptionalValue<long> _maxMessageSizeInKilobytes = new();
    private readonly OptionalValue<int> _maxSizeInMegabytes = new();
    private readonly OptionalValue<bool> _requiresDuplicateDetection = new();
    private readonly OptionalValue<bool> _requiresSession = new();
    private readonly OptionalValue<ServiceBusMessagingEntityStatus> _status = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusQueue"/> class.
    /// </summary>
    public ServiceBusQueue(string id)
    {
        Id = id;
    }

    /// <summary>
    /// The queue id.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The queue name.
    /// </summary>
    public OptionalValue<string> Name
    {
        get { return _name; }
        set { _name.Assign(value); }
    }

    /// <summary>
    /// ISO 8061 timeSpan idle interval after which the queue is automatically
    /// deleted. The minimum duration is 5 minutes.
    /// </summary>
    public OptionalValue<TimeSpan> AutoDeleteOnIdle
    {
        get { return _autoDeleteOnIdle; }
        set { _autoDeleteOnIdle.Assign(value); }
    }

    /// <summary>
    /// A value that indicates whether this queue has dead letter support when
    /// a message expires.
    /// </summary>
    public OptionalValue<bool> DeadLetteringOnMessageExpiration
    {
        get { return _deadLetteringOnMessageExpiration; }
        set { _deadLetteringOnMessageExpiration.Assign(value); }
    }

    /// <summary>
    /// ISO 8601 default message timespan to live value. This is the duration
    /// after which the message expires, starting from when the message is
    /// sent to Service Bus. This is the default value used when TimeToLive is
    /// not set on a message itself.
    /// </summary>
    public OptionalValue<TimeSpan> DefaultMessageTimeToLive
    {
        get { return _defaultMessageTimeToLive; }
        set { _defaultMessageTimeToLive.Assign(value); }
    }

    /// <summary>
    /// ISO 8601 timeSpan structure that defines the duration of the duplicate
    /// detection history. The default value is 10 minutes.
    /// </summary>
    public OptionalValue<TimeSpan> DuplicateDetectionHistoryTimeWindow
    {
        get { return _duplicateDetectionHistoryTimeWindow; }
        set { _duplicateDetectionHistoryTimeWindow.Assign(value); }
    }

    /// <summary>
    /// Value that indicates whether server-side batched operations are enabled.
    /// </summary>
    public OptionalValue<bool> EnableBatchedOperations
    {
        get { return _enableBatchedOperations; }
        set { _enableBatchedOperations.Assign(value); }
    }

    /// <summary>
    /// A value that indicates whether Express Entities are enabled. An express
    /// queue holds a message in memory temporarily before writing it to
    /// persistent storage.
    /// </summary>
    public OptionalValue<bool> EnableExpress
    {
        get { return _enableExpress; }
        set { _enableExpress.Assign(value); }
    }

    /// <summary>
    /// A value that indicates whether the queue is to be partitioned across
    /// multiple message brokers.
    /// </summary>
    public OptionalValue<bool> EnablePartitioning
    {
        get { return _enablePartitioning; }
        set { _enablePartitioning.Assign(value); }
    }

    /// <summary>
    /// Queue/Topic name to forward the Dead Letter message.
    /// </summary>
    public OptionalValue<string> ForwardDeadLetteredMessagesTo
    {
        get { return _forwardDeadLetteredMessagesTo; }
        set { _forwardDeadLetteredMessagesTo.Assign(value); }
    }

    /// <summary>
    /// Queue/Topic name to forward the messages.
    /// </summary>
    public OptionalValue<string> ForwardTo
    {
        get { return _forwardTo; }
        set { _forwardTo.Assign(value); }
    }

    /// <summary>
    /// ISO 8601 timespan duration of a peek-lock; that is, the amount of time
    /// that the message is locked for other receivers. The maximum value for
    /// LockDuration is 5 minutes; the default value is 1 minute.
    /// </summary>
    public OptionalValue<TimeSpan> LockDuration
    {
        get { return _lockDuration; }
        set { _lockDuration.Assign(value); }
    }

    /// <summary>
    /// The maximum delivery count. A message is automatically deadlettered
    /// after this number of deliveries. default value is 10.
    /// </summary>
    public OptionalValue<int> MaxDeliveryCount
    {
        get { return _maxDeliveryCount; }
        set { _maxDeliveryCount.Assign(value); }
    }

    /// <summary>
    /// Maximum size (in KB) of the message payload that can be accepted by the
    /// queue. This property is only used in Premium today and default is 1024.
    /// </summary>
    public OptionalValue<long> MaxMessageSizeInKilobytes
    {
        get { return _maxMessageSizeInKilobytes; }
        set { _maxMessageSizeInKilobytes.Assign(value); }
    }

    /// <summary>
    /// The maximum size of the queue in megabytes, which is the size of memory
    /// allocated for the queue. Default is 1024.
    /// </summary>
    public OptionalValue<int> MaxSizeInMegabytes
    {
        get { return _maxSizeInMegabytes; }
        set { _maxSizeInMegabytes.Assign(value); }
    }

    /// <summary>
    /// A value indicating if this queue requires duplicate detection.
    /// </summary>
    public OptionalValue<bool> RequiresDuplicateDetection
    {
        get { return _requiresDuplicateDetection; }
        set { _requiresDuplicateDetection.Assign(value); }
    }

    /// <summary>
    /// A value that indicates whether the queue supports the concept of
    /// sessions.
    /// </summary>
    public OptionalValue<bool> RequiresSession
    {
        get { return _requiresSession; }
        set { _requiresSession.Assign(value); }
    }

    /// <summary>
    /// Enumerates the possible values for the status of a messaging entity.
    /// </summary>
    public OptionalValue<ServiceBusMessagingEntityStatus> Status
    {
        get { return _status; }
        set { _status.Assign(value); }
    }

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.ServiceBus.ServiceBusQueue"/> instance.</returns>
    public global::Azure.Provisioning.ServiceBus.ServiceBusQueue ToProvisioningEntity()
    {
        var queue = new global::Azure.Provisioning.ServiceBus.ServiceBusQueue(Id);

        if (Name.IsSet && Name.Value != null)
        {
            queue.Name = Name.Value;
        }

        if (AutoDeleteOnIdle.IsSet)
        {
            queue.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;
        }
        if (DeadLetteringOnMessageExpiration.IsSet)
        {
            queue.DeadLetteringOnMessageExpiration = DeadLetteringOnMessageExpiration.Value;
        }
        if (DefaultMessageTimeToLive.IsSet)
        {
            queue.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
        }
        if (DuplicateDetectionHistoryTimeWindow.IsSet)
        {
            queue.DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;
        }
        if (EnableBatchedOperations.IsSet)
        {
            queue.EnableBatchedOperations = EnableBatchedOperations.Value;
        }
        if (EnableExpress.IsSet)
        {
            queue.EnableExpress = EnableExpress.Value;
        }
        if (EnablePartitioning.IsSet)
        {
            queue.EnablePartitioning = EnablePartitioning.Value;
        }
        if (ForwardDeadLetteredMessagesTo.IsSet && ForwardDeadLetteredMessagesTo.Value != null)
        {
            queue.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo.Value;
        }
        if (ForwardTo.IsSet && ForwardTo.Value != null)
        {
            queue.ForwardTo = ForwardTo.Value;
        }
        if (LockDuration.IsSet)
        {
            queue.LockDuration = LockDuration.Value;
        }
        if (MaxDeliveryCount.IsSet)
        {
            queue.MaxDeliveryCount = MaxDeliveryCount.Value;
        }
        if (MaxMessageSizeInKilobytes.IsSet)
        {
            queue.MaxSizeInMegabytes = MaxSizeInMegabytes.Value;
        }
        if (RequiresDuplicateDetection.IsSet)
        {
            queue.RequiresDuplicateDetection = RequiresDuplicateDetection.Value;
        }
        if (RequiresSession.IsSet)
        {
            queue.RequiresSession = RequiresSession.Value;
        }
        if (Status.IsSet)
        {
            queue.Status = Enum.Parse<global::Azure.Provisioning.ServiceBus.ServiceBusMessagingEntityStatus>(Status.Value.ToString());
        }
        return queue;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    public void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        var queue = this;

        if (queue.Name.IsSet)
        {
            writer.WriteString(nameof(Name), queue.Name.Value);
        }
        writer.WriteStartObject("Properties");

        if (queue.AutoDeleteOnIdle.IsSet)
        {
            writer.WriteString(nameof(AutoDeleteOnIdle), XmlConvert.ToString(queue.AutoDeleteOnIdle.Value));
        }
        if (queue.DeadLetteringOnMessageExpiration.IsSet)
        {
            writer.WriteBoolean(nameof(DeadLetteringOnMessageExpiration), queue.DeadLetteringOnMessageExpiration.Value);
        }
        if (queue.DefaultMessageTimeToLive.IsSet)
        {
            writer.WriteString(nameof(DefaultMessageTimeToLive), XmlConvert.ToString(queue.DefaultMessageTimeToLive.Value));
        }
        if (queue.DuplicateDetectionHistoryTimeWindow.IsSet)
        {
            writer.WriteString(nameof(DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(queue.DuplicateDetectionHistoryTimeWindow.Value));
        }
        if (queue.EnableBatchedOperations.IsSet)
        {
            writer.WriteBoolean(nameof(EnableBatchedOperations), queue.EnableBatchedOperations.Value);
        }
        if (queue.EnableExpress.IsSet)
        {
            writer.WriteBoolean(nameof(EnableExpress), queue.EnableExpress.Value);
        }
        if (queue.EnablePartitioning.IsSet)
        {
            writer.WriteBoolean(nameof(EnablePartitioning), queue.EnablePartitioning.Value);
        }
        if (queue.ForwardDeadLetteredMessagesTo.IsSet)
        {
            writer.WriteString(nameof(ForwardDeadLetteredMessagesTo), queue.ForwardDeadLetteredMessagesTo.Value);
        }
        if (queue.ForwardTo.IsSet)
        {
            writer.WriteString(nameof(ForwardTo), queue.ForwardTo.Value);
        }
        if (queue.LockDuration.IsSet)
        {
            writer.WriteString(nameof(LockDuration), XmlConvert.ToString(queue.LockDuration.Value));
        }
        if (queue.MaxDeliveryCount.IsSet)
        {
            writer.WriteNumber(nameof(MaxDeliveryCount), queue.MaxDeliveryCount.Value);
        }
        if (queue.MaxMessageSizeInKilobytes.IsSet)
        {
            writer.WriteNumber(nameof(MaxMessageSizeInKilobytes), queue.MaxMessageSizeInKilobytes.Value);
        }
        if (queue.MaxSizeInMegabytes.IsSet)
        {
            writer.WriteNumber(nameof(MaxSizeInMegabytes), queue.MaxSizeInMegabytes.Value);
        }
        if (queue.RequiresDuplicateDetection.IsSet)
        {
            writer.WriteBoolean(nameof(RequiresDuplicateDetection), queue.RequiresDuplicateDetection.Value);
        }
        if (queue.RequiresSession.IsSet)
        {
            writer.WriteBoolean(nameof(RequiresSession), queue.RequiresSession.Value);
        }
        if (queue.Status.IsSet)
        {
            writer.WriteString(nameof(Status), queue.Status.Value.ToString());
        }
        writer.WriteEndObject();
    }
}
