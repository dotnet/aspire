// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.AppConfiguration;
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Azure.Common;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering and configuring Azure App Configuration in a .NET Aspire application.
/// </summary>
public static class AspireAppConfigurationExtensions
{
    internal const string DefaultConfigSectionName = "Aspire:Azure:AppConfiguration";

    /// <summary>
    /// Adds the Azure App Configuration to be configuration in the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="AzureAppConfigurationOptions"/>.</param>
    /// <param name="optional">Determines the behavior of the App Configuration provider when an exception occurs while loading data from server. If false, the exception is thrown. If true, the exception is suppressed and no settings are populated from Azure App Configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:AppConfiguration" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureAppConfigurationSettings.Endpoint"/> is not provided.</exception>
    public static void AddAzureAppConfiguration(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureAppConfigurationSettings>? configureSettings = null,
        Action<AzureAppConfigurationOptions>? configureOptions = null,
        bool optional = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AzureAppConfigurationSettings settings = GetSettings(builder.Configuration, connectionName, configureSettings);

        builder.Configuration.AddAzureAppConfiguration(
            options =>
            {
                options.Connect(settings.Endpoint, settings.Credential ?? new DefaultAzureCredential());
                configureOptions?.Invoke(options);
            },
            optional);

        builder.Services.AddAzureAppConfiguration(); // register IConfigurationRefresherProvider service

        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(traceBuilder =>
                    traceBuilder.AddSource(["Microsoft.Extensions.Configuration.AzureAppConfiguration"]));
        }
    }

    /// <summary>
    /// Adds the Azure App Configuration to be configuration values in the <paramref name="configurationManager"/>.
    /// </summary>
    /// <param name="configurationManager">The <see cref="IConfigurationManager"/> to add the secrets to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="AzureAppConfigurationOptions"/>.</param>
    /// <param name="optional">Determines the behavior of the App Configuration provider when an exception occurs while loading data from server. If false, the exception is thrown. If true, the exception is suppressed and no settings are populated from Azure App Configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:AppConfiguration" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureAppConfigurationSettings.Endpoint"/> is not provided.</exception>
    public static IConfigurationBuilder AddAzureAppConfiguration(
        this IConfigurationManager configurationManager,
        string connectionName,
        Action<AzureAppConfigurationSettings>? configureSettings = null,
        Action<AzureAppConfigurationOptions>? configureOptions = null,
        bool optional = false)
    {
        ArgumentNullException.ThrowIfNull(configurationManager);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AzureAppConfigurationSettings settings = GetSettings(configurationManager, connectionName, configureSettings);

        return configurationManager.AddAzureAppConfiguration(
            options =>
            {
                options.Connect(settings.Endpoint, settings.Credential ?? new DefaultAzureCredential());
                configureOptions?.Invoke(options);
            },
            optional);
    }

    private static AzureAppConfigurationSettings GetSettings(
        IConfiguration configuration,
        string connectionName,
        Action<AzureAppConfigurationSettings>? configureSettings)
    {
        IConfigurationSection configSection = configuration.GetSection(DefaultConfigSectionName);

        var settings = new AzureAppConfigurationSettings();
        configSection.Bind(settings);

        if (configuration.GetConnectionString(connectionName) is string connectionString)
        {
            ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);
        }

        configureSettings?.Invoke(settings);

        if (settings.Endpoint is null)
        {
            throw new InvalidOperationException($"Endpoint is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'Endpoint' key in the '{DefaultConfigSectionName}' configuration section.");
        }

        return settings;
    }
}
