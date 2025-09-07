// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Azure.Common;
using Azure.Core;
using Azure.Core.Extensions;
using Microsoft.Extensions.Azure;

namespace Aspire.Azure.Security.KeyVault;

/// <summary>
/// <para>Abstracts the common configuration binding required by <see cref="AzureComponent{TSettings, TClient, TClientOptions}"/></para> 
/// <para>Deriving type implements KeyVaultClient specific item:</para>
/// <para><see cref="AzureComponent{TSettings, TClient, TClientOptions}.CreateHealthCheck(TClient, TSettings)"/></para>
/// </summary>
/// <typeparam name="TClient">The KeyVaultClient type for this component.</typeparam>
/// <typeparam name="TOptions">The associated configuration for the <typeparamref name="TClient"/></typeparam>
internal abstract class AbstractAzureKeyVaultComponent<TClient, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptions>
    : AzureComponent<AzureSecurityKeyVaultSettings, TClient, TOptions>
    where TClient : class
    where TOptions : class
{
    internal abstract TClient CreateComponentClient(Uri vaultUri, TOptions options, TokenCredential cred);

    protected override IAzureClientBuilder<TClient, TOptions> AddClient(AzureClientFactoryBuilder azureFactoryBuilder, AzureSecurityKeyVaultSettings settings, string connectionName, string configurationSectionName)
    {
        return azureFactoryBuilder.AddClient<TClient, TOptions>((options, cred, _) =>
        {
            if (settings.VaultUri is null)
            {
                throw new InvalidOperationException($"VaultUri is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'VaultUri' key in the '{configurationSectionName}' configuration section.");
            }

            return CreateComponentClient(settings.VaultUri, options, cred);
        });
    }

    protected override bool GetHealthCheckEnabled(AzureSecurityKeyVaultSettings settings)
        => !settings.DisableHealthChecks;

    protected override TokenCredential? GetTokenCredential(AzureSecurityKeyVaultSettings settings)
        => settings.Credential;

    protected override bool GetMetricsEnabled(AzureSecurityKeyVaultSettings settings)
        => false;

    protected override bool GetTracingEnabled(AzureSecurityKeyVaultSettings settings)
        => !settings.DisableTracing;
}
