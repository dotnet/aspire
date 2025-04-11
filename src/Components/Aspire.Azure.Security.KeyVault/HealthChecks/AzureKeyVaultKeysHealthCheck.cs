// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Security.KeyVault.HealthChecks;

/// <summary>
/// Creates a basic health check targeting an Azure Key Vault <see cref="KeyClient"/>.
/// </summary>
/// <param name="client">The configured <see cref="KeyClient"/> to use for the health check.</param>
/// <param name="options">The configuration options for the health check.</param>
internal sealed class AzureKeyVaultKeysHealthCheck(KeyClient client, AzureKeyVaultKeysHealthCheckOptions options) : IHealthCheck
{
    internal KeyClient Client => client;
    internal AzureKeyVaultKeysHealthCheckOptions Options => options;

    /// <summary>
    /// Executes a health check using the options provided via <see cref="AzureKeyVaultCertificatesHealthCheckOptions"/>.
    /// </summary>
    /// <param name="context">The context in which to perform the health check.</param>
    /// <param name="cancellationToken">The token to cancel the <see cref="Task"/>.</param>
    /// <returns>A <see cref="HealthCheckResult"/> representing the status of the <see cref="KeyClient"/> connection.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var keyName = Options.ItemName;

        try
        {
            await Client.GetKeyAsync(keyName, cancellationToken: cancellationToken).ConfigureAwait(false);

            return new HealthCheckResult(HealthStatus.Healthy);
        }
        catch (RequestFailedException azureEx) when (azureEx.Status == 404) // based on https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/core/Azure.Core/README.md#reporting-errors-requestfailedexception
        {
            if (Options.CreateWhenNotFound)
            {
                // When this call fails, the exception is caught by upper layer.
                // From https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks#create-health-checks:
                // "If CheckHealthAsync throws an exception during the check, a new HealthReportEntry is returned with its HealthReportEntry.Status set to the FailureStatus."
                await Client.CreateKeyAsync(keyName, KeyType.Rsa, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // The secret was not found, but it's fine as all we care about is whether it's possible to connect.
            return new HealthCheckResult(HealthStatus.Healthy);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}

internal sealed class AzureKeyVaultKeysHealthCheckOptions
    : AzureKeyVaultExtendedHealthCheckOptions<AzureKeyVaultKeysHealthCheck>
{ }
