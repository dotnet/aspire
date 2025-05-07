// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.AI.Projects;
using Aspire.Azure.Common;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// /// Provides extension methods for registering <see cref="AIProjectClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AzureAIProjectExtensions
{
    internal const string DefaultConfigSectionName = "Aspire:Azure:AI:Projects";

    /// <summary>
    /// Registers <see cref="AIProjectClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAIProjectSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{AIProjectClient, AIProjectClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:AI:Projects" section.</remarks>
    public static void AddAzureAIProjectClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureAIProjectSettings>? configureSettings = null,
        Action<IAzureClientBuilder<AIProjectClient, AIProjectClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new AIProjectComponent().AddClient(
            builder,
            DefaultConfigSectionName,
            configureSettings,
            configureClientBuilder,
            connectionName,
            serviceKey: null
        );
    }

    /// <summary>
    /// Registers <see cref="AIProjectClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAIProjectSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{AIProjectClient, AIProjectClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:AI:Projects" section.</remarks>
    public static void AddKeyedAzureAIProjectClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureAIProjectSettings>? configureSettings = null,
        Action<IAzureClientBuilder<AIProjectClient, AIProjectClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new AIProjectComponent().AddClient(
            builder,
            DefaultConfigSectionName,
            configureSettings,
            configureClientBuilder,
            connectionName: name,
            serviceKey: name
        );
    }

    private sealed class AIProjectComponent : AzureComponent<AzureAIProjectSettings, AIProjectClient, AIProjectClientOptions>
    {
        protected override IAzureClientBuilder<AIProjectClient, AIProjectClientOptions> AddClient(AzureClientFactoryBuilder azureFactoryBuilder, AzureAIProjectSettings settings, string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<AIProjectClient, AIProjectClientOptions>((options, creds) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException($"An AIProjectClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' in the '{configurationSectionName}' configuration section.");
                }

                // AIProjectClient takes a TokenCredential instance so it either has to be a provided credential
                // or we'll fall back to DefaultAzureCredential.
                return new AIProjectClient(
                    connectionString,
                    settings.Credential ?? new DefaultAzureCredential(),
                    options);
            });
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<AIProjectClient, AIProjectClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureAIProjectSettings settings, IConfiguration configuration)
            => configuration.Bind(settings);

        protected override IHealthCheck CreateHealthCheck(AIProjectClient client, AzureAIProjectSettings settings)
            => throw new NotImplementedException();

        protected override bool GetHealthCheckEnabled(AzureAIProjectSettings settings)
            => false;

        protected override TokenCredential? GetTokenCredential(AzureAIProjectSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureAIProjectSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureAIProjectSettings settings)
            => !settings.DisableTracing;
    }
}
