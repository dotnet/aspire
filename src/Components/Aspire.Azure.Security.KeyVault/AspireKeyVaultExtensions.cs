// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Security.KeyVault;
using Azure.Core.Extensions;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering and configuring Azure Key Vault secrets in a .NET Aspire application.
/// </summary>
public static class AspireKeyVaultExtensions
{
    /// <summary>
    /// Registers <see cref="SecretClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureSecurityKeyVaultSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Security:KeyVault" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureSecurityKeyVaultSettings.VaultUri"/> is not provided.</exception>
    public static void AddAzureKeyVaultClient(
    this IHostApplicationBuilder builder,
    string connectionName,
    Action<AzureSecurityKeyVaultSettings>? configureSettings = null,
    Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new AzureKeyVaultClientBuilder(builder, connectionName, configureSettings)
                   .AddSecretClient(configureClientBuilder);
    }

    /// <summary>
    /// Registers <see cref="SecretClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection information from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureSecurityKeyVaultSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Security:KeyVault:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureSecurityKeyVaultSettings.VaultUri"/> is not provided.</exception>
    public static void AddKeyedAzureKeyVaultClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureSecurityKeyVaultSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new AzureKeyVaultClientBuilder(builder, name, configureSettings)
                   .AddKeyedSecretClient(serviceKey: name, configureClientBuilder);
    }

    /// <summary>
    /// Registers <see cref="SecretClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureSecurityKeyVaultSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Security:KeyVault" section.</remarks>
    /// <returns> An instance of the <see cref="AzureKeyVaultClientBuilder"/> allowing for further Key Vault Clients to be registered.</returns>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureSecurityKeyVaultSettings.VaultUri"/> is not provided.</exception>
    public static AzureKeyVaultClientBuilder AddExtendedAzureKeyVaultClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureSecurityKeyVaultSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        return new AzureKeyVaultClientBuilder(builder, connectionName, configureSettings)
                   .AddSecretClient(configureClientBuilder);
    }

    /// <summary>
    /// Registers <see cref="SecretClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection information from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureSecurityKeyVaultSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Security:KeyVault:{name}" section.</remarks>
    /// /// <returns> An instance of the <see cref="AzureKeyVaultClientBuilder"/> allowing for further Key Vault Clients to be registered.</returns>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureSecurityKeyVaultSettings.VaultUri"/> is not provided.</exception>
    public static AzureKeyVaultClientBuilder AddExtendedKeyedAzureKeyVaultClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureSecurityKeyVaultSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new AzureKeyVaultClientBuilder(builder, name, configureSettings)
                   .AddKeyedSecretClient(serviceKey: name, configureClientBuilder);
    }
}
