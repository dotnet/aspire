// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Orleans.Shared;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Clustering.AzureStorage;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Aspire.Orleans.Client;

public static class AspireOrleansClientExtensions
{
    private const string OrleansConfigKeyPrefix = "Orleans";

    public static IHostApplicationBuilder UseAspireOrleansClient(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOrleansClient(clientBuilder =>
        {
            var serverSettings = new OrleansServerSettings();
            builder.Configuration.GetSection(OrleansConfigKeyPrefix).Bind(serverSettings);

            clientBuilder.Configure<ClusterOptions>(o =>
            {
                if (!string.IsNullOrWhiteSpace(serverSettings.ServiceId))
                {
                    o.ServiceId = serverSettings.ServiceId;
                }
                if (!string.IsNullOrWhiteSpace(serverSettings.ClusterId))
                {
                    o.ClusterId = serverSettings.ClusterId;
                }
            });

            if (serverSettings.Clustering is { } clusteringSection && clusteringSection.Exists())
            {
                ApplyClusteringSettings(builder, clientBuilder, clusteringSection);
            }

            // Enable distributed tracing for open telemetry.
            clientBuilder.AddActivityPropagation();

            // Retry filter to wait for an active silo at startup
            clientBuilder.UseConnectionRetryFilter(async (ex, cancellationToken) =>
            {
                if (ex is SiloUnavailableException)
                {
                    await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
                    return true;
                }
                return false;
            });
        });

        return builder;
    }

    private static void ApplyClusteringSettings(IHostApplicationBuilder builder, IClientBuilder clientBuilder, IConfigurationSection configuration)
    {
        var connectionSettings = new ConnectionSettings();
        configuration.Bind(connectionSettings);

        var type = connectionSettings.ProviderType;
        var connectionName = connectionSettings.ConnectionName;

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException(message: "A value must be specified for \"Clustering.ConnectionType\".", innerException: null);
        }

        if (string.IsNullOrWhiteSpace(connectionName))
        {
            throw new ArgumentException(message: "A value must be specified for \"Clustering.ConnectionName\".", innerException: null);
        }

        if (string.Equals("LocalhostClustering", type, StringComparison.OrdinalIgnoreCase))
        {
            clientBuilder.UseLocalhostClustering();
        }
        else if (string.Equals("AzureTableStorageResource", type, StringComparison.OrdinalIgnoreCase))
        {
            // Configure a table service client in the dependency injection container.
            builder.AddKeyedAzureTableService(connectionName);

            // Configure Orleans to use the configured table service client.
            clientBuilder.UseAzureStorageClustering(optionsBuilder => optionsBuilder.Configure(
                (AzureStorageGatewayOptions options, IServiceProvider serviceProvider) =>
                {
                    var tableServiceClient = Task.FromResult(serviceProvider.GetRequiredKeyedService<TableServiceClient>(connectionName));
                    options.ConfigureTableServiceClient(() => tableServiceClient);
                }));
        }
        else
        {
            throw new NotSupportedException($"Unsupported connection type \"{type}\".");
        }
    }
}
