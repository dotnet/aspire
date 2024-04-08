// Assembly 'Aspire.Azure.Security.KeyVault'

using System;
using Aspire.Azure.Common;
using Aspire.Azure.Security.KeyVault;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering and configuring Azure Key Vault secrets in a .NET Aspire application.
/// </summary>
public static class AspireKeyVaultExtensions
{
    /// <summary>
    /// Registers <see cref="T:Azure.Security.KeyVault.Secrets.SecretClient" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Security.KeyVault.AzureSecurityKeyVaultSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Security:KeyVault" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.Azure.Security.KeyVault.AzureSecurityKeyVaultSettings.VaultUri" /> is not provided.</exception>
    public static void AddAzureKeyVaultClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureSecurityKeyVaultSettings>? configureSettings = null, Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Security.KeyVault.Secrets.SecretClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection information from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Security.KeyVault.AzureSecurityKeyVaultSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Security:KeyVault:{name}" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.Azure.Security.KeyVault.AzureSecurityKeyVaultSettings.VaultUri" /> is not provided.</exception>
    public static void AddKeyedAzureKeyVaultClient(this IHostApplicationBuilder builder, string name, Action<AzureSecurityKeyVaultSettings>? configureSettings = null, Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Adds the Azure KeyVault secrets to be configuration values in the <paramref name="configurationManager" />.
    /// </summary>
    /// <param name="configurationManager">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationManager" /> to add the secrets to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Security.KeyVault.AzureSecurityKeyVaultSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientOptions">An optional method that can be used for customizing the <see cref="T:Azure.Security.KeyVault.Secrets.SecretClientOptions" />.</param>
    /// <param name="options">An optional <see cref="T:Azure.Extensions.AspNetCore.Configuration.Secrets.AzureKeyVaultConfigurationOptions" /> instance to configure the behavior of the configuration provider.</param>
    public static void AddAzureKeyVaultSecrets(this IConfigurationManager configurationManager, string connectionName, Action<AzureSecurityKeyVaultSettings>? configureSettings = null, Action<SecretClientOptions>? configureClientOptions = null, AzureKeyVaultConfigurationOptions? options = null);
}
