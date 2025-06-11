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
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        // Format 1: Simple namespace (no semicolons and not starting with "Endpoint=")
        // Example: "test.servicebus.windows.net"
        if (!connectionString.Contains(';') && !connectionString.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            FullyQualifiedNamespace = connectionString;
            return;
        }

        // Parse connection string parameters for all other formats
        var connectionBuilder = new DbConnectionStringBuilder()
        {
            ConnectionString = connectionString
        };

        // Check if EntityPath is present and parse it
        bool hasEntityPath = connectionBuilder.TryGetValue("EntityPath", out var entityPath);
        if (hasEntityPath)
        {
            // Extract queue/topic and subscription names from EntityPath
            // Format examples: "myqueue" or "mytopic/Subscriptions/mysub"
            ParseEntityPath(entityPath?.ToString());
            
            // Remove EntityPath for endpoint-only detection
            connectionBuilder.Remove("EntityPath");
        }

        // Format 2: Endpoint-only connection string (with or without EntityPath)
        // Example: "Endpoint=sb://test.servicebus.windows.net/" 
        // Example: "Endpoint=sb://test.servicebus.windows.net/;EntityPath=myqueue"
        if (connectionBuilder.Count == 1 && connectionBuilder.TryGetValue("Endpoint", out var endpoint))
        {
            FullyQualifiedNamespace = ExtractHostFromEndpoint(endpoint?.ToString() ?? string.Empty);
            return;
        }

        // Format 3: Full connection string without EntityPath
        // Example: "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key="
        // Format 4: Full connection string with EntityPath (EntityPath gets removed)
        // Example: "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key=;EntityPath=myqueue"
        if (hasEntityPath)
        {
            // Remove EntityPath from original connection string while preserving format
            ConnectionString = RemoveEntityPathFromConnectionString(connectionString);
        }
        else
        {
            // No EntityPath to remove, use original connection string as-is
            ConnectionString = connectionString;
        }
    }

    private static string ExtractHostFromEndpoint(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }
        return endpoint;
    }

    private static string RemoveEntityPathFromConnectionString(string connectionString)
    {
        // Find EntityPath parameter and remove it while preserving original format
        var parts = connectionString.Split(';');
        var filteredParts = new List<string>();
        
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (!string.IsNullOrEmpty(trimmedPart) && 
                !trimmedPart.StartsWith("EntityPath=", StringComparison.OrdinalIgnoreCase))
            {
                filteredParts.Add(part);
            }
        }
        
        var result = string.Join(";", filteredParts);
        
        // Handle edge case where original string ended with ';' but EntityPath was the last part
        if (connectionString.EndsWith(';') && !result.EndsWith(';'))
        {
            result += ";";
        }
        
        return result;
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
