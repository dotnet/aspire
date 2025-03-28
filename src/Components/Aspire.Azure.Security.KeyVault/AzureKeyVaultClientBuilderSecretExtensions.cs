// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Security.KeyVault;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using HealthChecks.Azure.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// 
/// </summary>
public static class AzureKeyVaultClientBuilderSecretExtensions
{
    /// <summary>
    /// Registers a <see cref="SecretClient"/> as a singleton into the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">Used to register AzureKeyVault clients.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="SecretClient"/>.</param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    public static AzureKeyVaultClientBuilder AddSecretClient(
        this AzureKeyVaultClientBuilder builder,
        Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null)
    {
        return builder.InnerAddSecretClient(configureClientBuilder);
    }

    /// <summary>
    /// Registers a keyed <see cref="SecretClient"/> as a singleton into the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">Used to register AzureKeyVault clients.</param>
    /// <param name="serviceKey">The name to call the <see cref="SecretClient"/> singleton service.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="SecretClient"/>.</param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="serviceKey"/> is null or empty.</exception>
    public static AzureKeyVaultClientBuilder AddKeyedSecretClient(
    this AzureKeyVaultClientBuilder builder,
    string serviceKey,
    Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null)
    {
        return builder.InnerAddSecretClient(configureClientBuilder, serviceKey);
    }

    /// <summary>
    /// Implements the creation of a <see cref="SecretClient"/> as an <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/>
    /// </summary>
    /// <param name="builder">Used to register AzureKeyVault clients.</param>
    /// <param name="serviceKey">The name to call the <see cref="SecretClient"/> singleton service.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="SecretClient"/>.</param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    private static AzureKeyVaultClientBuilder InnerAddSecretClient(
        this AzureKeyVaultClientBuilder builder,
        Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null,
        string? serviceKey = null)
    {
        new KeyVaultSecretsComponent()
            .AddClient(builder.HostBuilder, builder.DefaultConfigSectionName, builder.ConfigureSettings,
                       configureClientBuilder, builder.ConnectionName, serviceKey);

        return builder;
    }

    /// <summary>
    /// Adds the Azure KeyVault secrets to be configuration values in the <paramref name="configurationManager"/>.
    /// </summary>
    /// <param name="configurationManager">The <see cref="IConfigurationManager"/> to add the secrets to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureSecurityKeyVaultSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientOptions">An optional method that can be used for customizing the <see cref="SecretClientOptions"/>.</param>
    /// <param name="options">An optional <see cref="AzureKeyVaultConfigurationOptions"/> instance to configure the behavior of the configuration provider.</param>
    public static void AddAzureKeyVaultSecrets(
        this IConfigurationManager configurationManager,
        string connectionName,
        Action<AzureSecurityKeyVaultSettings>? configureSettings = null,
        Action<SecretClientOptions>? configureClientOptions = null,
        AzureKeyVaultConfigurationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(configurationManager);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var client = configurationManager.GetSecretClient(connectionName, configureSettings, configureClientOptions);
        configurationManager.AddAzureKeyVault(client, options ?? new AzureKeyVaultConfigurationOptions());
    }

    private static SecretClient GetSecretClient(
        this IConfiguration configuration,
        string connectionName,
        Action<AzureSecurityKeyVaultSettings>? configureSettings,
        Action<SecretClientOptions>? configureOptions)
    {
        var configSection = configuration.GetSection(AzureKeyVaultComponentConstants.s_defaultConfigSectionName);

        var settings = new AzureSecurityKeyVaultSettings();
        configSection.Bind(settings);

        if (configuration.GetConnectionString(connectionName) is string connectionString)
        {
            ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);
        }

        configureSettings?.Invoke(settings);

        var clientOptions = new SecretClientOptions();
        configSection.GetSection("ClientOptions").Bind(clientOptions);
        configureOptions?.Invoke(clientOptions);

        if (settings.VaultUri is null)
        {
            throw new InvalidOperationException($"VaultUri is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'VaultUri' key in the '{AzureKeyVaultComponentConstants.s_defaultConfigSectionName}' configuration section.");
        }

        return new SecretClient(settings.VaultUri, settings.Credential ?? new DefaultAzureCredential(), clientOptions);
    }

    /// <summary>
    /// Representation of an <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/> configured as a <see cref="SecretClient"/>
    /// </summary>
    private sealed class KeyVaultSecretsComponent : AbstractKeyVaultComponent<SecretClient, SecretClientOptions>
    {
        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<SecretClient, SecretClientOptions> clientBuilder, IConfiguration configuration)
            => clientBuilder.ConfigureOptions(options => configuration.Bind(options));

        protected override void BindSettingsToConfiguration(AzureSecurityKeyVaultSettings settings, IConfiguration configuration)
            => configuration.Bind(settings);

        protected override IHealthCheck CreateHealthCheck(SecretClient client, AzureSecurityKeyVaultSettings settings)
            => new AzureKeyVaultSecretsHealthCheck(client, new AzureKeyVaultSecretsHealthCheckOptions());

        internal override SecretClient CreateComponentClient(Uri vaultUri, SecretClientOptions options, TokenCredential cred)
            => new(vaultUri, cred, options);
    }
}
