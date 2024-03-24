// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Messaging.EventHubs;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

internal abstract class EventHubsComponent<TClient, TClientOptions> :
    AzureComponent<AzureMessagingEventHubsSettings, TClient, TClientOptions>
    where TClientOptions: class
    where TClient : class
{
    protected override IHealthCheck CreateHealthCheck(TClient client, AzureMessagingEventHubsSettings settings)
        => throw new NotImplementedException();

    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsSettings settings, IConfiguration config)
    {
        config.Bind(settings);
    }

    protected override bool GetHealthCheckEnabled(AzureMessagingEventHubsSettings settings)
        => false;

    protected override TokenCredential? GetTokenCredential(AzureMessagingEventHubsSettings settings)
        => settings.Credential;

    protected override bool GetTracingEnabled(AzureMessagingEventHubsSettings settings)
        => settings.Tracing;

    protected static string GenerateClientIdentifier(AzureMessagingEventHubsSettings settings)
    {
        // configure processor identifier
        var slug = Guid.NewGuid().ToString().Substring(24);
        var identifier = $"{Environment.MachineName}-{settings.EventHubName}-" +
                         $"{settings.ConsumerGroup ?? "default"}-{slug}";

        return identifier;
    }

    protected static string GetNamespaceFromSettings(AzureMessagingEventHubsSettings settings)
    {
        string ns;

        try
        {
            // extract the namespace from the connection string or qualified namespace
            var fullyQualifiedNs = string.IsNullOrWhiteSpace(settings.Namespace) ?
                EventHubsConnectionStringProperties.Parse(settings.ConnectionString).FullyQualifiedNamespace :
                new Uri(settings.Namespace).Host;

            ns = fullyQualifiedNs[..fullyQualifiedNs.IndexOf('.', StringComparison.Ordinal)];
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException)
        {
            throw new InvalidOperationException(
                $"A {typeof(TClient).Name} could not be configured. Please ensure that the ConnectionString or Namespace is well-formed.");
        }

        return ns;
    }

    protected static void EnsureConnectionStringOrNamespaceProvided(AzureMessagingEventHubsSettings settings,
        string connectionName, string configurationSectionName)
    {
        var connectionString = settings.ConnectionString;

        // Are we missing both connection string and namespace? throw.
        if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(settings.Namespace))
        {
            throw new InvalidOperationException(
                $"A {typeof(TClient).Name} could not be configured. Ensure valid connection information was provided in " +
                $"'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Namespace' in the '{configurationSectionName}' configuration section.");
        }

        // If we have a connection string, ensure there's an EntityPath if settings.EventHubName is missing
        if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            // No EventHubName?
            if (string.IsNullOrWhiteSpace(settings.EventHubName))
            {
                // look for EntityPath
                var props = EventHubsConnectionStringProperties.Parse(connectionString);

                // if EntityPath is missing, throw
                if (string.IsNullOrWhiteSpace(props.EventHubName))
                {
                    throw new InvalidOperationException(
                        $"A {typeof(TClient).Name} could not be configured. Ensure a valid EventHubName was provided in " +
                        $"the '{configurationSectionName}' configuration section, or include an EntityPath in the ConnectionString.");
                }
            }
        }
        // If we have a namespace and no connection string, ensure there's an EventHubName
        else if (!string.IsNullOrWhiteSpace(settings.Namespace) && string.IsNullOrWhiteSpace(settings.EventHubName))
        {
            throw new InvalidOperationException(
                $"A {typeof(TClient).Name} could not be configured. Ensure a valid EventHubName was provided in " +
                $"the '{configurationSectionName}' configuration section.");
        }
    }
}
