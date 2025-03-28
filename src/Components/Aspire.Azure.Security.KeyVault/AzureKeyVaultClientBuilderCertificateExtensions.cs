// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Security.KeyVault;
using Aspire.Azure.Security.KeyVault.HealthChecks;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extends the <see cref="AzureKeyVaultClientBuilder"/> for optionally registering a <see cref="CertificateClient"/>.
/// </summary>
public static class AzureKeyVaultClientBuilderCertificateExtensions
{
    /// <summary>
    /// Registers a <see cref="CertificateClient"/> as a singleton into the services provided by the <paramref name="builder"/>
    /// </summary>
    /// <param name="builder">The <see cref="AzureKeyVaultClientBuilder"/> being used to register Key Vault Clients.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="CertificateClient"/>.</param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    /// <exception cref="ArgumentException">Thrown if the mandatory <paramref name="builder"/> is null.</exception>
    public static AzureKeyVaultClientBuilder AddCertificateClient(
    this AzureKeyVaultClientBuilder builder,
    Action<IAzureClientBuilder<CertificateClient, CertificateClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.InnerAddCertificateClient(configureClientBuilder);
    }

    /// <summary>
    /// Registers a keyed <see cref="CertificateClient"/> as a singleton into the services provided by the <paramref name="builder"/>
    /// </summary>
    /// <param name="builder">The <see cref="AzureKeyVaultClientBuilder"/> being used to register Key Vault Clients.</param>
    /// <param name="serviceKey">The name to call the <see cref="CertificateClient"/> singleton service.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="CertificateClient"/></param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the mandatory <paramref name="serviceKey"/> is null or empty.</exception>
    public static AzureKeyVaultClientBuilder AddKeyedCertificateClient(
    this AzureKeyVaultClientBuilder builder,
    string serviceKey,
    Action<IAzureClientBuilder<CertificateClient, CertificateClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(serviceKey);

        return builder.InnerAddCertificateClient(configureClientBuilder, serviceKey);
    }

    /// <summary>
    /// Implements the creation of a <see cref="CertificateClient"/> as an <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/>
    /// </summary>
    /// <param name="builder">Used to register AzureKeyVault clients.</param>
    /// <param name="serviceKey">The name to call the <see cref="CertificateClient"/> singleton service.</param>
    /// <param name="configureClientBuilder">Optional configuration for the <see cref="CertificateClient"/>.</param>
    /// <returns>A <see cref="AzureKeyVaultClientBuilder"/> to configure further clients.</returns>
    private static AzureKeyVaultClientBuilder InnerAddCertificateClient(
        this AzureKeyVaultClientBuilder builder,
        Action<IAzureClientBuilder<CertificateClient, CertificateClientOptions>>? configureClientBuilder = null,
        string? serviceKey = null)
    {
        new KeyVaultCertificateComponent()
            .AddClient(builder.HostBuilder, builder.DefaultConfigSectionName, builder.ConfigureSettings,
                        configureClientBuilder, builder.ConnectionName, serviceKey);

        return builder;
    }

    /// <summary>
    /// Representation of an <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/> configured as a <see cref="CertificateClient"/>
    /// </summary>
    private sealed class KeyVaultCertificateComponent : AbstractKeyVaultComponent<CertificateClient, CertificateClientOptions>
    {
        internal override CertificateClient CreateComponentClient(Uri vaultUri, CertificateClientOptions options, TokenCredential cred)
            => new(vaultUri, cred, options);

        protected override IHealthCheck CreateHealthCheck(CertificateClient client, AzureSecurityKeyVaultSettings settings)
            => new AzureKeyVaultCertificatesHealthCheck(client, new AzureKeyVaultCertificatesHealthCheckOptions());

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<CertificateClient, CertificateClientOptions> clientBuilder, IConfiguration configuration)
            => clientBuilder.ConfigureOptions(options => configuration.Bind(options));

        protected override void BindSettingsToConfiguration(AzureSecurityKeyVaultSettings settings, IConfiguration configuration)
            => configuration.Bind(settings);
    }
}
