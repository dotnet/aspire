// Assembly 'Aspire.Azure.Security.KeyVault'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Security.KeyVault;

public sealed class AzureSecurityKeyVaultSettings : IConnectionStringSettings
{
    public Uri? VaultUri { get; set; }
    public TokenCredential? Credential { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public AzureSecurityKeyVaultSettings();
}
