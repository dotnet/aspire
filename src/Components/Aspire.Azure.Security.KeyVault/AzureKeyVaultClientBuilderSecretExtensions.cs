// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Security.KeyVault;
using Azure.Core;
using Azure.Core.Extensions;
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
    /// Representation of an <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/> configured as a <see cref="SecretClient"/>
    /// </summary>
    private sealed class KeyVaultSecretsComponent : AbstractKeyVaultComponent<SecretClient, SecretClientOptions>
    {
        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<SecretClient, SecretClientOptions> clientBuilder, IConfiguration configuration)
            => clientBuilder.ConfigureOptions(configuration.Bind);

        protected override void BindSettingsToConfiguration(AzureSecurityKeyVaultSettings settings, IConfiguration configuration)
            => configuration.Bind(settings);

        protected override IHealthCheck CreateHealthCheck(SecretClient client, AzureSecurityKeyVaultSettings settings)
            => new AzureKeyVaultSecretsHealthCheck(client, new AzureKeyVaultSecretsHealthCheckOptions());

        internal override SecretClient CreateComponentClient(Uri vaultUri, SecretClientOptions options, TokenCredential cred)
            => new(vaultUri, cred, options);
    }
}
