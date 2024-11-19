// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Xml;

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Represents a Service Bus Subscription.
/// </summary>
public class ServiceBusSubscription
{
    private readonly OptionalValue<string> _name = new();
    private readonly OptionalValue<TimeSpan> _autoDeleteOnIdle = new();
    private readonly OptionalValue<bool> _deadLetteringOnFilterEvaluationExceptions = new();
    private readonly OptionalValue<bool> _deadLetteringOnMessageExpiration = new();
    private readonly OptionalValue<TimeSpan> _defaultMessageTimeToLive = new();
    private readonly OptionalValue<TimeSpan> _duplicateDetectionHistoryTimeWindow = new();
    private readonly OptionalValue<bool> _enableBatchedOperations = new();
    private readonly OptionalValue<string> _forwardDeadLetteredMessagesTo = new();
    private readonly OptionalValue<string> _forwardTo = new();
    private readonly OptionalValue<bool> _isClientAffine = new();
    private readonly OptionalValue<TimeSpan> _lockDuration = new();
    private readonly OptionalValue<int> _maxDeliveryCount = new();
    private readonly OptionalValue<long> _maxMessageSizeInKilobytes = new();
    private readonly OptionalValue<bool> _requiresSession = new();
    private readonly OptionalValue<ServiceBusMessagingEntityStatus> _status = new();
    private readonly OptionalValue<ServiceBusClientAffineProperties> _clientAffineProperties = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusSubscription"/> class.
    /// </summary>
    public ServiceBusSubscription(string id)
    {
        Id = id;
    }

    /// <summary>
    /// The subscription id.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The subscription name.
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
    /// Properties specific to client affine subscriptions.
    /// </summary>
    public OptionalValue<ServiceBusClientAffineProperties> ClientAffineProperties
    {
        get { return _clientAffineProperties; }
        set { _clientAffineProperties.Assign(value); }
    }

