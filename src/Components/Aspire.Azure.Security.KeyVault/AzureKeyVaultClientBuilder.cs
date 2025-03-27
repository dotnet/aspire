// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Security.KeyVault;

/// <summary>
/// 
/// </summary>
/// <param name="host"></param>
/// <param name="connectionName"></param>
/// <param name="configureSettings"></param>
public class AzureKeyVaultClientBuilder(
    IHostApplicationBuilder host,
    string connectionName,
    Action<AzureSecurityKeyVaultSettings>? configureSettings )
{
    internal string DefaultConfigSectionName { get; } = AzureKeyVaultComponentConstants.s_defaultConfigSectionName;

    internal IHostApplicationBuilder HostBuilder { get; } = host;

    internal string ConnectionName { get; } = connectionName;

    internal Action<AzureSecurityKeyVaultSettings>? ConfigureSettings { get; } = configureSettings;
}
