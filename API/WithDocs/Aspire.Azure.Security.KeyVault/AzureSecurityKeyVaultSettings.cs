// Assembly 'Aspire.Azure.Security.KeyVault'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Security.KeyVault;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Key Vault.
/// </summary>
public sealed class AzureSecurityKeyVaultSettings : IConnectionStringSettings
{
    /// <summary>
    /// A <see cref="T:System.Uri" /> to the vault on which the client operates. Appears as "DNS Name" in the Azure portal.
    /// If you have a secret <see cref="T:System.Uri" />, use <see cref="T:Azure.Security.KeyVault.Secrets.KeyVaultSecretIdentifier" /> to parse the <see cref="P:Azure.Security.KeyVault.Secrets.KeyVaultSecretIdentifier.VaultUri" /> and other information.
    /// You should validate that this URI references a valid Key Vault resource. See <see href="https://aka.ms/azsdk/blog/vault-uri" /> for details.
    /// </summary>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure Key Vault.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Key Vault health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Tracing { get; set; }

    public AzureSecurityKeyVaultSettings();
}
