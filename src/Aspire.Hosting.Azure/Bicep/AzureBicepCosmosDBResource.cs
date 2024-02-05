// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

public class AzureBicepCosmosDBResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.cosmosdb.bicep"),
    IResourceWithConnectionString
{
    internal List<string> Databases { get; } = [];

    public string ResourceNameOutputKey => "accountName";

    public string AccountKeyOutputKey => "accountKey";

    public string? GetConnectionString()
    {
        return $"AccountEndpoint={Outputs["documentEndpoint"]};AccountKey={Outputs[AccountKeyOutputKey]};";
    }
}

public class AzureBicepCosmosDBDatabaseResource(string name, AzureBicepCosmosDBResource cosmosDB) :
    Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepCosmosDBResource>
{
    public AzureBicepCosmosDBResource Parent => cosmosDB;

    public string? GetConnectionString()
    {
        return Parent.GetConnectionString();
    }

    public void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.connectionString}}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

public static class AzureBicepCosmosExtensions
{
    public static IResourceBuilder<AzureBicepCosmosDBResource> AddBicepCosmosDb(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepCosmosDBResource(name)
        {
            ConnectionStringTemplate = $"AccountEndpoint={{{name}.outputs.documentEndpoint}};AccountKey={{keys({{{name}.outputs.accountName}})}}"
        };

        return builder.AddResource(resource)
                      .AddParameter("databaseAccountName", resource.CreateBicepResourceName())
                      .AddParameter("databases", resource.Databases)
                      .WithMetadata("azureResourceType", "Microsoft.DocumentDB/databaseAccounts@2023-04-15")
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepCosmosDBDatabaseResource> AddDatabase(this IResourceBuilder<AzureBicepCosmosDBResource> builder, string name, string? databaseName = null)
    {
        var dbName = databaseName ?? name;

        var resource = new AzureBicepCosmosDBDatabaseResource(name, builder.Resource);

        builder.Resource.Databases.Add(dbName);

        return builder.ApplicationBuilder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
