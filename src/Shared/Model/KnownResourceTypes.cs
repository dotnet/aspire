// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Dashboard.Model;

internal static class KnownResourceTypes
{
    public const string Executable = "Executable";
    public const string Project = "Project";
    public const string Container = "Container";

    // This field needs to be updated when new resource types are added.
    private static readonly ImmutableArray<string> s_builtInResources = ["Resource", "AzureAppConfigurationResource", "AzureContainerAppEnvironmentResource", "AzureApplicationInsightsResource", "AzureOpenAIDeploymentResource", "AzureOpenAIResource", "AzureCosmosDBContainerResource", "AzureCosmosDBDatabaseResource", "AzureCosmosDBEmulatorResource", "AzureCosmosDBResource", "AzureEventHubConsumerGroupResource", "AzureEventHubResource", "AzureEventHubsEmulatorResource", "AzureEventHubsResource", "AzureFunctionsProjectResource", "AzureKeyVaultResource", "AzureLogAnalyticsWorkspaceResource", "AzurePostgresFlexibleServerDatabaseResource", "AzurePostgresFlexibleServerResource", "AzurePostgresResource", "AzureRedisCacheResource", "AzureRedisResource", "AzureSearchResource", "AzureServiceBusEmulatorResource", "AzureServiceBusQueueResource", "AzureServiceBusResource", "AzureServiceBusSubscriptionResource", "AzureServiceBusTopicResource", "AzureSignalREmulatorResource", "AzureSignalRResource", "AzureSqlDatabaseResource", "AzureSqlServerResource", "AzureBlobStorageResource", "AzureQueueStorageResource", "AzureStorageEmulatorResource", "AzureStorageResource", "AzureTableStorageResource", "AzureWebPubSubHubResource", "AzureWebPubSubResource", "AppIdentityResource", "AzureBicepResource", "AzureProvisioningResource", "DockerComposeEnvironmentResource", "DockerComposeServiceResource", "ElasticsearchResource", "GarnetResource", "KafkaServerResource", "KafkaUIContainerResource", "KeycloakResource", "KubernetesEnvironmentResource", "KubernetesResource", "AttuResource", "MilvusDatabaseResource", "MilvusServerResource", "MongoDBDatabaseResource", "MongoDBServerResource", "MongoExpressContainerResource", "MySqlDatabaseResource", "MySqlServerResource", "PhpMyAdminContainerResource", "NatsServerResource", "NodeAppResource", "OracleDatabaseResource", "OracleDatabaseServerResource", "PgAdminContainerResource", "PgWebContainerResource", "PostgresDatabaseResource", "PostgresServerResource", "PythonAppResource", "PythonProjectResource", "QdrantServerResource", "RabbitMQServerResource", "RedisCommanderResource", "RedisInsightResource", "RedisResource", "SeqResource", "SqlServerDatabaseResource", "SqlServerServerResource", "ValkeyResource", "ContainerResource", "ExecutableResource", "ParameterResource", "ProjectResource", "ConnectionStringParameterResource", "ConnectionStringResource", "ExecutableContainerResource", "ProjectContainerResource"];

    public static bool IsKnownResourceType(string resourceType)
    {
        return s_builtInResources.Contains(resourceType);
    }
}
