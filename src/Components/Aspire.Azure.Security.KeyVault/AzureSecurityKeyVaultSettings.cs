// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;

namespace Aspire.Azure.Security.KeyVault;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Key Vault.
/// </summary>
public sealed class AzureSecurityKeyVaultSettings : IConnectionStringSettings
{
    /// <summary>
    /// A <see cref="Uri"/> to the vault on which the client operates. Appears as "DNS Name" in the Azure portal.
    /// If you have a secret <see cref="Uri"/>, use <see cref="KeyVaultSecretIdentifier"/> to parse the <see cref="KeyVaultSecretIdentifier.VaultUri"/> and other information.
    /// You should validate that this URI references a valid Key Vault resource. See <see href="https://aka.ms/azsdk/blog/vault-uri"/> for details.
    /// </summary>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure Key Vault.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Key Vault health check is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.</para>
    /// <para>Disabled by default.</para>
    /// </summary>
    /// <remarks>
    /// ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    public bool Tracing { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString) &&
            Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            VaultUri = uri;
        }
    }
}
