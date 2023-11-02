// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Data.Cosmos;
using System.Text.Json;

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
    public static IResourceBuilder<AzureCosmosDBConnectionResource> AddAzureCosmosDB(
       this IDistributedApplicationBuilder builder,
       string name,
       string? connectionString = null)
    {
        var connection = new AzureCosmosDBConnectionResource(name, connectionString);
        return builder.AddResource(connection)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter => WriteCosmosDBConnectionToManifest(jsonWriter, connection)));
    }

    private static void WriteCosmosDBConnectionToManifest(Utf8JsonWriter jsonWriter, AzureCosmosDBConnectionResource cosmosDbConnection)
    {
        jsonWriter.WriteString("type", "azure.data.cosmos.connection.v1");
        jsonWriter.WriteString("connectionString", cosmosDbConnection.GetConnectionString());
    }

    private static void WriteCosmosDBDatabaseToManifest(Utf8JsonWriter jsonWriter, AzureCosmosDatabaseResource cosmosDatabase)
    {
        jsonWriter.WriteString("type", "azure.data.cosmos.server.v1");
        jsonWriter.WriteString("parent", cosmosDatabase.Parent.Name);
        jsonWriter.WriteString("databaseName", cosmosDatabase.Name);
    }

    /// <summary>
    /// Adds an Azure Cosmos DB database to a <see cref="IResourceBuilder{AzureCosmosDatabaseResource}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureCosmosDatabaseResource}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDatabaseResource> AddDatabase(this IResourceBuilder<AzureCosmosDBConnectionResource> builder, string name)
    {
        var cosmosDatabase = new AzureCosmosDatabaseResource(name, builder.Resource);
        return builder
            .ApplicationBuilder
            .AddResource(cosmosDatabase)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                (json) => WriteCosmosDBDatabaseToManifest(json, cosmosDatabase)));
    }
}
