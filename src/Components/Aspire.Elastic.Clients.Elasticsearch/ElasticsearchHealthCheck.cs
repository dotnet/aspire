// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Elastic.Transport;
using System.Diagnostics;
using static Elastic.Clients.Elasticsearch.HealthStatus;

// TODO: Use health check from AspNetCore.Diagnostics.HealthChecks once following PR released:
// https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/pull/2244
namespace Aspire.Elastic.Clients.Elasticsearch;
internal sealed class ElasticsearchHealthCheck : IHealthCheck
{
    private static readonly ConcurrentDictionary<string, ElasticsearchClient> s_connections = new();

    private readonly ElasticsearchOptions _options;

    public ElasticsearchHealthCheck(ElasticsearchOptions options)
    {
        Debug.Assert(options.Uri is not null || options.Client is not null || options.AuthenticateWithElasticCloud);
        _options = Guard.ThrowIfNull(options);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            ElasticsearchClient? elasticsearchClient = null;
            if (_options.Client is not null)
            {
                elasticsearchClient = _options.Client;
            }
            else
            {
                ElasticsearchClientSettings? settings = null;

                settings = _options.AuthenticateWithElasticCloud
               ? new ElasticsearchClientSettings(_options.CloudId!, new ApiKey(_options.ApiKey!))
               : new ElasticsearchClientSettings(new Uri(_options.Uri!));

                if (_options.RequestTimeout.HasValue)
                {
                    settings = settings.RequestTimeout(_options.RequestTimeout.Value);
                }

                if (!s_connections.TryGetValue(_options.Uri!, out elasticsearchClient))
                {

                    if (_options.AuthenticateWithBasicCredentials)
                    {
                        settings = settings.Authentication(new BasicAuthentication(_options.UserName!, _options.Password!));
                    }
                    else if (_options.AuthenticateWithCertificate)
                    {
                        settings = settings.ClientCertificate(_options.Certificate!);
                    }
                    else if (_options.AuthenticateWithApiKey)
                    {
                        settings.Authentication(new ApiKey(_options.ApiKey!));
                    }

                    if (_options.CertificateValidationCallback != null)
                    {
                        settings = settings.ServerCertificateValidationCallback(_options.CertificateValidationCallback);
                    }

                    elasticsearchClient = new ElasticsearchClient(settings);

                    if (!s_connections.TryAdd(_options.Uri!, elasticsearchClient))
                    {
                        elasticsearchClient = s_connections[_options.Uri!];
                    }
                }
            }

            if (_options.UseClusterHealthApi)
            {
                var healthResponse = await elasticsearchClient.Cluster.HealthAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (healthResponse.ApiCallDetails.HttpStatusCode != 200)
                {
                    return new HealthCheckResult(context.Registration.FailureStatus);
                }

                return healthResponse.Status switch
                {
                    Green => HealthCheckResult.Healthy(),
                    Yellow => HealthCheckResult.Degraded(),
                    _ => new HealthCheckResult(context.Registration.FailureStatus)
                };
            }

            var pingResult = await elasticsearchClient.PingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            bool isSuccess = pingResult.ApiCallDetails.HttpStatusCode == 200;

            return isSuccess
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
