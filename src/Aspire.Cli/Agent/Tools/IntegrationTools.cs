// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;

namespace Aspire.Cli.Agent.Tools;

/// <summary>
/// Tool for listing available Aspire integrations.
/// </summary>
internal sealed class ListIntegrationsTool : IListIntegrationsTool
{
    private readonly INuGetPackageCache _packageCache;

    // Well-known integration package pairs
    private static readonly IReadOnlyList<IntegrationInfo> s_knownIntegrations =
    [
        new("Redis", "Aspire.Hosting.Redis", "Aspire.StackExchange.Redis", "AddRedis()", "AddRedisClient()"),
        new("PostgreSQL", "Aspire.Hosting.PostgreSQL", "Aspire.Npgsql", "AddPostgres().AddDatabase()", "AddNpgsqlDataSource()"),
        new("PostgreSQL + EF Core", "Aspire.Hosting.PostgreSQL", "Aspire.Npgsql.EntityFrameworkCore.PostgreSQL", "AddPostgres().AddDatabase()", "AddNpgsqlDbContext<T>()"),
        new("SQL Server", "Aspire.Hosting.SqlServer", "Aspire.Microsoft.Data.SqlClient", "AddSqlServer().AddDatabase()", "AddSqlServerClient()"),
        new("SQL Server + EF Core", "Aspire.Hosting.SqlServer", "Aspire.Microsoft.EntityFrameworkCore.SqlServer", "AddSqlServer().AddDatabase()", "AddSqlServerDbContext<T>()"),
        new("MongoDB", "Aspire.Hosting.MongoDB", "Aspire.MongoDB.Driver", "AddMongoDB().AddDatabase()", "AddMongoDBClient()"),
        new("RabbitMQ", "Aspire.Hosting.RabbitMQ", "Aspire.RabbitMQ.Client", "AddRabbitMQ()", "AddRabbitMQClient()"),
        new("Kafka", "Aspire.Hosting.Kafka", "Aspire.Confluent.Kafka", "AddKafka()", "AddKafkaProducer()/AddKafkaConsumer()"),
        new("Azure Storage", "Aspire.Hosting.Azure.Storage", "Aspire.Azure.Storage.Blobs", "AddAzureStorage().AddBlobs()", "AddAzureBlobClient()"),
        new("Azure Cosmos DB", "Aspire.Hosting.Azure.CosmosDB", "Aspire.Microsoft.Azure.Cosmos", "AddAzureCosmosDB()", "AddAzureCosmosClient()"),
        new("Azure Service Bus", "Aspire.Hosting.Azure.ServiceBus", "Aspire.Azure.Messaging.ServiceBus", "AddAzureServiceBus()", "AddAzureServiceBusClient()"),
        new("Azure Key Vault", "Aspire.Hosting.Azure.KeyVault", "Aspire.Azure.Security.KeyVault", "AddAzureKeyVault()", "AddAzureKeyVaultClient()"),
        new("MySQL", "Aspire.Hosting.MySql", "Aspire.MySqlConnector", "AddMySql().AddDatabase()", "AddMySqlDataSource()"),
        new("Oracle", "Aspire.Hosting.Oracle", "Aspire.Oracle.EntityFrameworkCore", "AddOracle().AddDatabase()", "AddOracleDbContext<T>()"),
        new("Elasticsearch", "Aspire.Hosting.Elasticsearch", "Aspire.Elastic.Clients.Elasticsearch", "AddElasticsearch()", "AddElasticsearchClient()"),
        new("Milvus", "Aspire.Hosting.Milvus", "Aspire.Milvus.Client", "AddMilvus()", "AddMilvusClient()"),
        new("Qdrant", "Aspire.Hosting.Qdrant", "Aspire.Qdrant.Client", "AddQdrant()", "AddQdrantClient()"),
        new("Ollama", "Aspire.Hosting.Ollama", null, "AddOllama().AddModel()", null),
        new("Seq", "Aspire.Hosting.Seq", "Aspire.Seq", "AddSeq()", "builder.Logging.AddSeq()"),
        new("Garnet", "Aspire.Hosting.Garnet", "Aspire.StackExchange.Redis", "AddGarnet()", "AddRedisClient()"),
        new("Valkey", "Aspire.Hosting.Valkey", "Aspire.StackExchange.Redis", "AddValkey()", "AddRedisClient()"),
        new("NATS", "Aspire.Hosting.Nats", "Aspire.NATS.Net", "AddNats()", "AddNatsClient()"),
    ];

