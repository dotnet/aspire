// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Security.KeyVault;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// A builder used for creating one or more Key Vault Clients, registered into the <paramref name="host"/>.
/// </summary>
/// <param name="host">The <see cref="IHostApplicationBuilder"/> to register the clients to as singletons.</param>
/// <param name="connectionName">The name used to retrieve the VaultUri from ConnectionStrings in the configuration provider.</param>
/// <param name="configureSettings">An optional configuration point for the overall <see cref="AzureSecurityKeyVaultSettings"/> applied to each Key Vault Client.</param>
public class AzureKeyVaultClientBuilder(
    IHostApplicationBuilder host,
    string connectionName,
    Action<AzureSecurityKeyVaultSettings>? configureSettings)
{
    /// <summary>
    /// The default name of the configuration section for Key Vault.
    /// </summary>
    internal string DefaultConfigSectionName { get; } = AzureKeyVaultComponentConstants.s_defaultConfigSectionName;

    /// <summary>
    /// The <see cref="IHostApplicationBuilder"/> to register Key Vault Clients into as singletons.
    /// </summary>
    internal IHostApplicationBuilder HostBuilder { get; } = host;

    /// <summary>
    /// <para>The name used to retrieve the VaultUri from ConnectionStrings in the configuration provider.</para>
    /// <para>Setting the value after the initial creation allows for keyed clients of different types to have separate ConnectionStrings configuration names.</para>
    /// <para>For example: ConnectionStrings.MyKeyedSecretClient in previous builder stage will become ConnectionStrings.MyKeyedKeyClient.</para>
    /// </summary>
    internal string ConnectionName { get; set; } = connectionName;

    /// <summary>
    /// An optional configuration point for the overall <see cref="AzureSecurityKeyVaultSettings"/> applied to each Key Vault Client.
    /// </summary>
    internal Action<AzureSecurityKeyVaultSettings>? ConfigureSettings { get; } = configureSettings;
}
