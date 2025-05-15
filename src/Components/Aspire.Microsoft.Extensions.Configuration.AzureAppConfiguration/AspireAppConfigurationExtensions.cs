// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Microsoft.Extensions.Configuration.AzureAppConfiguration;
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
    internal const string DefaultConfigSectionName = "Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration";

    /// <summary>
    /// Adds the Azure App Configuration to be configuration in the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="AzureAppConfigurationOptions"/>.</param>
    /// <remarks>Reads the settings from "Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureAppConfigurationSettings.Endpoint"/> is not provided.</exception>
    public static void AddAzureAppConfiguration(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureAppConfigurationSettings>? configureSettings = null,
        Action<AzureAppConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        IConfigurationSection configSection = builder.Configuration.GetSection(DefaultConfigSectionName);

        var settings = new AzureAppConfigurationSettings();
        configSection.Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);
        }

        configureSettings?.Invoke(settings);

        if (settings.Endpoint is null)
        {
            throw new InvalidOperationException($"Endpoint is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'Endpoint' key in the '{DefaultConfigSectionName}' configuration section.");
        }

        builder.Configuration.AddAzureAppConfiguration(
            options =>
            {
                options.Connect(settings.Endpoint, settings.Credential ?? new DefaultAzureCredential());
                configureOptions?.Invoke(options);
            },
            settings.Optional);

        builder.Services.AddAzureAppConfiguration(); // register IConfigurationRefresherProvider service

        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(traceBuilder =>
                    traceBuilder.AddSource(["Microsoft.Extensions.Configuration.AzureAppConfiguration"]));
        }
    }
}
