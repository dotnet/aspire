// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core.Extensions;
using Azure.Security.KeyVault.Keys;

namespace Aspire.Azure.Security.KeyVault;

/// <summary>
/// 
/// </summary>
public static class AzureKeyVaultClientBuilderKeyExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureClientBuilder"></param>
    /// <returns></returns>
    public static AzureKeyVaultClientBuilder AddKeyClient(
    this AzureKeyVaultClientBuilder builder,
    Action<IAzureClientBuilder<KeyClient, KeyClientOptions>>? configureClientBuilder = null)
    {
        return builder.InnerAddKeyClient(configureClientBuilder);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serviceKey"></param>
    /// <param name="configureClientBuilder"></param>
    /// <returns></returns>
    public static AzureKeyVaultClientBuilder AddKeyedKeyClient(
    this AzureKeyVaultClientBuilder builder,
    string serviceKey,
    Action<IAzureClientBuilder<KeyClient, KeyClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceKey);

        return builder.InnerAddKeyClient(configureClientBuilder, serviceKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureClientBuilder"></param>
    /// <param name="serviceKey"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static AzureKeyVaultClientBuilder InnerAddKeyClient(
        this AzureKeyVaultClientBuilder builder,
        Action<IAzureClientBuilder<KeyClient, KeyClientOptions>>? configureClientBuilder = null,
        string? serviceKey = null)
    {
        throw new NotImplementedException();
    }
}
