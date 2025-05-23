// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

internal static class KnownResourceTypes
{
    public const string Executable = "Executable";
    public const string Project = "Project";
    public const string Container = "Container";

    // This field needs to be updated when new resource types are added.
    private static readonly HashSet<string> s_builtInResources = 
    [
        "AppIdentityResource", 
        "AttuResource", 
        "AzureAppConfigurationResource", 
        "AzureApplicationInsightsResource", 
        "AzureBicepResource", 
        "AzureBlobStorageResource", 
        "AzureContainerAppEnvironmentResource", 
        "AzureCosmosDBContainerResource", 
        "AzureCosmosDBDatabaseResource", 
        "AzureCosmosDBEmulatorResource", 
        "AzureCosmosDBResource", 
        "AzureEventHubConsumerGroupResource", 
        "AzureEventHubResource", 
        "AzureEventHubsEmulatorResource", 
        "AzureEventHubsResource", 
        "AzureFunctionsProjectResource", 
        "AzureKeyVaultResource", 
        "AzureLogAnalyticsWorkspaceResource", 
        "AzureOpenAIDeploymentResource", 
        "AzureOpenAIResource", 
        "AzurePostgresFlexibleServerDatabaseResource", 
        "AzurePostgresFlexibleServerResource", 
        "AzurePostgresResource", 
        "AzureProvisioningResource", 
        "AzureQueueStorageResource", 
        "AzureRedisCacheResource", 
        "AzureRedisResource", 
        "AzureSearchResource", 
        "AzureServiceBusEmulatorResource", 
        "AzureServiceBusQueueResource", 
        "AzureServiceBusResource", 
        "AzureServiceBusSubscriptionResource", 
        "AzureServiceBusTopicResource", 
        "AzureSignalREmulatorResource", 
        "AzureSignalRResource", 
        "AzureSqlDatabaseResource", 
        "AzureSqlServerResource", 
        "AzureStorageEmulatorResource", 
        "AzureStorageResource", 
        "AzureTableStorageResource", 
        "AzureWebPubSubHubResource", 
        "AzureWebPubSubResource", 
        "ConnectionStringParameterResource", 
        "ConnectionStringResource", 
        Container, 
        "DockerComposeEnvironmentResource", 
        "DockerComposeServiceResource", 
        Executable, 
        "ExecutableContainerResource", 
        "GarnetResource", 
        "KafkaServerResource", 
        "KafkaUIContainerResource", 
        "KeycloakResource", 
        "KubernetesEnvironmentResource", 
        "KubernetesResource", 
        "MilvusDatabaseResource", 
        "MilvusServerResource", 
        "MongoDBDatabaseResource", 
        "MongoDBServerResource", 
        "MongoExpressContainerResource", 
        "MySqlDatabaseResource", 
        "MySqlServerResource", 
        "NatsServerResource", 
        "NodeAppResource", 
        "OracleDatabaseResource", 
        "OracleDatabaseServerResource", 
        "ParameterResource", 
        "PgAdminContainerResource", 
        "PgWebContainerResource", 
        "PhpMyAdminContainerResource", 
        "PostgresDatabaseResource", 
        "PostgresServerResource", 
        Project, 
        "ProjectContainerResource", 
        "PythonAppResource", 
        "PythonProjectResource", 
        "QdrantServerResource", 
        "RabbitMQServerResource", 
        "RedisCommanderResource", 
        "RedisInsightResource", 
        "RedisResource", 
        "Resource", 
        "SeqResource", 
        "SqlServerDatabaseResource", 
        "SqlServerServerResource", 
        "ValkeyResource"
    ];

    public static bool IsKnownResourceType(string resourceType)
    {
        return s_builtInResources.Contains(resourceType);
    }
}
