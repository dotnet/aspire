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

public static class AspireKeyVaultExtensions
{
    public static void AddAzureKeyVaultClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureSecurityKeyVaultSettings>? configureSettings = null, Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureKeyVaultClient(this IHostApplicationBuilder builder, string name, Action<AzureSecurityKeyVaultSettings>? configureSettings = null, Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>? configureClientBuilder = null);
    public static void AddAzureKeyVaultSecrets(this IConfigurationManager configurationManager, string connectionName, Action<AzureSecurityKeyVaultSettings>? configureSettings = null, Action<SecretClientOptions>? configureClientOptions = null, AzureKeyVaultConfigurationOptions? options = null);
}
