// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Security.KeyVault.HealthChecks;

/// <summary>
/// Basis for Key Vault Client <see cref="IHealthCheck"/> options.
/// </summary>
/// <typeparam name="TKeyVaultClientHealthCheck"></typeparam>
internal class AzureKeyVaultExtendedHealthCheckOptions<TKeyVaultClientHealthCheck>
{
    /// <summary>
    /// Default naming of the reference item used for the Health Check.
    /// </summary>
    private string _itemName = nameof(TKeyVaultClientHealthCheck);

    /// <summary>
    /// The name of the item held in Key Vault to be used for the Health Check.
    /// </summary>
    public string ItemName
    {
        get => _itemName;
        set => _itemName = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// A boolean value that indicates whether the secret should be created when it's not found.
    /// <see langword="false"/> by default.
    /// </summary>
    /// <remarks>
    /// Enabling it requires secret set permissions and can be used to improve performance
    /// (secret not found is signaled via <see cref="RequestFailedException"/>).
    /// </remarks>
    public bool CreateWhenNotFound { get; set; }
}
