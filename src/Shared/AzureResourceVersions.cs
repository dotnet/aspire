// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class AzureResourceVersions
{
    public const string AppConfigurationStoreResourceVersion = "2023-03-01";

    public const string CognitiveServicesAccountResourceVersion = "2023-05-01";
    public const string CognitiveServicesAccountDeploymentResourceVersion = CognitiveServicesAccountResourceVersion;

    public const string CosmosDBAccountResourceVersion = "2023-04-15";
    public const string CosmosDBSqlDatabaseResourceVersion = CosmosDBAccountResourceVersion;

    public const string KeyVaultServiceResourceVersion = "2022-07-01";
    public const string KeyVaultSecretResourceVersion = KeyVaultServiceResourceVersion;

    public const string PostgreSqlFlexibleServerResourceVersion = "2023-03-01-preview";
    public const string PostgreSqlFlexibleServerDatabaseResourceVersion = PostgreSqlFlexibleServerResourceVersion;
    public const string PostgreSqlFlexibleServerFirewallRuleResourceVersion = PostgreSqlFlexibleServerResourceVersion;

    public const string RedisResourceVersion = "2020-06-01";

    public const string ServiceBusNamespaceResourceVersion = "2021-11-01";
    public const string ServiceBusQueueResourceVersion = ServiceBusNamespaceResourceVersion;
    public const string ServiceBusTopicResourceVersion = ServiceBusNamespaceResourceVersion;
    public const string ServiceBusSubscriptionResourceVersion = ServiceBusNamespaceResourceVersion;

    public const string SignalRResourceVersion = "2022-02-01";

    public const string SqlServerResourceVersion = "2020-11-01-preview";
    public const string SqlDatabaseResourceVersion = SqlServerResourceVersion;
    public const string SqlFirewallRuleResourceVersion = SqlServerResourceVersion;

    public const string StorageAccountResourceVersion = "2022-09-01";
    public const string BlobServiceResourceVersion = StorageAccountResourceVersion;

    public const string WebPubSubServiceResourceVersion = "2021-10-01";
}
