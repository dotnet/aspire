// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.HealthChecks.Minio;
public class MinioHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _minioHealthLiveUri;
        private readonly Uri _minioHealthClusterUri;
        private readonly Uri _minioHealthClusterReadUri;

        public MinioHealthCheck(HttpClient httpClient, string minioBaseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _minioHealthLiveUri = new Uri($"{minioBaseUrl}/minio/health/live");
            _minioHealthClusterUri = new Uri($"{minioBaseUrl}/minio/health/cluster");
            _minioHealthClusterReadUri = new Uri($"{minioBaseUrl}/minio/health/cluster/read");
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Node Liveness Check
                var livenessResponse = await _httpClient.GetAsync(_minioHealthLiveUri, cancellationToken).ConfigureAwait(true);
                if (!livenessResponse.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Unhealthy("MinIO is not responding to liveness checks");
                }

                // Cluster Write Quorum Check
                var clusterWriteResponse = await _httpClient.GetAsync(_minioHealthClusterUri, cancellationToken).ConfigureAwait(true);
                if (!clusterWriteResponse.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Unhealthy("MinIO cluster does not have write quorum");
                }

                // Cluster Read Quorum Check
                var clusterReadResponse = await _httpClient.GetAsync(_minioHealthClusterReadUri, cancellationToken).ConfigureAwait(true);
                if (!clusterReadResponse.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Unhealthy("MinIO cluster does not have read quorum");
                }

                return HealthCheckResult.Healthy("MinIO is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Error occurred while checking MinIO health", ex);
            }
        }
    }

