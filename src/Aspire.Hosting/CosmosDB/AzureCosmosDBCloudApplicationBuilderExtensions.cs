// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.CosmosDB;
using System.Text.Json;

namespace Aspire.Hosting;

public static class AzureCosmosDBCloudApplicationBuilderExtensions
{
     public static IDistributedApplicationResourceBuilder<CosmosDBConnectionResource> AddAzureCosmosDB(
         this IDistributedApplicationBuilder builder,
         string name,
         string? connectionString = null)
    {
        var connection = new CosmosDBConnectionResource(name, connectionString);
        return builder.AddResource(connection)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter => WriteCosmosDBConnectionToManifest(jsonWriter, connection)));
    }

    private static void WriteCosmosDBConnectionToManifest(Utf8JsonWriter jsonWriter, CosmosDBConnectionResource cosmosDbConnection)
    {
        jsonWriter.WriteString("type", "azure.cosmosdb.connection.v1");
        jsonWriter.WriteString("connectionString", cosmosDbConnection.GetConnectionString());
    }

    private static void WriteCosmosDBDatabaseToManifest(Utf8JsonWriter jsonWriter, CosmosDatabaseResource cosmosDatabase)
    {
        jsonWriter.WriteString("type", "azure.cosmosdb.database.v1");
        jsonWriter.WriteString("parent", cosmosDatabase.Parent.Name);
        jsonWriter.WriteString("databaseName", cosmosDatabase.Name);
    }

    public static IDistributedApplicationResourceBuilder<CosmosDatabaseResource> AddDatabase(this IDistributedApplicationResourceBuilder<CosmosDBConnectionResource> builder, string name)
    {
        var cosmosDatabase = new CosmosDatabaseResource(name, builder.Resource);
        return builder
            .ApplicationBuilder
            .AddResource(cosmosDatabase)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                (json) => WriteCosmosDBDatabaseToManifest(json, cosmosDatabase)));
    }

    public static IDistributedApplicationResourceBuilder<TDestination> WithAzureCosmosDB<TDestination>(
        this IDistributedApplicationResourceBuilder<TDestination> builder,
        IDistributedApplicationResourceBuilder<CosmosDatabaseResource> cosmosDatabaseResource)
        where TDestination : IDistributedApplicationResourceWithEnvironment
    {
        return builder
            .WithReference(cosmosDatabaseResource)
            .WithEnvironment("Aspire.Azure.EntityFrameworkCore.CosmosDB:DatabaseName", cosmosDatabaseResource.Resource.Name);
    }

}
