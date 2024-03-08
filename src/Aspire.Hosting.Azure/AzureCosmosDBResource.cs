// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Cosmos;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.KeyVaults;
using Azure.ResourceManager.CosmosDB.Models;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents an Azure Cosmos DB.
/// </summary>
public class AzureCosmosDBResource(string name) :
    AzureBicepResource(name, templateResourceName: "Aspire.Hosting.Azure.Bicep.cosmosdb.bicep"),
    IResourceWithConnectionString,
    IResourceWithEndpoints
{
    internal List<string> Databases { get; } = [];

    internal EndpointReference EmulatorEndpoint => new(this, "emulator");

    /// <summary>
    /// Gets the "connectionString" reference from the secret outputs of the Azure Cosmos DB resource.
    /// </summary>
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets a value indicating whether the Azure Cosmos DB resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cosmos DB resource.
    /// </summary>
    public string ConnectionStringExpression => ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string to use for this database.</returns>
    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (ProvisioningTaskCompletionSource is not null)
        {
            await ProvisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return GetConnectionString();
    }

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString()
    {
        if (IsEmulator)
        {
            return AzureCosmosDBEmulatorConnectionString.Create(EmulatorEndpoint.Port);
        }

        return ConnectionString.Value;
    }
}

/// <summary>
/// A resource that represents an Azure Cosmos DB.
/// </summary>
public class AzureCosmosDBConstructResource : AzureConstructResource, IResourceWithConnectionString, IResourceWithEndpoints
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="configureConstruct"></param>
    public AzureCosmosDBConstructResource(string name, Action<ResourceModuleConstruct> configureConstruct) : base(name, configureConstruct)
    {

    }

    internal AzureCosmosDBConstructResource(IResource innerResource, Action<ResourceModuleConstruct> configureConstruct) : base(innerResource.Name, configureConstruct)
    {
        _innerResource = innerResource;
    }

    private readonly IResource? _innerResource;

    /// <summary>
    /// TODO:
    /// </summary>
    public override string Name => base.Name;

    /// <summary>
    /// TODO:
    /// </summary>
    public override ResourceAnnotationCollection Annotations => _innerResource?.Annotations ?? base.Annotations;

    internal List<string> Databases { get; } = [];

    internal EndpointReference EmulatorEndpoint => new(this, "emulator");

    /// <summary>
    /// Gets the "connectionString" reference from the secret outputs of the Azure Cosmos DB resource.
    /// </summary>
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets a value indicating whether the Azure Cosmos DB resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cosmos DB resource.
    /// </summary>
    public string ConnectionStringExpression => ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string to use for this database.</returns>
    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (ProvisioningTaskCompletionSource is not null)
        {
            await ProvisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return GetConnectionString();
    }

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString()
    {
        if (IsEmulator)
        {
            return AzureCosmosDBEmulatorConnectionString.Create(EmulatorEndpoint.Port);
        }

        return ConnectionString.Value;
    }
}

/// <summary>
/// Extension methods for adding Azure Cosmos DB resources to the application model.
/// </summary>
public static class AzureCosmosExtensions
{
    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureCosmosDBResource(name);
        return builder.AddResource(resource)
                      .WithParameter("databaseAccountName", resource.CreateBicepResourceName())
                      .WithParameter("databases", resource.Databases)
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBConstructResource> AddAzureCosmosDBConstruct(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureCosmosDBConstructResource>, ResourceModuleConstruct, CosmosDBAccount, IEnumerable<CosmosDBSqlDatabase>>? configureResource = null)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var cosmosAccount = new CosmosDBAccount(construct, CosmosDBAccountKind.GlobalDocumentDB, name: name);
            cosmosAccount.AssignProperty(x => x.ConsistencyPolicy.DefaultConsistencyLevel, "'Session'");
            cosmosAccount.AssignProperty(x => x.DatabaseAccountOfferType, "'Standard'");
            cosmosAccount.AssignProperty(x => x.Locations[0].LocationName, "location");
            cosmosAccount.AssignProperty(x => x.Locations[0].FailoverPriority, "0");

            cosmosAccount.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            var keyVaultNameParameter = new Parameter("keyVaultName");
            construct.AddParameter(keyVaultNameParameter);

            var azureResource = (AzureCosmosDBConstructResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            List<CosmosDBSqlDatabase> cosmosSqlDatabases = new List<CosmosDBSqlDatabase>();
            foreach (var databaseName in azureResource.Databases)
            {
                var cosmosSqlDatabase = new CosmosDBSqlDatabase(construct, cosmosAccount, name: databaseName);
                cosmosSqlDatabases.Add(cosmosSqlDatabase);
            }

            var keyVault = KeyVault.FromExisting(construct, "keyVaultName");
            _ = new KeyVaultSecret(construct, "connectionString", cosmosAccount.GetConnectionString());

            if (configureResource != null)
            {
                configureResource(azureResourceBuilder, construct, cosmosAccount, cosmosSqlDatabases);
            }
        };

        var resource = new AzureCosmosDBConstructResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures an Azure Cosmos DB resource to be emulated using the Azure Cosmos DB emulator with the NoSQL API. This resource requires an <see cref="AzureCosmosDBResource"/> to be added to the application model.
    /// For more information on the Azure Cosmos DB emulator, see <a href="https://learn.microsoft.com/azure/cosmos-db/emulator#authentication"></a>
    /// </summary>
    /// <param name="builder">The Azure Cosmos DB resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// When using the Azure Cosmos DB emulator, the container requires a TLS/SSL certificate.
    /// For more information, see <a href="https://learn.microsoft.com/azure/cosmos-db/how-to-develop-emulator?tabs=docker-linux#export-the-emulators-tlsssl-certificate"></a>
    /// </remarks>
    public static IResourceBuilder<AzureCosmosDBResource> RunAsEmulator(this IResourceBuilder<AzureCosmosDBResource> builder, Action<IResourceBuilder<AzureCosmosDBEmulatorResource>>? configureContainer = null)
    {
        builder.WithEndpoint(name: "emulator", containerPort: 8081)
               .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator", Tag = "latest" });

        if (configureContainer != null)
        {
            var surrogate = new AzureCosmosDBEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Configures an Azure Cosmos DB resource to be emulated using the Azure Cosmos DB emulator with the NoSQL API. This resource requires an <see cref="AzureCosmosDBResource"/> to be added to the application model.
    /// For more information on the Azure Cosmos DB emulator, see <a href="https://learn.microsoft.com/azure/cosmos-db/emulator#authentication"></a>
    /// </summary>
    /// <param name="builder">The Azure Cosmos DB resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// When using the Azure Cosmos DB emulator, the container requires a TLS/SSL certificate.
    /// For more information, see <a href="https://learn.microsoft.com/azure/cosmos-db/how-to-develop-emulator?tabs=docker-linux#export-the-emulators-tlsssl-certificate"></a>
    /// </remarks>
    [Obsolete("Renamed to RunAsEmulator. Will be removed in next preview")]
    public static IResourceBuilder<AzureCosmosDBResource> UseEmulator(this IResourceBuilder<AzureCosmosDBResource> builder, Action<IResourceBuilder<AzureCosmosDBEmulatorResource>>? configureContainer = null)
    {
        return builder.RunAsEmulator(configureContainer);
    }

    /// <summary>
    /// Configures the gateway port for the Azure Cosmos DB emulator.
    /// </summary>
    /// <param name="builder">Builder for the Cosmos emulator container</param>
    /// <param name="port">Host port to bind to the emulator gateway port.</param>
    /// <returns>Cosmos emulator resource builder.</returns>
    public static IResourceBuilder<AzureCosmosDBEmulatorResource> UseGatewayPort(this IResourceBuilder<AzureCosmosDBEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a database to the associated Cosmos DB account resource.
    /// </summary>
    /// <param name="builder">AzureCosmosDB resource builder.</param>
    /// <param name="databaseName">Name of database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBResource> AddDatabase(this IResourceBuilder<AzureCosmosDBResource> builder, string databaseName)
    {
        builder.Resource.Databases.Add(databaseName);
        return builder;
    }

    /// <summary>
    /// Adds a database to the associated Cosmos DB account resource.
    /// </summary>
    /// <param name="builder">AzureCosmosDB resource builder.</param>
    /// <param name="databaseName">Name of database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBConstructResource> AddDatabase(this IResourceBuilder<AzureCosmosDBConstructResource> builder, string databaseName)
    {
        builder.Resource.Databases.Add(databaseName);
        return builder;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureResource"></param>
    /// <param name="useProvisioner"></param>
    /// <returns></returns>
    public static IResourceBuilder<MongoDBServerResource> PublishAsAzureCosmosDB(this IResourceBuilder<MongoDBServerResource> builder, Action<IResourceBuilder<MongoDBServerResource>, ResourceModuleConstruct, CosmosDBAccount, IEnumerable<CosmosDBSqlDatabase>>? configureResource = null, bool useProvisioner = true)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var cosmosAccount = new CosmosDBAccount(construct, CosmosDBAccountKind.MongoDB, name: construct.Resource.Name);
            cosmosAccount.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            List<CosmosDBSqlDatabase> mongoDatabases = new List<CosmosDBSqlDatabase>();
            foreach (var database in builder.Resource.Databases)
            {
                var databaseName = database.Value;
                var mongoDatabase = new CosmosDBSqlDatabase(construct, cosmosAccount, name: databaseName);
                mongoDatabases.Add(mongoDatabase);
            }

            var keyVaultNameParameter = new Parameter("keyVaultName");
            construct.AddParameter(keyVaultNameParameter);

            var keyVault = KeyVault.FromExisting(construct, "keyVaultName");
            _ = new KeyVaultSecret(construct, "connectionString", cosmosAccount.GetConnectionString());

            if (configureResource != null)
            {
                configureResource(builder, construct, cosmosAccount, mongoDatabases);
            }
        };

        var resource = new AzureCosmosDBConstructResource(builder.Resource, configureConstruct);
        var resourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource);
        resourceBuilder.WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                       .WithManifestPublishingCallback(resource.WriteToManifest);

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            resourceBuilder.WithParameter(AzureBicepResource.KnownParameters.PrincipalType);
        }

        if (useProvisioner)
        {
            // Used to hold a reference to the azure surrogate for use with the provisioner.
            builder.WithAnnotation(new AzureBicepResourceAnnotation(resource));
            builder.WithConnectionStringRedirection(resource);

            // Remove the container annotation so that DCP doesn't do anything with it.
            if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault() is { } containerAnnotation)
            {
                builder.Resource.Annotations.Remove(containerAnnotation);
            }
        }

        return builder;
    }
}

file static class AzureCosmosDBEmulatorConnectionString
{
    public static string Create(int port) => $"AccountKey={CosmosConstants.EmulatorAccountKey};AccountEndpoint=https://127.0.0.1:{port};DisableServerCertificateValidation=True;";
}
