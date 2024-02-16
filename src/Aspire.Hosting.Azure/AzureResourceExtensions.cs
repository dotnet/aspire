// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Data.Cosmos;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure resources to the application model.
/// </summary>
public static class AzureResourceExtensions
{
    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var keyVault = new AzureKeyVaultResource(name);
        return builder.AddResource(keyVault)
                      .WithManifestPublishingCallback(WriteAzureKeyVaultToManifest);
    }

    private static void WriteAzureKeyVaultToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.keyvault.v0");
    }

    /// <summary>
    /// Adds an Azure Service Bus resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="queueNames">A list of queue names associated with this service bus resource.</param>
    /// <param name="topicNames">A list of topic names associated with this service bus resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, string[]? queueNames = null, string[]? topicNames = null)
    {
        var resource = new AzureServiceBusResource(name)
        {
            QueueNames = queueNames ?? [],
            TopicNames = topicNames ?? []
        };

        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(context => WriteAzureServiceBusToManifest(resource, context));
    }

    private static void WriteAzureServiceBusToManifest(AzureServiceBusResource resource, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.servicebus.v0");

        if (resource.QueueNames.Length > 0)
        {
            context.Writer.WriteStartArray("queues");
            foreach (var queueName in resource.QueueNames)
            {
                context.Writer.WriteStringValue(queueName);
            }
            context.Writer.WriteEndArray();
        }

        if (resource.TopicNames.Length > 0)
        {
            context.Writer.WriteStartArray("topics");
            foreach (var topicName in resource.TopicNames)
            {
                context.Writer.WriteStringValue(topicName);
            }
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Adds an Azure Storage resource to the application model. This resource can be used to create Azure blob, table, and queue resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureStorageResource(name);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(WriteAzureStorageToManifest);
    }

    private static void WriteAzureStorageToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.storage.v0");
    }

    /// <summary>
    /// Adds an Azure blob storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="storageBuilder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBlobStorageResource> AddBlobs(this IResourceBuilder<AzureStorageResource> storageBuilder, string name)
    {
        var resource = new AzureBlobStorageResource(name, storageBuilder.Resource);
        return storageBuilder.ApplicationBuilder.AddResource(resource)
                             .WithManifestPublishingCallback(context => WriteBlobStorageToManifest(context, resource));
    }

    private static void WriteBlobStorageToManifest(ManifestPublishingContext context, AzureBlobStorageResource resource)
    {
        context.Writer.WriteString("type", "azure.storage.blob.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure table storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="storageBuilder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureTableStorageResource> AddTables(this IResourceBuilder<AzureStorageResource> storageBuilder, string name)
    {
        var resource = new AzureTableStorageResource(name, storageBuilder.Resource);
        return storageBuilder.ApplicationBuilder.AddResource(resource)
                             .WithManifestPublishingCallback(context => WriteTableStorageToManifest(context, resource));
    }

    private static void WriteTableStorageToManifest(ManifestPublishingContext context, AzureTableStorageResource resource)
    {
        context.Writer.WriteString("type", "azure.storage.table.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure queue storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureQueueStorageResource> AddQueues(this IResourceBuilder<AzureStorageResource> builder, string name)
    {
        var resource = new AzureQueueStorageResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource)
                             .WithManifestPublishingCallback(context => WriteQueueStorageToManifest(context, resource));
    }

    private static void WriteQueueStorageToManifest(ManifestPublishingContext context, AzureQueueStorageResource resource)
    {
        context.Writer.WriteString("type", "azure.storage.queue.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Configures an Azure Storage resource to be emulated using Azurite. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure storage resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureStorageResource> UseEmulator(this IResourceBuilder<AzureStorageResource> builder, Action<IResourceBuilder<AzureStorageEmulatorResource>>? configureContainer = null)
    {
        builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "blob", containerPort: 10000))
               .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "queue", containerPort: 10001))
               .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "table", containerPort: 10002))
               .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/azure-storage/azurite", Tag = "latest" });

        if (configureContainer != null)
        {
            var surrogate = new AzureStorageEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Enables persistence in the Azure Storage emulator.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureStorageEmulatorResource"/>.</param>
    /// <param name="path">Relative path to the AppHost where emulator storage is persisted between runs.</param>
    /// <returns>A builder for the <see cref="AzureStorageEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> UsePersistence(this IResourceBuilder<AzureStorageEmulatorResource> builder, string? path = null)
    {
        path = path ?? $".azurite/{builder.Resource.Name}";
        var fullyQualifiedPath = Path.GetFullPath(path, builder.ApplicationBuilder.AppHostDirectory);
        return builder.WithVolumeMount(fullyQualifiedPath, "/data", VolumeMountType.Bind, false);

    }

    /// <summary>
    /// Modifies the host port that the storage emulator listens on for blob requests.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> UseBlobPort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        return builder.WithEndpoint("blob", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Modifies the host port that the storage emulator listens on for queue requests.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> UseQueuePort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        return builder.WithEndpoint("queue", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Modifies the host port that the storage emulator listens on for table requests.
    /// </summary>
    /// <param name="builder">Storage emulator resource builder.</param>
    /// <param name="port">Host port to use.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureStorageEmulatorResource> UseTablePort(this IResourceBuilder<AzureStorageEmulatorResource> builder, int port)
    {
        return builder.WithEndpoint("table", endpoint =>
        {
            endpoint.Port = port;
        });
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
    public static IResourceBuilder<AzureCosmosDBResource> UseEmulator(this IResourceBuilder<AzureCosmosDBResource> builder, Action<IResourceBuilder<AzureCosmosDBEmulatorResource>>? configureContainer = null)
    {
        builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "emulator", containerPort: 8081))
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
    /// Adds an Azure Redis resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureRedisResource> AddAzureRedis(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureRedisResource(name);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(WriteAzureRedisToManifest);
    }

    private static void WriteAzureRedisToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.redis.v0");
    }

    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureAppConfigurationResource(name);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(WriteAzureAppConfigurationToManifest);
    }

    private static void WriteAzureAppConfigurationToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.appconfiguration.v0");
    }

    /// <summary>
    /// Adds an Azure SQL Server resource to the application model. This resource can be used to create Azure SQL Database resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSqlServerResource> AddAzureSqlServer(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureSqlServerResource(name);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(WriteSqlServerToManifest);
    }

    private static void WriteSqlServerToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.sql.v0");
    }

    /// <summary>
    /// Adds an Azure SQL Database resource to the application model. This resource requires an <see cref="AzureSqlServerResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serverBuilder">The Azure SQL Server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSqlDatabaseResource> AddDatabase(this IResourceBuilder<AzureSqlServerResource> serverBuilder, string name)
    {
        var resource = new AzureSqlDatabaseResource(name, serverBuilder.Resource);
        return serverBuilder.ApplicationBuilder.AddResource(resource)
                            .WithManifestPublishingCallback(context => WriteSqlDatabaseToManifest(context, resource));
    }

    private static void WriteSqlDatabaseToManifest(ManifestPublishingContext context, AzureSqlDatabaseResource resource)
    {
        context.Writer.WriteString("type", "azure.sql.database.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureOpenAIResource}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureOpenAIResource(name);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(WriteAzureOpenAIToManifest);
    }

    private static void WriteAzureOpenAIToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.openai.account.v0");
    }

    /// <summary>
    /// Adds an Azure OpenAI Deployment resource to the application model. This resource requires an <see cref="AzureOpenAIResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serverBuilder">The Azure SQL Server resource builder.</param>
    /// <param name="name">The name of the deployment.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlDatabaseResource}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIDeploymentResource> AddDeployment(this IResourceBuilder<AzureOpenAIResource> serverBuilder, string name)
    {
        var resource = new AzureOpenAIDeploymentResource(name, serverBuilder.Resource);
        return serverBuilder.ApplicationBuilder.AddResource(resource)
                            .WithManifestPublishingCallback(context => WriteAzureOpenAIDeploymentToManifest(context, resource));
    }

    private static void WriteAzureOpenAIDeploymentToManifest(ManifestPublishingContext context, AzureOpenAIDeploymentResource resource)
    {
        // Example:
        // "type": "azure.openai.deployment.v0",
        // "parent": "azureOpenAi",

        context.Writer.WriteString("type", "azure.openai.deployment.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="serverBuilder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlDatabaseResource}"/>.</returns>
    public static IResourceBuilder<AzureApplicationInsightsResource> AddApplicationInsights(
        this IDistributedApplicationBuilder serverBuilder,
        string name,
        string? connectionString = null)
    {
        var appInsights = new AzureApplicationInsightsResource(name, connectionString);
        return serverBuilder.AddResource(appInsights)
                        .WithManifestPublishingCallback(WriteAzureApplicationInsightsDeploymentToManifest);
    }

    private static void WriteAzureApplicationInsightsDeploymentToManifest(ManifestPublishingContext context)
    {
        // Example:
        // "type": "azure.appinsights.v0",

        context.Writer.WriteString("type", "azure.appinsights.v0");
    }

    /// <summary>
    /// Injects a connection string as an environment variable from the source resource into the destination resource.
    /// The environment variable will be "APPLICATIONINSIGHTS_CONNECTION_STRING={connectionString}."
    /// <para>
    /// Each resource defines the format of the connection string value. The
    /// underlying connection string value can be retrieved using <see cref="IResourceWithConnectionString.GetConnectionString"/>.
    /// </para>
    /// <para>
    /// Connection strings are also resolved by the configuration system (appSettings.json in the AppHost project, or environment variables). If a connection string is not found on the resource, the configuration system will be queried for a connection string
    /// using the resource's name.
    /// </para>
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where connection string will be injected.</param>
    /// <param name="source">The Azure Application Insights resource from which to extract the connection string.</param>
    /// <param name="optional"><see langword="true"/> to allow a missing connection string; <see langword="false"/> to throw an exception if the connection string is not found.</param>
    /// <exception cref="DistributedApplicationException">Throws an exception if the connection string resolves to null. It can be null if the resource has no connection string, and if the configuration has no connection string for the source resource.</exception>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder,
        IResourceBuilder<AzureApplicationInsightsResource> source, bool optional = false)
        where TDestination : IResourceWithEnvironment
    {
        var resource = source.Resource;

        return builder.WithEnvironment(context =>
        {
            // UseAzureMonitor is looking for this specific environment variable name.
            var connectionStringName = "APPLICATIONINSIGHTS_CONNECTION_STRING";

            if (context.ExecutionContext.Operation == DistributedApplicationOperation.Publish)
            {
                context.EnvironmentVariables[connectionStringName] = $"{{{resource.Name}.connectionString}}";
                return;
            }

            var connectionString = resource.GetConnectionString() ??
                builder.ApplicationBuilder.Configuration.GetConnectionString(resource.Name);

            if (string.IsNullOrEmpty(connectionString))
            {
                if (optional)
                {
                    // This is an optional connection string, so we can just return.
                    return;
                }

                throw new DistributedApplicationException($"A connection string for '{resource.Name}' could not be retrieved.");
            }

            if (builder.Resource is ContainerResource)
            {
                connectionString = HostNameResolver.ReplaceLocalhostWithContainerHost(connectionString, builder.ApplicationBuilder.Configuration);
            }

            context.EnvironmentVariables[connectionStringName] = connectionString;
        });
    }

    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(
       this IDistributedApplicationBuilder builder,
       string name,
       string? connectionString = null)
    {
        var connection = new AzureCosmosDBResource(name, connectionString);
        return builder.AddResource(connection)
                      .WithManifestPublishingCallback(context => WriteCosmosDBToManifest(context, connection));
    }

    /// <summary>
    /// Adds a resource which represents a database in the associated Cosmos DB account resource.
    /// </summary>
    /// <param name="builder">AzureCosmosDB resource builder.</param>
    /// <param name="name">Name of database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBDatabaseResource> AddDatabase(this IResourceBuilder<AzureCosmosDBResource> builder, string name)
    {
        var database = new AzureCosmosDBDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(database)
            .WithManifestPublishingCallback(context => WriteCosmosDBDatabaseToManifest(context, database));
    }

    private static void WriteCosmosDBDatabaseToManifest(ManifestPublishingContext context, AzureCosmosDBDatabaseResource database)
    {
        context.Writer.WriteString("type", "azure.cosmosdb.database.v0");
        context.Writer.WriteString("parent", database.Parent.Name);
    }

    private static void WriteCosmosDBToManifest(ManifestPublishingContext context, AzureCosmosDBResource cosmosDb)
    {
        // If we are using an emulator then we assume that a connection string was not
        // provided for the purpose of manifest generation.
        if (cosmosDb.IsEmulator || cosmosDb.GetConnectionString() is not { } connectionString)
        {
            context.Writer.WriteString("type", "azure.cosmosdb.account.v0");
        }
        else
        {
            context.Writer.WriteString("type", "azure.cosmosdb.connection.v0");
            context.Writer.WriteString("connectionString", connectionString);
        }

    }
}
