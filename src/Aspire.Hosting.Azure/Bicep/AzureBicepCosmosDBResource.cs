// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Cosmos;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Cosmos DB.
/// </summary>
public class AzureBicepCosmosDBResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.cosmosdb.bicep"),
    IResourceWithConnectionString
{
    internal List<string> Databases { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the Azure Cosmos DB resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cosmos DB resource.
    /// </summary>
    public string ConnectionStringExpression => $"{{{Name}.secretOutputs.connectionString}}";

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString()
    {
        if (IsEmulator)
        {
            return AzureCosmosDBEmulatorConnectionString.Create(GetEmulatorPort("emulator"));
        }

        return SecretOutputs["connectionString"];
    }

    private int GetEmulatorPort(string endpointName) =>
        Annotations
            .OfType<AllocatedEndpointAnnotation>()
            .FirstOrDefault(x => x.Name == endpointName)
            ?.Port
        ?? throw new DistributedApplicationException($"Azure Cosmos DB resource does not have endpoint annotation with name '{endpointName}'.");
}

/// <summary>
/// A resource that represents an Azure Cosmos DB database.
/// </summary>
public class AzureBicepCosmosDBDatabaseResource(string name, AzureBicepCosmosDBResource cosmosDB) :
    Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepCosmosDBResource>
{
    /// <summary>
    /// Gets the parent Azure Cosmos DB resource.
    /// </summary>
    public AzureBicepCosmosDBResource Parent => cosmosDB;

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cosmos DB database resource.
    /// </summary>
    public string ConnectionStringExpression => $"{{{Parent.Name}.connectionString}}";

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    public string? GetConnectionString()
    {
        return Parent.GetConnectionString();
    }

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", ConnectionStringExpression);
        context.Writer.WriteString("parent", Parent.Name);
    }
}

/// <summary>
/// Extension methods for adding Azure Cosmos DB resources to the application model.
/// </summary>
public static class AzureBicepCosmosExtensions
{
    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepCosmosDBResource> AddBicepCosmosDb(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepCosmosDBResource(name);
        return builder.AddResource(resource)
                      .WithParameter("databaseAccountName", resource.CreateBicepResourceName())
                      .WithParameter("databases", resource.Databases)
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures an Azure Cosmos DB resource to be emulated using the Azure Cosmos DB emulator with the NoSQL API. This resource requires an <see cref="AzureBicepCosmosDBResource"/> to be added to the application model.
    /// For more information on the Azure Cosmos DB emulator, see <a href="https://learn.microsoft.com/azure/cosmos-db/emulator#authentication"></a>
    /// </summary>
    /// <param name="builder">The Azure Cosmos DB resource builder.</param>
    /// <param name="port">The port used for the client SDK to access the emulator. Defaults to <c>8081</c></param>
    /// <param name="imageTag">The image tag for the <c>mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator</c> image.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// When using the Azure Cosmos DB emulator, the container requires a TLS/SSL certificate.
    /// For more information, see <a href="https://learn.microsoft.com/azure/cosmos-db/how-to-develop-emulator?tabs=docker-linux#export-the-emulators-tlsssl-certificate"></a>
    /// </remarks>
    public static IResourceBuilder<AzureBicepCosmosDBResource> UseEmulator(this IResourceBuilder<AzureBicepCosmosDBResource> builder, int? port = null, string? imageTag = null)
    {
        return builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "emulator", port: port, containerPort: 8081))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator", Tag = imageTag ?? "latest" });
    }

    /// <summary>
    /// Adds a resource which represents a database in the associated Cosmos DB account resource.
    /// </summary>
    /// <param name="builder">AzureCosmosDB resource builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="databaseName">Name of database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepCosmosDBDatabaseResource> AddDatabase(this IResourceBuilder<AzureBicepCosmosDBResource> builder, string name, string? databaseName = null)
    {
        var dbName = databaseName ?? name;

        var resource = new AzureBicepCosmosDBDatabaseResource(name, builder.Resource);

        builder.Resource.Databases.Add(dbName);

        return builder.ApplicationBuilder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

file static class AzureCosmosDBEmulatorConnectionString
{
    public static string Create(int port) => $"AccountKey={CosmosConstants.EmulatorAccountKey};AccountEndpoint=https://127.0.0.1:{port};";
}
