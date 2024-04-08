// Assembly 'Aspire.Microsoft.Azure.Cosmos'

using System;
using Aspire.Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Extensions.Hosting;

public static class AspireAzureCosmosDBExtensions
{
    public static void AddAzureCosmosDBClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureCosmosDBSettings>? configureSettings = null, Action<CosmosClientOptions>? configureClientOptions = null);
    public static void AddKeyedAzureCosmosDbClient(this IHostApplicationBuilder builder, string name, Action<AzureCosmosDBSettings>? configureSettings = null, Action<CosmosClientOptions>? configureClientOptions = null);
}
