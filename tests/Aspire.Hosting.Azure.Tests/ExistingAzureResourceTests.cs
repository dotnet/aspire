// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class ExistingAzureResourceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddExistingAzureServiceBusInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsExisting(existingResourceName, resourceGroupParameter: default);
        serviceBus.AddServiceBusQueue("queue");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task RequiresPublishAsExistingInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsExisting(existingResourceName, resourceGroupParameter: default);
        serviceBus.AddServiceBusQueue("queue");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

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

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint

            output name string = messaging.name
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task AddExistingAzureServiceBusInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting(existingResourceName, resourceGroupParameter: default);
        serviceBus.AddServiceBusQueue("queue");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingServiceBusWithResourceGroupInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);
        serviceBus.AddServiceBusQueue("queue");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
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

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingServiceBusWithStaticArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .PublishAsExisting("existingResourceName", "existingResourceGroupName");
        serviceBus.AddServiceBusQueue("queue");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{messaging.outputs.serviceBusEndpoint}",
              "path": "messaging.module.bicep",
              "scope": {
                "resourceGroup": "existingResourceGroupName"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: 'existingResourceName'
            }

            resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue'
              parent: messaging
            }

            output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint

            output name string = messaging.name
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(storageAccount.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "path": "storage.module.bicep",
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

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
              name: existingResourceName
            }

            resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
              name: 'default'
              parent: storage
            }

            output blobEndpoint string = storage.properties.primaryEndpoints.blob

            output queueEndpoint string = storage.properties.primaryEndpoints.queue

            output tableEndpoint string = storage.properties.primaryEndpoints.table

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingStorageAccountWithResourceGroupAndStaticArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storageAccount = builder.AddAzureStorage("storage")
            .PublishAsExisting("existingResourcename", "existingResourceGroupName");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(storageAccount.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "path": "storage.module.bicep",
              "scope": {
                "resourceGroup": "existingResourceGroupName"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
              name: 'existingResourcename'
            }

            resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
              name: 'default'
              parent: storage
            }

            output blobEndpoint string = storage.properties.primaryEndpoints.blob

            output queueEndpoint string = storage.properties.primaryEndpoints.queue

            output tableEndpoint string = storage.properties.primaryEndpoints.table

            output name string = storage.name
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(appConfiguration.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{appConfig.outputs.appConfigEndpoint}",
              "path": "appConfig.module.bicep",
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

            resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
              name: existingResourceName
            }

            output appConfigEndpoint string = appConfig.properties.endpoint

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(eventHubs.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{eventHubs.outputs.eventHubsEndpoint}",
              "path": "eventHubs.module.bicep",
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

            resource eventHubs 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
              name: existingResourceName
            }

            output eventHubsEndpoint string = eventHubs.properties.serviceBusEndpoint

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(keyVault.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{keyVault.outputs.vaultUri}",
              "path": "keyVault.module.bicep",
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

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: existingResourceName
            }

            output vaultUri string = keyVault.properties.vaultUri

            output name string = existingResourceName

            output id string = keyVault.id
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(logAnalytics.Resource);

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

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(postgresSql.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{postgresSql.outputs.connectionString}",
              "path": "postgresSql.module.bicep",
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

            resource postgresSql 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
              name: existingResourceName
            }

            resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: postgresSql
            }

            output connectionString string = 'Host=${postgresSql.properties.fullyQualifiedDomainName}'

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingPostgresSqlWithResourceGroupWithPasswordAuth()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var existingUserName = builder.AddParameter("existingUserName");
        var existingPassword = builder.AddParameter("existingPassword");

        var postgresSql = builder.AddAzurePostgresFlexibleServer("postgresSql")
            .PublishAsExisting(existingResourceName, existingResourceGroupName)
            .WithPasswordAuthentication(existingUserName, existingPassword);

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(postgresSql.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{postgresSql-kv.secrets.connectionstrings--postgresSql}",
              "path": "postgresSql.module.bicep",
              "params": {
                "administratorLogin": "{existingUserName.value}",
                "administratorLoginPassword": "{existingPassword.value}",
                "keyVaultName": "{postgresSql-kv.outputs.name}",
                "existingResourceName": "{existingResourceName.value}"
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;
        var m = ManifestNode.ToString();
        output.WriteLine(m);

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param administratorLogin string

            @secure()
            param administratorLoginPassword string

            param keyVaultName string

            resource postgresSql 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
              name: existingResourceName
            }

            resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: postgresSql
            }

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--postgresSql'
              properties: {
                value: 'Host=${postgresSql.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
              }
              parent: keyVault
            }

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(search.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{search.outputs.connectionString}",
              "path": "search.module.bicep",
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

            resource search 'Microsoft.Search/searchServices@2023-11-01' existing = {
              name: existingResourceName
            }

            output connectionString string = 'Endpoint=https://${existingResourceName}.search.windows.net'

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(signalR.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "Endpoint=https://{signalR.outputs.hostName};AuthType=azure",
              "path": "signalR.module.bicep",
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

            resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
              name: existingResourceName
            }

            output hostName string = signalR.properties.hostName

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(webPubSub.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{webPubSub.outputs.endpoint}",
              "path": "webPubSub.module.bicep",
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

            resource webPubSub 'Microsoft.SignalRService/webPubSub@2024-03-01' existing = {
              name: existingResourceName
            }

            output endpoint string = 'https://${webPubSub.properties.hostName}'

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(sqlServer.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "Server=tcp:{sqlServer.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sqlServer.module.bicep",
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

            resource sqlServer 'Microsoft.Sql/servers@2021-11-01' existing = {
              name: existingResourceName
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

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureSqlServerInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var sqlServer = builder.AddAzureSqlServer("sqlServer")
            .RunAsExisting(existingResourceName, resourceGroupParameter: default);

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(sqlServer.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Server=tcp:{sqlServer.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sqlServer.module.bicep",
              "params": {
                "existingResourceName": "{existingResourceName.value}"
              }
            }
            """;

        Assert.Equal(expectedManifest, ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            resource sqlServer 'Microsoft.Sql/servers@2021-11-01' existing = {
              name: existingResourceName
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

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
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

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{redis.outputs.connectionString}",
              "path": "redis.module.bicep",
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

            resource redis 'Microsoft.Cache/redis@2024-03-01' existing = {
              name: existingResourceName
            }

            output connectionString string = '${redis.properties.hostName},ssl=true'

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureRedisWithResouceGroupAndAccessKeyAuth()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var redis = builder.AddAzureRedis("redis")
            .PublishAsExisting("existingResourceName", "existingResourceGroupName")
            .WithAccessKeyAuthentication();

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{redis-kv.secrets.connectionstrings--redis}",
              "path": "redis.module.bicep",
              "params": {
                "keyVaultName": "{redis-kv.outputs.name}"
              },
              "scope": {
                "resourceGroup": "existingResourceGroupName"
              }
            }
            """;
        var m = ManifestNode.ToString();
        output.WriteLine(m);
        Assert.Equal(expectedManifest, m);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyVaultName string

            resource redis 'Microsoft.Cache/redis@2024-03-01' existing = {
              name: 'existingResourceName'
            }

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--redis'
              properties: {
                value: '${redis.properties.hostName},ssl=true,password=${redis.listKeys().primaryKey}'
              }
              parent: keyVault
            }

            output name string = redis.name
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureApplicationInsightsWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var appInsights = builder.AddAzureApplicationInsights("appInsights")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(appInsights.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{appInsights.outputs.appInsightsConnectionString}",
              "path": "appInsights.module.bicep",
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

            resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
              name: existingResourceName
            }

            output appInsightsConnectionString string = appInsights.properties.ConnectionString
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureOpenAIWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var openAI = builder.AddAzureOpenAI("openAI")
            .PublishAsExisting(existingResourceName, existingResourceGroupName)
            .AddDeployment(new AzureOpenAIDeployment("mymodel", "gpt-35-turbo", "0613", "Basic", 4));

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(openAI.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{openAI.outputs.connectionString}",
              "path": "openAI.module.bicep",
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

            resource openAI 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
              name: existingResourceName
            }

            resource mymodel 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
              name: 'mymodel'
              properties: {
                model: {
                  format: 'OpenAI'
                  name: 'gpt-35-turbo'
                  version: '0613'
                }
              }
              sku: {
                name: 'Basic'
                capacity: 4
              }
              parent: openAI
            }

            output connectionString string = 'Endpoint=${openAI.properties.endpoint}'

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureCosmosDBWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        cosmos.AddCosmosDatabase("mydb")
            .AddContainer("container", "/id");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(cosmos.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{cosmos.outputs.connectionString}",
              "path": "cosmos.module.bicep",
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

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
              name: existingResourceName
            }

            resource mydb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
              name: 'mydb'
              location: location
              properties: {
                resource: {
                  id: 'mydb'
                }
              }
              parent: cosmos
            }

            resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
              name: 'container'
              location: location
              properties: {
                resource: {
                  id: 'container'
                  partitionKey: {
                    paths: [
                      '/id'
                    ]
                  }
                }
              }
              parent: mydb
            }

            output connectionString string = cosmos.properties.documentEndpoint

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureCosmosDBWithResourceGroupAccessKey()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .PublishAsExisting(existingResourceName, existingResourceGroupName)
            .WithAccessKeyAuthentication();

        cosmos.AddCosmosDatabase("mydb")
            .AddContainer("container", "/id");

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(cosmos.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "connectionString": "{cosmos-kv.secrets.connectionstrings--cosmos}",
              "path": "cosmos.module.bicep",
              "params": {
                "keyVaultName": "{cosmos-kv.outputs.name}",
                "existingResourceName": "{existingResourceName.value}"
              },
              "scope": {
                "resourceGroup": "{existingResourceGroupName.value}"
              }
            }
            """;
        var m = ManifestNode.ToString();
        output.WriteLine(m);
        Assert.Equal(expectedManifest, m);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param existingResourceName string

            param keyVaultName string

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
              name: existingResourceName
            }

            resource mydb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
              name: 'mydb'
              location: location
              properties: {
                resource: {
                  id: 'mydb'
                }
              }
              parent: cosmos
            }

            resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
              name: 'container'
              location: location
              properties: {
                resource: {
                  id: 'container'
                  partitionKey: {
                    paths: [
                      '/id'
                    ]
                  }
                }
              }
              parent: mydb
            }

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--cosmos'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey}'
              }
              parent: keyVault
            }

            resource mydb_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--mydb'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydb'
              }
              parent: keyVault
            }

            resource container_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--container'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydb;Container=container'
              }
              parent: keyVault
            }

            output name string = existingResourceName
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }
}
