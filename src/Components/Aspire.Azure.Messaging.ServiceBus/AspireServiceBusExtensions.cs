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

/// <summary>
/// Provides extension methods for registering <see cref="ServiceBusClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireServiceBusExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Messaging:ServiceBus";

    /// <summary>
    /// Registers <see cref="ServiceBusClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzureServiceBusClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new ServiceBusClientComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusReceiver"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzureServiceBusReceiver(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusReceiver, ServiceBusReceiverOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new ServiceBusReceiverComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusSender"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzureServiceBusSender(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusSender, ServiceBusSenderOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new ServiceBusSenderComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusProcessor"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddAzureServiceBusProcessor(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusProcessor, ServiceBusProcessorOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new ServiceBusProcessorComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzureServiceBusClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new ServiceBusClientComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusReceiver"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzureServiceBusReceiver(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusReceiver, ServiceBusReceiverOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new ServiceBusReceiverComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusSender"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzureServiceBusSender(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusSender, ServiceBusSenderOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new ServiceBusSenderComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="ServiceBusProcessor"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureMessagingServiceBusSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Messaging:ServiceBus:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureMessagingServiceBusSettings.ConnectionString"/> nor <see cref="AzureMessagingServiceBusSettings.FullyQualifiedNamespace"/> is provided.</exception>
    public static void AddKeyedAzureServiceBusProcessor(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureMessagingServiceBusSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ServiceBusProcessor, ServiceBusProcessorOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new ServiceBusProcessorComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    private abstract class ServiceBusComponent<TClient, TOptions> : AzureComponent<AzureMessagingServiceBusSettings, TClient, TOptions>
        where TClient : class
        where TOptions : class
    {
        protected override IHealthCheck CreateHealthCheck(TClient client, AzureMessagingServiceBusSettings settings)
        {
            return !string.IsNullOrEmpty(settings.HealthCheckQueueName)
                ? new AzureServiceBusQueueHealthCheck(new AzureServiceBusQueueHealthCheckOptions(settings.HealthCheckQueueName)
                {
                    FullyQualifiedNamespace = settings.FullyQualifiedNamespace,
                    ConnectionString = settings.ConnectionString,
                    Credential = settings.Credential
                })
                : new AzureServiceBusTopicHealthCheck(new AzureServiceBusTopicHealthCheckOptions(settings.HealthCheckTopicName!)
                {
                    FullyQualifiedNamespace = settings.FullyQualifiedNamespace,
                    ConnectionString = settings.ConnectionString,
                    Credential = settings.Credential
                });
        }

        protected override void BindSettingsToConfiguration(AzureMessagingServiceBusSettings settings, IConfiguration config)
        {
            config.Bind(settings);
        }

        protected override bool GetHealthCheckEnabled(AzureMessagingServiceBusSettings settings)
            => !string.IsNullOrEmpty(settings.HealthCheckQueueName) || !string.IsNullOrEmpty(settings.HealthCheckTopicName);

        protected override TokenCredential? GetTokenCredential(AzureMessagingServiceBusSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureMessagingServiceBusSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureMessagingServiceBusSettings settings)
            => !settings.DisableTracing;
    }

    private sealed class ServiceBusClientComponent : ServiceBusComponent<ServiceBusClient, ServiceBusClientOptions>
    {
        protected override IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingServiceBusSettings settings,
            string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<ServiceBusClient, ServiceBusClientOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(settings.FullyQualifiedNamespace))
                {
                    throw new InvalidOperationException($"A ServiceBusClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Namespace' in the '{configurationSectionName}' configuration section.");
                }

                return !string.IsNullOrEmpty(connectionString) ?
                    new ServiceBusClient(connectionString, options) :
                    new ServiceBusClient(settings.FullyQualifiedNamespace, cred, options);
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }
    }

    private sealed class ServiceBusSenderComponent : ServiceBusComponent<ServiceBusSender, ServiceBusSenderOptions>
    {
        protected override IAzureClientBuilder<ServiceBusSender, ServiceBusSenderOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingServiceBusSettings settings,
            string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<ServiceBusSender, ServiceBusSenderOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(settings.FullyQualifiedNamespace))
                {
                    throw new InvalidOperationException($"A ServiceBusSender could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Namespace' in the '{configurationSectionName}' configuration section.");
                }

                if (string.IsNullOrEmpty(settings.QueueOrTopicName))
                {
                    throw new InvalidOperationException($"A ServiceBusSender could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'QueueOrTopicName' in the '{configurationSectionName}' configuration section.");
                }

                var client = !string.IsNullOrEmpty(connectionString) ?
                    new ServiceBusClient(connectionString) :
                    new ServiceBusClient(settings.FullyQualifiedNamespace);
                return client.CreateSender(settings.QueueOrTopicName!, options);
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<ServiceBusSender, ServiceBusSenderOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }
    }

    private sealed class ServiceBusReceiverComponent : ServiceBusComponent<ServiceBusReceiver, ServiceBusReceiverOptions>
    {
        protected override IAzureClientBuilder<ServiceBusReceiver, ServiceBusReceiverOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingServiceBusSettings settings,
            string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<ServiceBusReceiver, ServiceBusReceiverOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(settings.FullyQualifiedNamespace))
                {
                    throw new InvalidOperationException($"A ServiceBusReceiver could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Namespace' in the '{configurationSectionName}' configuration section.");
                }

                if (string.IsNullOrEmpty(settings.QueueOrTopicName) && string.IsNullOrEmpty(settings.SubscriptionName))
                {
                    throw new InvalidOperationException($"A ServiceBusReceiver could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'QueueOrTopicName' and, if using a subscription, 'SubscriptionName' in the '{configurationSectionName}' configuration section.");
                }

                var client = !string.IsNullOrEmpty(connectionString) ?
                    new ServiceBusClient(connectionString) :
                    new ServiceBusClient(settings.FullyQualifiedNamespace);
                return settings.SubscriptionName != null ?
                    client.CreateReceiver(settings.QueueOrTopicName, settings.SubscriptionName, options) :
                    client.CreateReceiver(settings.QueueOrTopicName, options);
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<ServiceBusReceiver, ServiceBusReceiverOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }
    }

    private sealed class ServiceBusProcessorComponent : ServiceBusComponent<ServiceBusProcessor, ServiceBusProcessorOptions>
    {
        protected override IAzureClientBuilder<ServiceBusProcessor, ServiceBusProcessorOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingServiceBusSettings settings,
            string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<ServiceBusProcessor, ServiceBusProcessorOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(settings.FullyQualifiedNamespace))
                {
                    throw new InvalidOperationException($"A ServiceBusProcessor could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Namespace' in the '{configurationSectionName}' configuration section.");
                }

                if (string.IsNullOrEmpty(settings.QueueOrTopicName) && string.IsNullOrEmpty(settings.SubscriptionName))
                {
                    throw new InvalidOperationException($"A ServiceBusProcessor could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'QueueOrTopicName' and, if using a subscription, 'SubscriptionName' in the '{configurationSectionName}' configuration section.");
                }

                var client = !string.IsNullOrEmpty(connectionString) ?
                    new ServiceBusClient(connectionString) :
                    new ServiceBusClient(settings.FullyQualifiedNamespace);
                return settings.SubscriptionName != null ?
                    client.CreateProcessor(settings.QueueOrTopicName, settings.SubscriptionName, options) :
                    client.CreateProcessor(settings.QueueOrTopicName, options);
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<ServiceBusProcessor, ServiceBusProcessorOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }
    }
}
