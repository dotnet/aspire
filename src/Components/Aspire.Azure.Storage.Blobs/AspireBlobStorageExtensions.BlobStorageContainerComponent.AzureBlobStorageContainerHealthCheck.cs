// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

partial class AspireBlobStorageExtensions
{
    partial class BlobStorageContainerComponent
    {
        /// <summary>
        /// Azure Blob Storage container health check.
        /// </summary>
        /// <param name="blobContainerClient">
        /// The <see cref="BlobContainerClient"/> used to perform Azure Blob Storage container operations.
        /// Azure SDK recommends treating clients as singletons <see href="https://devblogs.microsoft.com/azure-sdk/lifetime-management-and-thread-safety-guarantees-of-azure-sdk-net-clients/"/>,
        /// so this should be the exact same instance used by other parts of the application.
        /// </param>
        private sealed class AzureBlobStorageContainerHealthCheck(BlobContainerClient blobContainerClient) : IHealthCheck
        {
            /// <inheritdoc />
            public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            {
                try
                {
                    await blobContainerClient.ExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    return HealthCheckResult.Healthy();
                }
                catch (Exception ex)
                {
                    return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
                }
            }
        }
    }
}
