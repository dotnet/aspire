// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Xml;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a Service Bus Subscription.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureServiceBusSubscriptionResource : Resource, IResourceWithParent<AzureServiceBusTopicResource>, IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBusSubscriptionResource"/> class.
    /// </summary>
    public AzureServiceBusSubscriptionResource(string name, string subscriptionName, AzureServiceBusTopicResource parent) : base(name)
    {
        SubscriptionName = subscriptionName;
        Parent = parent;
    }

    /// <summary>
    /// The subscription name.
    /// </summary>
    public string SubscriptionName { get; set; }

    /// <summary>
    /// Gets the parent Azure Service Bus Topic resource.
    /// </summary>
    public AzureServiceBusTopicResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Service Bus Subscription.
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
    /// The maximum delivery count. A message is automatically deadlettered
    /// after this number of deliveries. default value is 10.
    /// </summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>
    /// A value that indicates whether the queue supports the concept of
    /// sessions.
    /// </summary>
    public bool? RequiresSession { get; set; }

    /// <summary>
    /// The rules for this subscription.
    /// </summary>
    public List<AzureServiceBusRule> Rules { get; } = [];

    // ensure Azure Functions projects can WithReference a ServiceBus queue
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
        => ((IResourceWithAzureFunctionsConfig)Parent).ApplyAzureFunctionsConfiguration(target, connectionName);

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.ServiceBus.ServiceBusSubscription"/> instance.</returns>
    internal global::Azure.Provisioning.ServiceBus.ServiceBusSubscription ToProvisioningEntity()
    {
        var subscription = new global::Azure.Provisioning.ServiceBus.ServiceBusSubscription(Infrastructure.NormalizeBicepIdentifier(Name));

        if (SubscriptionName != null)
        {
            subscription.Name = SubscriptionName;
        }

        if (DeadLetteringOnMessageExpiration.HasValue)
        {
            subscription.DeadLetteringOnMessageExpiration = DeadLetteringOnMessageExpiration.Value;
        }
        if (DefaultMessageTimeToLive.HasValue)
        {
            subscription.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
        }
        if (ForwardDeadLetteredMessagesTo != null)
        {
            subscription.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo;
        }
        if (ForwardTo != null)
        {
            subscription.ForwardTo = ForwardTo;
        }
        if (LockDuration.HasValue)
        {
            subscription.LockDuration = LockDuration.Value;
        }
        if (MaxDeliveryCount.HasValue)
        {
            subscription.MaxDeliveryCount = MaxDeliveryCount.Value;
        }
        if (RequiresSession.HasValue)
        {
            subscription.RequiresSession = RequiresSession.Value;
        }
        return subscription;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    internal void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        var subscription = this;

        if (subscription.SubscriptionName != null)
        {
            writer.WriteString(nameof(Name), subscription.SubscriptionName);
        }

        writer.WriteStartObject("Properties");

        if (subscription.DeadLetteringOnMessageExpiration.HasValue)
        {
            writer.WriteBoolean(nameof(DeadLetteringOnMessageExpiration), subscription.DeadLetteringOnMessageExpiration.Value);
        }
        if (subscription.DefaultMessageTimeToLive.HasValue)
        {
            writer.WriteString(nameof(DefaultMessageTimeToLive), XmlConvert.ToString(subscription.DefaultMessageTimeToLive.Value));
        }
        if (subscription.ForwardDeadLetteredMessagesTo != null)
        {
            writer.WriteString(nameof(ForwardDeadLetteredMessagesTo), subscription.ForwardDeadLetteredMessagesTo);
        }
        if (subscription.ForwardTo != null)
        {
            writer.WriteString(nameof(ForwardTo), subscription.ForwardTo);
        }
        if (subscription.LockDuration.HasValue)
        {
            writer.WriteString(nameof(LockDuration), XmlConvert.ToString(subscription.LockDuration.Value));
        }
        if (subscription.MaxDeliveryCount.HasValue)
        {
            writer.WriteNumber(nameof(MaxDeliveryCount), subscription.MaxDeliveryCount.Value);
        }
        if (subscription.RequiresSession.HasValue)
        {
            writer.WriteBoolean(nameof(RequiresSession), subscription.RequiresSession.Value);
        }

        writer.WriteEndObject();
    }
}
