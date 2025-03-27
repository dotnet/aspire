// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core.Extensions;
using Azure.Security.KeyVault.Certificates;

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
}
