// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Security.KeyVault.HealthChecks;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Security.KeyVault;

/// <summary>
/// 
/// </summary>
public static class AzureKeyVaultClientBuilderCertificateExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureClientBuilder"></param>
    /// <returns></returns>
    public static AzureKeyVaultClientBuilder AddCertificateClient(
    this AzureKeyVaultClientBuilder builder,
    Action<IAzureClientBuilder<CertificateClient, CertificateClientOptions>>? configureClientBuilder = null)
    {
        return builder.InnerAddCertificateClient(configureClientBuilder);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serviceKey"></param>
    /// <param name="configureClientBuilder"></param>
    /// <returns></returns>
    public static AzureKeyVaultClientBuilder AddKeyedCertificateClient(
    this AzureKeyVaultClientBuilder builder,
    string serviceKey,
    Action<IAzureClientBuilder<CertificateClient, CertificateClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceKey);

        return builder.InnerAddCertificateClient(configureClientBuilder, serviceKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureClientBuilder"></param>
    /// <param name="serviceKey"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static AzureKeyVaultClientBuilder InnerAddCertificateClient(
        this AzureKeyVaultClientBuilder builder,
        Action<IAzureClientBuilder<CertificateClient, CertificateClientOptions>>? configureClientBuilder = null,
        string? serviceKey = null)
    {
        throw new NotImplementedException();
    }

    private class KeyVaultCertificateComponent : AzureComponent<AzureSecurityKeyVaultSettings, CertificateClient, CertificateClientOptions>
    {
        protected override IAzureClientBuilder<CertificateClient, CertificateClientOptions> AddClient(AzureClientFactoryBuilder azureFactoryBuilder, AzureSecurityKeyVaultSettings settings, string connectionName, string configurationSectionName)
        {
            throw new NotImplementedException();
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<CertificateClient, CertificateClientOptions> clientBuilder, IConfiguration configuration)
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            => clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200

        protected override void BindSettingsToConfiguration(AzureSecurityKeyVaultSettings settings, IConfiguration configuration)
            => configuration.Bind(settings);

        protected override IHealthCheck CreateHealthCheck(CertificateClient client, AzureSecurityKeyVaultSettings settings)
            => new AzureKeyVaultCertificatesHealthCheck(client, settings);

        protected override bool GetHealthCheckEnabled(AzureSecurityKeyVaultSettings settings)
        {
            throw new NotImplementedException();
        }

        protected override bool GetMetricsEnabled(AzureSecurityKeyVaultSettings settings)
        {
            throw new NotImplementedException();
        }

        protected override TokenCredential? GetTokenCredential(AzureSecurityKeyVaultSettings settings)
        {
            throw new NotImplementedException();
        }

        protected override bool GetTracingEnabled(AzureSecurityKeyVaultSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}
