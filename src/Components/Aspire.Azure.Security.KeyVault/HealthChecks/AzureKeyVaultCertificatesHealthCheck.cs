// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Security.KeyVault.HealthChecks;

/// <summary>
/// Creates a basic health check targeting an Azure Key Vault <see cref="CertificateClient"/>
/// </summary>
/// <param name="client">The configured <see cref="CertificateClient"/> to use for the health check.</param>
/// <param name="options"></param>
internal sealed class AzureKeyVaultCertificatesHealthCheck(CertificateClient client, AzureKeyVaultCertificatesHealthCheckOptions options) : IHealthCheck
{
    internal CertificateClient Client => client;
    internal AzureKeyVaultCertificatesHealthCheckOptions Options => options;

    /// <summary>
    /// Executes a health check using the options provided via <see cref="AzureKeyVaultKeysHealthCheckOptions"/>.
    /// </summary>
    /// <param name="context">The context in which to perform the health check.</param>
    /// <param name="cancellationToken">The token to cancel the <see cref="Task"/>.</param>
    /// <returns>A <see cref="HealthCheckResult"/> representing the status of the <see cref="CertificateClient"/> connection.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var certificateName = Options.ItemName;

        try
        {
            await Client.GetCertificateAsync(certificateName, cancellationToken).ConfigureAwait(false);

            return new HealthCheckResult(HealthStatus.Healthy);
        }
        catch (RequestFailedException azureEx) when (azureEx.Status == 404) // based on https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/core/Azure.Core/README.md#reporting-errors-requestfailedexception
        {
            // Retaining structure to mimic -> AspNetCore.HealthChecks.Azure.KeyVault.Secrets
            if (Options.CreateWhenNotFound)
            {
                throw new NotImplementedException();
            }

            return new HealthCheckResult(HealthStatus.Healthy);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}

internal sealed class AzureKeyVaultCertificatesHealthCheckOptions
    : AzureKeyVaultExtendedHealthCheckOptions<AzureKeyVaultCertificatesHealthCheck>
{
    /// <summary>
    /// CreateCertificate{Async} starts a long running process, inappropriate for a Health Check.
    /// </summary>
    public AzureKeyVaultCertificatesHealthCheckOptions() => CreateWhenNotFound = false;
}
