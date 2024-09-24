// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Cosmos;
using Aspire.Hosting.Utils;
using Azure.Identity;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Aspire.Hosting;

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
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureCosmosDB(name, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<IResourceBuilder<AzureCosmosDBResource>, ResourceModuleConstruct, CosmosDBAccount, IEnumerable<CosmosDBSqlDatabase>>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var kvNameParam = new BicepParameter("keyVaultName", typeof(string));
            construct.Add(kvNameParam);

            var keyVault = KeyVaultService.FromExisting("keyVault");
            keyVault.Name = kvNameParam;
            construct.Add(keyVault);

            var cosmosAccount = new CosmosDBAccount(name)
            {
                Kind = CosmosDBAccountKind.GlobalDocumentDB,
                ConsistencyPolicy = new ConsistencyPolicy()
                {
                    DefaultConsistencyLevel = DefaultConsistencyLevel.Session
                },
                DatabaseAccountOfferType = CosmosDBAccountOfferType.Standard,
                Locations =
                {
                    new CosmosDBAccountLocation
                    {
                        LocationName = new IdentifierExpression("location"),
                        FailoverPriority = 0
                    }
                },
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(cosmosAccount);

            var azureResource = (AzureCosmosDBResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            List<CosmosDBSqlDatabase> cosmosSqlDatabases = new List<CosmosDBSqlDatabase>();
            foreach (var databaseName in azureResource.Databases)
            {
                var cosmosSqlDatabase = new CosmosDBSqlDatabase(databaseName, cosmosAccount.ResourceVersion)
                {
                    Parent = cosmosAccount,
                    Name = databaseName,
                    Resource = new CosmosDBSqlDatabaseResourceInfo()
                    {
                        DatabaseName = databaseName
                    }
                };
                construct.Add(cosmosSqlDatabase);
                cosmosSqlDatabases.Add(cosmosSqlDatabase);
            }

            var secret = new KeyVaultSecret("connectionString")
            {
                Parent = keyVault,
                Name = "connectionString",
                Properties = new SecretProperties
                {
                    Value = BicepFunction.Interpolate($"AccountEndpoint={cosmosAccount.DocumentEndpoint};AccountKey={cosmosAccount.GetKeys().PrimaryMasterKey}")
                }
            };
            construct.Add(secret);

            configureResource?.Invoke(azureResourceBuilder, construct, cosmosAccount, cosmosSqlDatabases);
        };

        var resource = new AzureCosmosDBResource(name, configureConstruct);

        CosmosClient? cosmosClient = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(resource, async (@event, ct) =>
        {
            var connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{resource.Name}' resource but the connection string was null.");
            }

            cosmosClient = CreateCosmosClient(connectionString);
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddAzureCosmosDB(sp =>
        {
            return cosmosClient ?? throw new InvalidOperationException("CosmosClient is not initialized.");
        }, name: healthCheckKey);

        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest)
                      .WithHealthCheck(healthCheckKey);

        static CosmosClient CreateCosmosClient(string connectionString)
        {
            var clientOptions = new CosmosClientOptions();
            clientOptions.CosmosClientTelemetryOptions.DisableDistributedTracing = true;

            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                return new CosmosClient(uri.OriginalString, new DefaultAzureCredential(), clientOptions);
            }
            else
            {
                if (CosmosUtils.IsEmulatorConnectionString(connectionString))
                {
                    clientOptions.ConnectionMode = ConnectionMode.Gateway;
                    clientOptions.LimitToEndpoint = true;
                }

                return new CosmosClient(connectionString, clientOptions);
            }
        }
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
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithEndpoint(name: "emulator", targetPort: 8081)
               .WithAnnotation(new ContainerImageAnnotation
               {
                   Registry = "mcr.microsoft.com",
                   Image = "cosmosdb/linux/azure-cosmos-emulator",
                   Tag = "latest"
               });

        if (configureContainer != null)
        {
            var surrogate = new AzureCosmosDBEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Adds a named volume for the data folder to an Azure Cosmos DB emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureCosmosDBEmulatorResource"/>.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>A builder for the <see cref="AzureCosmosDBEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBEmulatorResource> WithDataVolume(this IResourceBuilder<AzureCosmosDBEmulatorResource> builder, string? name = null)
        => builder.WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "true")
                  .WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/tmp/cosmos/appdata", isReadOnly: false);

    /// <summary>
    /// Configures the gateway port for the Azure Cosmos DB emulator.
    /// </summary>
    /// <param name="builder">Builder for the Cosmos emulator container</param>
    /// <param name="port">Host port to bind to the emulator gateway port.</param>
    /// <returns>Cosmos emulator resource builder.</returns>
    public static IResourceBuilder<AzureCosmosDBEmulatorResource> WithGatewayPort(this IResourceBuilder<AzureCosmosDBEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Configures the partition count for the Azure Cosmos DB emulator.
    /// </summary>
    /// <param name="builder">Builder for the Cosmos emulator container</param>
    /// <param name="count">Desired partition count.</param>
    /// <returns>Cosmos emulator resource builder.</returns>
    /// <remarks>Not calling this method will result in the default of 25 partitions. The actual started partitions is always one more than specified.
    /// See <a href="https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-windows-arguments#change-the-number-of-default-containers">this documentation</a> about setting the partition count.
    /// </remarks>
    public static IResourceBuilder<AzureCosmosDBEmulatorResource> WithPartitionCount(this IResourceBuilder<AzureCosmosDBEmulatorResource> builder, int count)
    {
        if (count < 1 || count > 250)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be between 1 and 250.");
        }

        return builder.WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", count.ToString(CultureInfo.InvariantCulture));
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
}
