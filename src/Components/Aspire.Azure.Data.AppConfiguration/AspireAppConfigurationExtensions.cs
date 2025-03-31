// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Data.AppConfiguration;
using Azure.Core.Extensions;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering and configuring Azure App Configuration in a .NET Aspire application.
/// </summary>
public static class AspireAppConfigurationExtensions
{
    internal const string DefaultConfigSectionName = "Aspire:Azure:Data:AppConfiguration";

    /// <summary>
    /// Registers <see cref="ConfigurationClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:AppConfiguration" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureDataAppConfigurationSettings.Endpoint"/> is not provided.</exception>
    public static void AddAzureAppConfigurationClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureDataAppConfigurationSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new AppConfigurationComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="ConfigurationClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection information from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:AppConfiguration" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureDataAppConfigurationSettings.Endpoint"/> is not provided.</exception>
    public static void AddKeyedAzureAppConfigurationClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureDataAppConfigurationSettings>? configureSettings = null,
        Action<IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new AppConfigurationComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Adds the Azure App Configuration to be configuration values in the <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfigurationManager"/> to add the secrets to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="AzureAppConfigurationOptions"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:AppConfiguration" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureDataAppConfigurationSettings.Endpoint"/> is not provided.</exception>
    public static IConfigurationBuilder AddAzureAppConfiguration(
        this IConfigurationManager configuration,
        string connectionName,
        Action<AzureDataAppConfigurationSettings>? configureSettings = null,
        Action<AzureAppConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new AzureDataAppConfigurationSettings();

        if (configuration.GetConnectionString(connectionName) is string connectionString)
        {
            if (!string.IsNullOrEmpty(connectionString) &&
                Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                settings.Endpoint = uri;
            }
        }

        configureSettings?.Invoke(settings);

        if (settings.Endpoint is null)
        {
            throw new InvalidOperationException($"Endpoint is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'Endpoint' key in the '{DefaultConfigSectionName}' configuration section.");
        }

        return configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(settings.Endpoint, settings.Credential ?? new DefaultAzureCredential());
            configureOptions?.Invoke(options);
        });
    }
}
