// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Messaging.ServiceBus;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Messaging.ServiceBus;
using HealthChecks.AzureServiceBus;
using HealthChecks.AzureServiceBus.Configuration;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireServiceBusExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Messaging:ServiceBus";
    private const string NamespaceConfigKeyName = "Namespace";
    public const string DefaultNamespaceConfigKey = $"{DefaultConfigSectionName}:{NamespaceConfigKeyName}";

    /// <summary>
    /// Registers <see cref="ServiceBusClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{ServiceBusClient, ServiceBusClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Messaging.ServiceBus" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.Namespace"/> is provided.</exception>
    public static void AddAzureServiceBus(
        this IHostApplicationBuilder builder,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null)
    {
        new MessageBusComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, name: null);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{ServiceBusClient, ServiceBusClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Messaging.ServiceBus:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.Namespace"/> is provided.</exception>
    public static void AddAzureServiceBus(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = MessageBusComponent.GetKeyedConfigurationSectionName(name, DefaultConfigSectionName);

        new MessageBusComponent().AddClient(builder, configurationSectionName, configureSettings, configureClientBuilder, name);
    }

    private sealed class MessageBusComponent : AzureComponent<AzureMessagingServiceBusSettings, ServiceBusClient, ServiceBusClientOptions>
    {
        protected override string[] ActivitySourceNames => ["Azure.Messaging.ServiceBus", "Azure.Messaging.ServiceBus.ServiceBusReceiver", "Azure.Messaging.ServiceBus.ServiceBusSender", "Azure.Messaging.ServiceBus.ServiceBusProcessor"];

        protected override IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureMessagingServiceBusSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.ConnectionString))
            {
                return azureFactoryBuilder.AddServiceBusClient(settings.ConnectionString);
            }
            else
            {
                return azureFactoryBuilder.AddServiceBusClientWithNamespace(settings.Namespace!);
            }
        }

        protected override IHealthCheck CreateHealthCheck(ServiceBusClient client, AzureMessagingServiceBusSettings settings)
            => !string.IsNullOrEmpty(settings.HealthCheckQueueName)
                    ? new AzureServiceBusQueueHealthCheck(new AzureServiceBusQueueHealthCheckOptions(settings.HealthCheckQueueName)
                    {
                        FullyQualifiedNamespace = settings.Namespace,
                        ConnectionString = settings.ConnectionString,
                        Credential = settings.Credential
                    })
                    : new AzureServiceBusTopicHealthCheck(new AzureServiceBusTopicHealthCheckOptions(settings.HealthCheckTopicName!)
                    {
                        FullyQualifiedNamespace = settings.Namespace,
                        ConnectionString = settings.ConnectionString,
                        Credential = settings.Credential
                    });

        protected override bool GetHealthCheckEnabled(AzureMessagingServiceBusSettings settings)
            => !string.IsNullOrEmpty(settings.HealthCheckQueueName) || !string.IsNullOrEmpty(settings.HealthCheckTopicName);

        protected override TokenCredential? GetTokenCredential(AzureMessagingServiceBusSettings settings)
            => settings.Credential;

        protected override bool GetTracingEnabled(AzureMessagingServiceBusSettings settings)
            => settings.Tracing;

        protected override void Validate(AzureMessagingServiceBusSettings settings, string configurationSectionName)
        {
            if (string.IsNullOrEmpty(settings.ConnectionString) && string.IsNullOrEmpty(settings.Namespace))
            {
                throw new InvalidOperationException($"A ServiceBusClient could not be configured. Either specify a 'ConnectionString' or 'Namespace' in '{configurationSectionName}' configuration section.");
            }
        }

        protected override void LoadCustomSettings(AzureMessagingServiceBusSettings settings, IConfiguration rootConfiguration, string configurationSectionName)
        {
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                settings.ConnectionString = rootConfiguration.GetConnectionString(configurationSectionName);
            }
        }
    }
}