    public ListIntegrationsTool(INuGetPackageCache packageCache)
    {
        _packageCache = packageCache;
    }

    public Task<string> ExecuteAsync(string? filter)
    {
        var integrations = s_knownIntegrations.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            integrations = integrations.Where(i =>
                i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                i.HostingPackage.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (i.ClientPackage?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var result = integrations.Select(i =>
        {
            var clientInfo = i.ClientPackage is not null
                ? $"\n  Client: {i.ClientPackage} → {i.ClientMethod}"
                : "\n  Client: N/A (hosting only)";

            return $"""
                **{i.Name}**
                  Hosting: {i.HostingPackage} → {i.HostingMethod}{clientInfo}
                """;
        });

        var output = string.Join("\n\n", result);
        return Task.FromResult($"Available Aspire Integrations:\n\n{output}");
    }

    private sealed record IntegrationInfo(
        string Name,
        string HostingPackage,
        string? ClientPackage,
        string HostingMethod,
        string? ClientMethod);
}

/// <summary>
/// Tool for getting detailed integration documentation.
/// </summary>
internal sealed class GetIntegrationDocsTool : IGetIntegrationDocsTool
{
    private readonly INuGetPackageCache _packageCache;

    public GetIntegrationDocsTool(INuGetPackageCache packageCache)
    {
        _packageCache = packageCache;
    }

    public async Task<string> ExecuteAsync(string packageId)
    {
        // Try to get package info from NuGet
        // For now, return static documentation based on known packages
        var docs = GetStaticDocs(packageId);
        if (docs is not null)
        {
            return docs;
        }

        return $"No detailed documentation found for '{packageId}'. Use 'list_integrations' to see available integrations.";
    }

    private static string? GetStaticDocs(string packageId)
    {
        return packageId.ToLowerInvariant() switch
        {
            "aspire.hosting.redis" or "redis" => """
                # Redis Integration

                ## Hosting (AppHost)
                ```csharp
                var redis = builder.AddRedis("cache");
                
                // With persistence
                var redis = builder.AddRedis("cache")
                    .WithDataVolume()
                    .WithPersistence();

                // Reference from a project
                builder.AddProject<Projects.MyApi>("api")
                    .WithReference(redis);
                ```

                ## Client (Service Project)
                ```csharp
                // In Program.cs
                builder.AddRedisClient("cache");

                // Inject IConnectionMultiplexer or IDatabase
                public class MyService(IConnectionMultiplexer redis) { }
                ```

                ## Connection
                The connection string is automatically configured via WithReference().
                The service will receive a connection string named "cache".
                """,

            "aspire.hosting.postgresql" or "postgres" or "postgresql" => """
                # PostgreSQL Integration

                ## Hosting (AppHost)
                ```csharp
                var postgres = builder.AddPostgres("pg")
                    .AddDatabase("mydb");

                // With pgAdmin for management
                var postgres = builder.AddPostgres("pg")
                    .WithPgAdmin()
                    .AddDatabase("mydb");

                // Reference from a project
                builder.AddProject<Projects.MyApi>("api")
                    .WithReference(postgres);
                ```

                ## Client (Service Project) - Npgsql
                ```csharp
                builder.AddNpgsqlDataSource("mydb");

                public class MyService(NpgsqlDataSource dataSource) { }
                ```

                ## Client (Service Project) - EF Core
                ```csharp
                builder.AddNpgsqlDbContext<MyDbContext>("mydb");

                public class MyService(MyDbContext db) { }
                ```
                """,

            "aspire.hosting.rabbitmq" or "rabbitmq" => """
                # RabbitMQ Integration

                ## Hosting (AppHost)
                ```csharp
                var rabbitmq = builder.AddRabbitMQ("messaging");

                // With management plugin
                var rabbitmq = builder.AddRabbitMQ("messaging")
                    .WithManagementPlugin();

                // Reference from a project
                builder.AddProject<Projects.MyWorker>("worker")
                    .WithReference(rabbitmq);
                ```

                ## Client (Service Project)
                ```csharp
                builder.AddRabbitMQClient("messaging");

                public class MyService(IConnection connection) { }
                ```
                """,

            _ => null
        };
    }
}
