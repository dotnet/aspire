// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Microsoft.Azure.Cosmos;

/// <summary>
/// Represents a builder that can be used to register multiple container
/// instances against the same Cosmos database connection.
/// </summary>
public sealed class CosmosDatabaseBuilder(
    IHostApplicationBuilder hostBuilder,
    string connectionName,
    MicrosoftAzureCosmosSettings settings,
    CosmosClientOptions clientOptions)
{
    private CosmosClient? _client;

    internal CosmosDatabaseBuilder AddContainer(string name)
    {
        _client ??= AspireMicrosoftAzureCosmosExtensions.GetCosmosClient(connectionName, settings, clientOptions);

        var connectionInfo = hostBuilder.GetCosmosConnectionInfo(name) ?? throw new InvalidOperationException($"The connection string '{name}' does not exist.");

        hostBuilder.Services.AddKeyedSingleton(name, (sp, _) =>
        {
            if (string.IsNullOrEmpty(connectionInfo.ContainerName))
            {
                throw new InvalidOperationException(
                    $"A Container could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{name}'");
            }
            return _client.GetContainer(settings.DatabaseName, connectionInfo.ContainerName);
        });

        return this;
    }
}
