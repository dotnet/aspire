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

    internal CosmosDatabaseBuilder AddDatabase()
    {
        hostBuilder.Services.AddSingleton(sp =>
        {
            if (string.IsNullOrEmpty(settings.DatabaseName))
            {
                throw new InvalidOperationException(
                    $"A Database could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}'.");
            }
            _client ??= AspireMicrosoftAzureCosmosExtensions.GetCosmosClient(connectionName, settings, clientOptions);
            return _client.GetDatabase(settings.DatabaseName);
        });

        return this;
    }

    internal CosmosDatabaseBuilder AddKeyedDatabase()
    {
        hostBuilder.Services.AddKeyedSingleton(connectionName, (sp, _) =>
        {
            if (string.IsNullOrEmpty(settings.DatabaseName))
            {
                throw new InvalidOperationException(
                    $"A Database could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}'.");
            }
            _client ??= AspireMicrosoftAzureCosmosExtensions.GetCosmosClient(connectionName, settings, clientOptions);
            return _client.GetDatabase(settings.DatabaseName);
        });

        return this;
    }

    /// <summary>
    /// Register a <see cref="Container"/> against the database managed with <see cref="CosmosDatabaseBuilder"/> as a
    /// keyed singleton.
    /// </summary>
    /// <param name="name">The name of the container to register.</param>
    /// <returns>A <see cref="CosmosDatabaseBuilder"/> that can be used for further chaining.</returns>
    public CosmosDatabaseBuilder AddKeyedContainer(string name)
    {
        _client ??= AspireMicrosoftAzureCosmosExtensions.GetCosmosClient(connectionName, settings, clientOptions);

        var connectionInfo = hostBuilder.GetCosmosConnectionInfo(name);

        hostBuilder.Services.AddKeyedSingleton(name, (sp, _) =>
        {
            // If a connection string was provided, check that it contains a valid container name.
            if (connectionInfo is not null && string.IsNullOrEmpty(connectionInfo?.ContainerName))
            {
                throw new InvalidOperationException(
                    $"A Container could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{name}'");
            }

            // Use the container name from the connection string if provided, otherwise use the name
            return _client.GetContainer(settings.DatabaseName, connectionInfo?.ContainerName ?? name);
        });

        return this;
    }
}
