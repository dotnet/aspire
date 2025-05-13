// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

internal sealed class AzureBlobStorageContainerHealthCheck(BlobContainerClient blobContainerClient) : IHealthCheck
{
    private readonly BlobContainerClient _blobServiceClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _blobServiceClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
