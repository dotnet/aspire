// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Messaging.EventHubs;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="EventHubProducerClient"/> and/or <see cref="EventProcessorClient" /> n in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireEventHubsExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Messaging:EventHubs:"; // + nameof(TClient)

    public static void AddAzureEventHubProcessorClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null)
    {
        new EventProcessorClientComponent()
            .AddClient(builder, DefaultConfigSectionName + nameof(EventProcessorClient),
                configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    public static void AddKeyedAzureEventHubProcessorClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = EventHubProducerClientComponent
            .GetKeyedConfigurationSectionName(name, DefaultConfigSectionName +
                                                    nameof(EventHubProducerClient));

        new EventProcessorClientComponent()
            .AddClient(builder, configurationSectionName, configureSettings,
                configureClientBuilder, connectionName: name, serviceKey: name);
    }

    public static void AddAzureEventHubProducerClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null)
    {
        new EventHubProducerClientComponent()
            .AddClient(builder, DefaultConfigSectionName + nameof(EventHubProducerClient),
                configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    public static void AddKeyedAzureEventHubProducerClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingEventHubsSettings>? configureSettings = null,
        Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = EventHubProducerClientComponent
            .GetKeyedConfigurationSectionName(name, DefaultConfigSectionName +
                                                    nameof(EventHubProducerClient));

        new EventHubProducerClientComponent()
            .AddClient(builder, configurationSectionName, configureSettings,
                configureClientBuilder, connectionName: name, serviceKey: name);
    }
}

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
        => !string.IsNullOrEmpty(settings.HealthCheckQueueName) || !string.IsNullOrEmpty(settings.HealthCheckTopicName);

    protected override TokenCredential? GetTokenCredential(AzureMessagingEventHubsSettings settings)
        => settings.Credential;

    protected override bool GetTracingEnabled(AzureMessagingEventHubsSettings settings)
        => settings.Tracing;
}
