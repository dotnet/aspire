// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration;

namespace Aspire.Azure.Data.AppConfiguration;

/// <summary>
/// Provides extension methods for registering and configuring Azure App Configuration in a .NET Aspire application.
/// </summary>
public static class AspireAppConfigurationExtensions
{
    /// <summary>
    /// Adds the Azure App Configuration to be configuration values in the <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfigurationManager"/> to add the secrets to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataAppConfigurationSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="AzureAppConfigurationOptions"/>.</param>
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

        return configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(settings.Endpoint, settings.Credential ?? new DefaultAzureCredential());
            configureOptions?.Invoke(options);
        });
    }
}
