// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ClientModel;
using Aspire.Azure.AI.OpenAI;
using Aspire.Azure.Common;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="AzureOpenAIClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAzureOpenAIExtensions
{
    internal const string DefaultConfigSectionName = "Aspire:Azure:AI:OpenAI";

    /// <summary>
    /// Registers <see cref="AzureOpenAIClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    ///
    /// Additionally, registers the <see cref="AzureOpenAIClient"/> as an <see cref="OpenAIClient"/> service.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureOpenAISettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{AzureOpenAIClient, AzureOpenAIClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static void AddAzureOpenAIClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureOpenAISettings>? configureSettings = null,
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null)
    {
        new OpenAIComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);

        // Add the AzureOpenAIClient service as OpenAIClient. That way the service can be resolved by both service Types.
        builder.Services.TryAddSingleton(typeof(OpenAIClient), static provider => provider.GetRequiredService<AzureOpenAIClient>());
    }

    /// <summary>
    /// Registers <see cref="AzureOpenAIClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    ///
    /// Additionally, registers the <see cref="AzureOpenAIClient"/> as an <see cref="OpenAIClient"/> service.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureOpenAISettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{AzureOpenAIClient, OpenAIClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI:{name}" section.</remarks>
    public static void AddKeyedAzureOpenAIClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureOpenAISettings>? configureSettings = null,
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        new OpenAIComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);

        // Add the AzureOpenAIClient service as OpenAIClient. That way the service can be resolved by both service Types.
        builder.Services.TryAddKeyedSingleton(typeof(OpenAIClient), serviceKey: name, static (provider, key) => provider.GetRequiredKeyedService<AzureOpenAIClient>(key));
    }

    private sealed class OpenAIComponent : AzureComponent<AzureOpenAISettings, AzureOpenAIClient, AzureOpenAIClientOptions>
    {
        protected override string[] ActivitySourceNames => ["OpenAI.*"];

        protected override string[] MetricSourceNames => ["OpenAI.*"];

        protected override IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureOpenAISettings settings, string connectionName,
            string configurationSectionName)
        {
            return azureFactoryBuilder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>((options, _, _) =>
            {
                if (settings.Endpoint is null)
                {
                    throw new InvalidOperationException($"An OpenAIClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a '{nameof(AzureOpenAISettings.Endpoint)}' or '{nameof(AzureOpenAISettings.Key)}' in the '{configurationSectionName}' configuration section.");
                }
                else
                {
                    // Connect to Azure OpenAI

                    if (!string.IsNullOrEmpty(settings.Key))
                    {
                        var credential = new ApiKeyCredential(settings.Key);
                        return new AzureOpenAIClient(settings.Endpoint, credential, options);
                    }
                    else
                    {
                        return new AzureOpenAIClient(settings.Endpoint, settings.Credential ?? new DefaultAzureCredential(), options);
                    }
                }
            });
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureOpenAISettings settings, IConfiguration config)
        {
            config.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(AzureOpenAIClient client, AzureOpenAISettings settings)
        {
            throw new NotImplementedException();
        }

        protected override bool GetHealthCheckEnabled(AzureOpenAISettings settings)
        {
            return false;
        }

        protected override TokenCredential? GetTokenCredential(AzureOpenAISettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureOpenAISettings settings)
            => !settings.DisableMetrics;

        protected override bool GetTracingEnabled(AzureOpenAISettings settings)
            => !settings.DisableTracing;
    }
}