    /// <summary>
    /// Value that indicates whether a subscription has dead letter support on
    /// filter evaluation exceptions.
    /// </summary>
    public OptionalValue<bool> DeadLetteringOnFilterEvaluationExceptions
    {
        get { return _deadLetteringOnFilterEvaluationExceptions!; }
        set { _deadLetteringOnFilterEvaluationExceptions.Assign(value); }
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
    /// Value that indicates whether the subscription has an affinity to the
    /// client id.
    /// </summary>
    public OptionalValue<bool> IsClientAffine
    {
        get { return _isClientAffine; }
        set { _isClientAffine.Assign(value); }
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
    /// <returns>A <see cref="global::Azure.Provisioning.ServiceBus.ServiceBusSubscription"/> instance.</returns>
    public global::Azure.Provisioning.ServiceBus.ServiceBusSubscription ToProvisioningEntity()
    {
        var subscription = new global::Azure.Provisioning.ServiceBus.ServiceBusSubscription(Id);

        if (Name.IsSet && Name.Value != null)
        {
            subscription.Name = Name.Value;
        }

        if (AutoDeleteOnIdle.IsSet)
        {
            subscription.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;
        }
        if (DeadLetteringOnFilterEvaluationExceptions.IsSet)
        {
            subscription.DeadLetteringOnFilterEvaluationExceptions = DeadLetteringOnFilterEvaluationExceptions.Value;
        }
        if (DeadLetteringOnMessageExpiration.IsSet)
        {
            subscription.DeadLetteringOnMessageExpiration = DeadLetteringOnMessageExpiration.Value;
        }
        if (DefaultMessageTimeToLive.IsSet)
        {
            subscription.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
        }
        if (DuplicateDetectionHistoryTimeWindow.IsSet)
        {
            subscription.DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;
        }
        if (EnableBatchedOperations.IsSet)
        {
            subscription.EnableBatchedOperations = EnableBatchedOperations.Value;
        }
        if (ForwardDeadLetteredMessagesTo.IsSet && ForwardDeadLetteredMessagesTo.Value != null)
        {
            subscription.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo.Value;
        }
        if (ForwardTo.IsSet && ForwardTo.Value != null)
        {
            subscription.ForwardTo = ForwardTo.Value;
        }
        if (IsClientAffine.IsSet)
        {
            subscription.IsClientAffine = IsClientAffine.Value;
        }
        if (LockDuration.IsSet)
        {
            subscription.LockDuration = LockDuration.Value;
        }
        if (MaxDeliveryCount.IsSet)
        {
            subscription.MaxDeliveryCount = MaxDeliveryCount.Value;
        }
        if (RequiresSession.IsSet)
        {
            subscription.RequiresSession = RequiresSession.Value;
        }
        if (Status.IsSet)
        {
            subscription.Status = Enum.Parse<global::Azure.Provisioning.ServiceBus.ServiceBusMessagingEntityStatus>(Status.Value.ToString());
        }
        return subscription;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    public void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        var subscription = this;

        if (subscription.Name.IsSet)
        {
            writer.WriteString(nameof(ServiceBusQueue.Name), subscription.Name.Value);
        }

        writer.WriteStartObject("Properties");

        if (subscription.AutoDeleteOnIdle.IsSet)
        {
            writer.WriteString(nameof(ServiceBusSubscription.AutoDeleteOnIdle), XmlConvert.ToString(subscription.AutoDeleteOnIdle.Value));
        }
        if (subscription.ClientAffineProperties.IsSet && subscription.ClientAffineProperties.Value != null)
        {
            writer.WriteStartObject(nameof(subscription.ClientAffineProperties));

            if (subscription.ClientAffineProperties.Value.ClientId.IsSet)
            {
                writer.WriteString(nameof(ServiceBusClientAffineProperties.ClientId), subscription.ClientAffineProperties.Value.ClientId.Value);
            }
            if (subscription.ClientAffineProperties.Value.IsDurable.IsSet)
            {
                writer.WriteBoolean(nameof(ServiceBusClientAffineProperties.IsDurable), subscription.ClientAffineProperties.Value.IsDurable.Value);
            }
            if (subscription.ClientAffineProperties.Value.IsShared.IsSet)
            {
                writer.WriteBoolean(nameof(ServiceBusClientAffineProperties.IsShared), subscription.ClientAffineProperties.Value.IsShared.Value);
            }

            writer.WriteEndObject();
        }

        if (subscription.DeadLetteringOnFilterEvaluationExceptions.IsSet)
        {
            writer.WriteBoolean(nameof(DeadLetteringOnFilterEvaluationExceptions), subscription.DeadLetteringOnFilterEvaluationExceptions.Value);
        }
        if (subscription.DeadLetteringOnMessageExpiration.IsSet)
        {
            writer.WriteBoolean(nameof(DeadLetteringOnMessageExpiration), subscription.DeadLetteringOnMessageExpiration.Value);
        }
        if (subscription.DefaultMessageTimeToLive.IsSet)
        {
            writer.WriteString(nameof(DefaultMessageTimeToLive), XmlConvert.ToString(subscription.DefaultMessageTimeToLive.Value));
        }
        if (subscription.DuplicateDetectionHistoryTimeWindow.IsSet)
        {
            writer.WriteString(nameof(DuplicateDetectionHistoryTimeWindow), XmlConvert.ToString(subscription.DuplicateDetectionHistoryTimeWindow.Value));
        }
        if (subscription.EnableBatchedOperations.IsSet)
        {
            writer.WriteBoolean(nameof(EnableBatchedOperations), subscription.EnableBatchedOperations.Value);
        }
        if (subscription.ForwardDeadLetteredMessagesTo.IsSet)
        {
            writer.WriteString(nameof(ForwardDeadLetteredMessagesTo), subscription.ForwardDeadLetteredMessagesTo.Value);
        }
        if (subscription.ForwardTo.IsSet)
        {
            writer.WriteString(nameof(ForwardTo), subscription.ForwardTo.Value);
        }
        if (subscription.IsClientAffine.IsSet)
        {
            writer.WriteBoolean(nameof(IsClientAffine), subscription.IsClientAffine.Value);
        }
        if (subscription.LockDuration.IsSet)
        {
            writer.WriteString(nameof(LockDuration), XmlConvert.ToString(subscription.LockDuration.Value));
        }
        if (subscription.MaxDeliveryCount.IsSet)
        {
            writer.WriteNumber(nameof(MaxDeliveryCount), subscription.MaxDeliveryCount.Value);
        }
        if (subscription.RequiresSession.IsSet)
        {
            writer.WriteBoolean(nameof(RequiresSession), subscription.RequiresSession.Value);
        }
        if (subscription.Status.IsSet)
        {
            writer.WriteString(nameof(Status), subscription.Status.Value.ToString());
        }

        writer.WriteEndObject();
    }
}
