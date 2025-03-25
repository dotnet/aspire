// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Service Bus.
/// </summary>
public sealed class AzureMessagingServiceBusSettings : IConnectionStringSettings
{
    private bool? _disableTracing;

    /// <summary>
    /// Gets or sets the connection string used to connect to the Service Bus namespace.
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="FullyQualifiedNamespace"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Service Bus namespace.
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Service Bus namespace.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Name of the queue used by the health check. Mandatory to get health checks enabled.
    /// </summary>
    public string? HealthCheckQueueName { get; set; }

    /// <summary>
    /// Name of the topic used by the health check. Mandatory to get health checks enabled.
    /// </summary>
    public string? HealthCheckTopicName { get; set; }

    /// <summary>
    /// Name of the queue or topic associated with the connection string.
    /// </summary>
    public string? QueueOrTopicName { get; set; }

    /// <summary>
    /// Name of the subscription associated with the connection string.
    /// </summary>
    public string? SubscriptionName { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <remarks>
    /// ServiceBus ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing
    {
        get { return _disableTracing ??= !GetTracingDefaultValue(); }
        set { _disableTracing = value; }
    }

    // Defaults DisableTracing to false if the experimental switch is set
    // TODO: remove this when ActivitySource support is no longer experimental
    private static bool GetTracingDefaultValue()
    {
        if (AppContext.TryGetSwitch("Azure.Experimental.EnableActivitySource", out var enabled))
        {
            return enabled;
        }

        var envVar = Environment.GetEnvironmentVariable("AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE");
        if (envVar is not null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
        {
            return true;
        }

        return false;
    }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            // a service bus namespace can't contain ';'. if it is found assume it is a connection string
            if (!connectionString.Contains(';'))
            {
                FullyQualifiedNamespace = connectionString;
                return;
            }

            var connectionBuilder = new DbConnectionStringBuilder()
            {
                ConnectionString = connectionString
            };

            // Note: Strip out the EntityPath from the connection string in order
            // to tell if we are left with just Endpoint.
            // The EntityPath can contain a queue or topic name. And if it references a topic,
            // it can contain {topic}/Subscriptions/{subscription}. See https://github.com/Azure/azure-sdk-for-net/pull/27070

            if (connectionBuilder.TryGetValue("EntityPath", out var entityPath))
            {
                ParseEntityPath(entityPath.ToString());
                connectionBuilder.Remove("EntityPath");
            }

            if (connectionBuilder.Count == 1 &&
                connectionBuilder.TryGetValue("Endpoint", out var endpoint))
            {
                // if all that's left is Endpoint, it is a fully qualified namespace
                FullyQualifiedNamespace = endpoint.ToString();
                return;
            }

            // Use the connection string with the entitypath removed to
            // support configuring processors/receivers/etc from the service
            // bus client with names derived from settings.
            ConnectionString = connectionBuilder.ConnectionString;
        }
    }

    private void ParseEntityPath(string? entityPath)
    {
        if (string.IsNullOrEmpty(entityPath))
        {
            return;
        }

        // Check if it's a subscription path format: "topicName/Subscriptions/subscriptionName"
        const string subscriptionsSegment = "/Subscriptions/";
        var subscriptionsIndex = entityPath.IndexOf(subscriptionsSegment, StringComparison.OrdinalIgnoreCase);

        if (subscriptionsIndex > 0 && subscriptionsIndex + subscriptionsSegment.Length < entityPath.Length)
        {
            QueueOrTopicName = entityPath.Substring(0, subscriptionsIndex);
            SubscriptionName = entityPath.Substring(subscriptionsIndex + subscriptionsSegment.Length);
        }
        else
        {
            // Single valued entity paths can be either a queue or topic name. The
            // ServiceBus APIs are responsible for determining the type of entity
            // when ServiceBusSender is constructed so we set that here.
            QueueOrTopicName = entityPath;
        }
    }
}
