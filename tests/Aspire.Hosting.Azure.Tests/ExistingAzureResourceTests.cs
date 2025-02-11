// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class ExistingAzureResourceTests
{
    [Fact]
    public async Task AddExistingAzureServiceBusInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsExisting(existingResourceName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task RequiresPublishAsExistingInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsExisting(existingResourceName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
              name: take('messaging-${uniqueString(resourceGroup().id)}', 50)
              location: location
              properties: {
                disableLocalAuth: true
              }
              sku: {
                name: sku
              }
              tags: {
                'aspire-resource-name': 'messaging'
              }
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task AddExistingAzureServiceBusInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting(existingResourceName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingServiceBusWithResourceGroupInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting(existingResourceName, existingResourceGroupName)
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;

        Assert.Equal(expectedBicep, BicepText);

    }

    [Fact]
    public async Task SupportsExistingServiceBusWithStaticArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting("existingResourceName", "existingResourceGroupName")
            .WithQueue("queue");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "existingResourceGroupName"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: 'existingResourceName'
            }

            resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: messaging
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
            """;

        Assert.Equal(expectedBicep, BicepText);

    }

    [Fact]
    public async Task SupportsExistingStorageAccountWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var storageAccount = builder.AddAzureStorage("storage")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(storageAccount.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "path": "storage.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
              name: existingResourceName
            }

            resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
              name: 'default'
              parent: storage
            }

            resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                principalType: principalType
              }
              scope: storage
            }

            resource storage_StorageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
                principalType: principalType
              }
              scope: storage
            }

            resource storage_StorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
                principalType: principalType
              }
              scope: storage
            }

            output blobEndpoint string = storage.properties.primaryEndpoints.blob

            output queueEndpoint string = storage.properties.primaryEndpoints.queue

            output tableEndpoint string = storage.properties.primaryEndpoints.table
            """;

        Assert.Equal(expectedBicep, BicepText);

    }

    [Fact]
    public async Task SupportsExistingStorageAccountWithResourceGroupAndStaticArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storageAccount = builder.AddAzureStorage("storage")
            .PublishAsExisting("existingResourcename", "existingResourceGroupName");

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(storageAccount.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "path": "storage.module.bicep",
              "params": {
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "existingResourceGroupName"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalType string

            param principalId string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
              name: 'existingResourcename'
            }

            resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
              name: 'default'
              parent: storage
            }

            resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                principalType: principalType
              }
              scope: storage
            }

            resource storage_StorageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
                principalType: principalType
              }
              scope: storage
            }

            resource storage_StorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
                principalType: principalType
              }
              scope: storage
            }

            output blobEndpoint string = storage.properties.primaryEndpoints.blob

            output queueEndpoint string = storage.properties.primaryEndpoints.queue

            output tableEndpoint string = storage.properties.primaryEndpoints.table
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAppConfigurationWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var appConfiguration = builder.AddAzureAppConfiguration("appConfig")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(appConfiguration.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{appConfig.outputs.appConfigEndpoint}",
              "path": "appConfig.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
              name: existingResourceName
            }

            resource appConfig_AppConfigurationDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(appConfig.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
                principalType: principalType
              }
              scope: appConfig
            }

            output appConfigEndpoint string = appConfig.properties.endpoint
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingEventHubsWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var eventHubs = builder.AddAzureEventHubs("eventHubs")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(eventHubs.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{eventHubs.outputs.eventHubsEndpoint}",
              "path": "eventHubs.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource eventHubs 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            resource eventHubs_AzureEventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(eventHubs.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
                principalType: principalType
              }
              scope: eventHubs
            }

            output eventHubsEndpoint string = eventHubs.properties.serviceBusEndpoint
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingKeyVaultWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var keyVault = builder.AddAzureKeyVault("keyVault")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(keyVault.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{keyVault.outputs.vaultUri}",
              "path": "keyVault.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: existingResourceName
            }

            resource keyVault_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(keyVault.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
                principalType: principalType
              }
              scope: keyVault
            }

            output vaultUri string = keyVault.properties.vaultUri
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingLogAnalyticsWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("logAnalytics")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(logAnalytics.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "path": "logAnalytics.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}"
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
              name: existingResourceName
            }

            output logAnalyticsWorkspaceId string = logAnalytics.id
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingPostgresSqlWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var postgresSql = builder.AddAzurePostgresFlexibleServer("postgresSql")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(postgresSql.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{postgresSql.outputs.connectionString}",
              "path": "postgresSql.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalId": "",
                "principalType": "",
                "principalName": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalId string

            param principalType string

            param principalName string

            resource postgresSql 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
              name: existingResourceName
              properties: {
                authConfig: {
                  activeDirectoryAuth: 'Enabled'
                  passwordAuth: 'Disabled'
                }
              }
            }

            resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: postgresSql
            }

            resource postgresSql_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
              name: principalId
              properties: {
                principalName: principalName
                principalType: principalType
              }
              parent: postgresSql
              dependsOn: [
                postgresSql
                postgreSqlFirewallRule_AllowAllAzureIps
              ]
            }

            output connectionString string = 'Host=${postgresSql.properties.fullyQualifiedDomainName};Username=${principalName}'
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureSearchWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var search = builder.AddAzureSearch("search")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(search.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{search.outputs.connectionString}",
              "path": "search.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource search 'Microsoft.Search/searchServices@2023-11-01' existing = {
              name: existingResourceName
            }

            resource search_SearchIndexDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(search.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
                principalType: principalType
              }
              scope: search
            }

            resource search_SearchServiceContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(search.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
                principalType: principalType
              }
              scope: search
            }

            output connectionString string = 'Endpoint=https://${existingResourceName}.search.windows.net'
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureSignalRWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var signalR = builder.AddAzureSignalR("signalR")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(signalR.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "Endpoint=https://{signalR.outputs.hostName};AuthType=azure",
              "path": "signalR.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
              name: existingResourceName
            }

            resource signalR_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalR.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalType: principalType
              }
              scope: signalR
            }

            output hostName string = signalR.properties.hostName
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureWebPubSubWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var webPubSub = builder.AddAzureWebPubSub("webPubSub")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(webPubSub.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{webPubSub.outputs.endpoint}",
              "path": "webPubSub.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalType": "",
                "principalId": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalType string

            param principalId string

            resource webPubSub 'Microsoft.SignalRService/webPubSub@2024-03-01' existing = {
              name: existingResourceName
            }

            resource webPubSub_WebPubSubServiceOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(webPubSub.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
                principalType: principalType
              }
              scope: webPubSub
            }

            output endpoint string = 'https://${webPubSub.properties.hostName}'
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureSqlServerWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var sqlServer = builder.AddAzureSqlServer("sqlServer")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(sqlServer.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "Server=tcp:{sqlServer.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sqlServer.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalId": "",
                "principalName": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalName string

            param existingResourceName string

            resource sqlServer 'Microsoft.Sql/servers@2021-11-01' existing = {
              name: existingResourceName
              properties: {
                administrators: {
                  administratorType: 'ActiveDirectory'
                  login: principalName
                  sid: principalId
                  tenantId: subscription().tenantId
                  azureADOnlyAuthentication: true
                }
              }
            }

            resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: sqlServer
            }

            output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureSqlServerInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var sqlServer = builder.AddAzureSqlServer("sqlServer")
            .RunAsExisting(existingResourceName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(sqlServer.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Server=tcp:{sqlServer.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sqlServer.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalId": "",
                "principalName": "",
                "principalType": ""
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalName string

            param existingResourceName string

            param principalType string

            resource sqlServer 'Microsoft.Sql/servers@2021-11-01' existing = {
              name: existingResourceName
              properties: {
                administrators: {
                  administratorType: 'ActiveDirectory'
                  principalType: principalType
                  login: principalName
                  sid: principalId
                  tenantId: subscription().tenantId
                  azureADOnlyAuthentication: true
                }
              }
            }

            resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: sqlServer
            }

            resource sqlFirewallRule_AllowAllIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllIps'
              properties: {
                endIpAddress: '255.255.255.255'
                startIpAddress: '0.0.0.0'
              }
              parent: sqlServer
            }

            output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
            """;
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureRedisWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var redis = builder.AddAzureRedis("redis")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{redis.outputs.connectionString}",
              "path": "redis.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}",
                "principalId": "",
                "principalName": ""
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param principalId string

            param principalName string

            resource redis 'Microsoft.Cache/redis@2024-03-01' existing = {
              name: existingResourceName
              properties: {
                disableAccessKeyAuthentication: true
                redisConfiguration: {
                  'aad-enabled': 'true'
                }
              }
            }

            resource redis_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-03-01' = {
              name: take('rediscontributor${uniqueString(resourceGroup().id)}', 24)
              properties: {
                accessPolicyName: 'Data Contributor'
                objectId: principalId
                objectIdAlias: principalName
              }
              parent: redis
            }

            output connectionString string = '${redis.properties.hostName},ssl=true'
            """;

        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureRedisWithResouceGroupAndAccessKeyAuth()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var redis = builder.AddAzureRedis("redis")
            .PublishAsExisting("existingResourceName", "existingResourceGroupName")
            .WithAccessKeyAuthentication();

        var (ManifestNode, BicepText) = await ManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{redis.secretOutputs.connectionString}",
              "path": "redis.module.bicep",
              "params": {
                "keyVaultName": ""
              },
              "scope": {
                "resourceGroup": "existingResourceGroupName"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyVaultName string

            resource redis 'Microsoft.Cache/redis@2024-03-01' existing = {
              name: 'existingResourceName'
              properties: {
                disableAccessKeyAuthentication: false
              }
            }

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionString'
              properties: {
                value: '${redis.properties.hostName},ssl=true,password=${redis.listKeys().primaryKey}'
              }
              parent: keyVault
            }
            """;

        Assert.Equal(expectedBicep, BicepText);
    }
}
