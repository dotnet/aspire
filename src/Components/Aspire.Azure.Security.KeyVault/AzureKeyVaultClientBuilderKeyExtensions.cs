// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Security.KeyVault;
using Aspire.Azure.Security.KeyVault.HealthChecks;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extends the <see cref="AzureKeyVaultClientBuilder"/> for optionally registering a <see cref="KeyClient"/>.
/// </summary>
public static class AzureKeyVaultClientBuilderKeyExtensions
{
    /// <summary>
    /// Registers a <see cref="KeyClient"/> as a singleton into the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="AzureKeyVaultClientBuilder"/> being used to register Key Vault Clients.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="KeyClient"/>.</param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    /// <exception cref="ArgumentException">Thrown if the mandatory <paramref name="builder"/> is null.</exception>
    public static AzureKeyVaultClientBuilder AddKeyClient(
    this AzureKeyVaultClientBuilder builder,
    Action<IAzureClientBuilder<KeyClient, KeyClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.InnerAddKeyClient(configureClientBuilder);
    }

    /// <summary>
    /// Registers a keyed <see cref="KeyClient"/> as a singleton into the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="AzureKeyVaultClientBuilder"/> being used to register Key Vault Clients.</param>
    /// <param name="serviceKey">The name to call the <see cref="KeyClient"/> singleton service.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="KeyClient"/></param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the mandatory <paramref name="serviceKey"/> is null or empty.</exception>
    /// <returns></returns>
    public static AzureKeyVaultClientBuilder AddKeyedKeyClient(
    this AzureKeyVaultClientBuilder builder,
    string serviceKey,
    Action<IAzureClientBuilder<KeyClient, KeyClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(serviceKey);

        return builder.InnerAddKeyClient(configureClientBuilder, serviceKey);
    }

    /// <summary>
    /// Implements the creation of a <see cref="KeyClient"/> as an <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/>
    /// </summary>
    /// <param name="builder">Used to register AzureKeyVault clients.</param>
    /// <param name="serviceKey">The name to call the <see cref="KeyClient"/> singleton service.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="KeyClient"/>.</param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    private static AzureKeyVaultClientBuilder InnerAddKeyClient(
        this AzureKeyVaultClientBuilder builder,
        Action<IAzureClientBuilder<KeyClient, KeyClientOptions>>? configureClientBuilder = null,
        string? serviceKey = null)
    {
        new KeyVaultKeyComponent()
            .AddClient(builder.HostBuilder, builder.DefaultConfigSectionName, builder.ConfigureSettings,
                       configureClientBuilder, builder.ConnectionName, serviceKey);

        return builder;
    }

    /// <summary>
    /// Representation of an <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/> configured as a <see cref="KeyClient"/>
    /// </summary>
    private sealed class KeyVaultKeyComponent : AbstractKeyVaultComponent<KeyClient, KeyClientOptions>
    {
        protected override IHealthCheck CreateHealthCheck(KeyClient client, AzureSecurityKeyVaultSettings settings)
            => new AzureKeyVaultKeysHealthCheck(client, new AzureKeyVaultKeysHealthCheckOptions());

        internal override KeyClient CreateComponentClient(Uri vaultUri, KeyClientOptions options, TokenCredential cred)
            => new(vaultUri, cred, options);

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<KeyClient, KeyClientOptions> clientBuilder, IConfiguration configuration)
            => clientBuilder.ConfigureOptions(options => configuration.Bind(options));

        protected override void BindSettingsToConfiguration(AzureSecurityKeyVaultSettings settings, IConfiguration configuration)
            => configuration.Bind(settings);
    }
}
