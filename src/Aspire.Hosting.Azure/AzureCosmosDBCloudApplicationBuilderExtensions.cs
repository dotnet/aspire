// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Data.Cosmos;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure Cosmos DB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class AzureCosmosDBCloudApplicationBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureCosmosDatabaseResource}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(
       this IDistributedApplicationBuilder builder,
       string name,
       string? connectionString = null)
    {
        var connection = new AzureCosmosDBResource(name, connectionString);
        return builder.AddResource(connection)
                      .WithManifestPublishingCallback(context => WriteCosmosDBToManifest(context, connection));
    }

    private static void WriteCosmosDBToManifest(ManifestPublishingContext context, AzureCosmosDBResource cosmosDb)
    {
        var connectionString = cosmosDb.GetConnectionString();
        if (connectionString is null)
        {
            context.Writer.WriteString("type", "azure.cosmosdb.account.v0");
        }
        else
        {
            context.Writer.WriteString("type", "azure.cosmosdb.connection.v0");
            context.Writer.WriteString("connectionString", cosmosDb.GetConnectionString());
        }

    }
}
