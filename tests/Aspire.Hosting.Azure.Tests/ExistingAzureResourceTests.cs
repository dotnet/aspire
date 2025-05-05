// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var (ManifestNode, BicepText) = await GetManifestWithBicep(model, serviceBus.Resource);

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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");

        // ensure the role assignments resource has the correct manifest and bicep, specifically the correct scope property

        var messagingRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "messaging-roles");
        (ManifestNode, BicepText) = await GetManifestWithBicep(messagingRoles, skipPreparer: true);

        expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "path": "messaging-roles.module.bicep",
              "params": {
                "messaging_outputs_name": "{messaging.outputs.name}",
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

            param messaging_outputs_name string

            param principalType string

            param principalId string

            resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: messaging_outputs_name
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
    }

    [Fact]
    public async Task SupportsExistingAzureOpenAIWithResourceGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var openAI = builder.AddAzureOpenAI("openAI")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);
        openAI.AddDeployment("mymodel", "gpt-35-turbo", "0613")
            .WithProperties(d =>
            {
                d.SkuName = "Basic";
                d.SkuCapacity = 4;
            });

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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
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

        await Verifier.Verify(BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
    }

    [Fact]
    public async Task SupportsExistingAzureContainerRegistryInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var acr = builder.AddAzureContainerRegistry("acr")
            .RunAsExisting(existingResourceName, resourceGroupParameter: default);

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(acr.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "acr.module.bicep",
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

            resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
              name: existingResourceName
            }

            output name string = existingResourceName

            output loginServer string = acr.properties.loginServer
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }

    [Fact]
    public async Task SupportsExistingAzureContainerRegistryInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var existingResourceName = builder.AddParameter("existingResourceName");
        var existingResourceGroupName = builder.AddParameter("existingResourceGroupName");
        var acr = builder.AddAzureContainerRegistry("acr")
            .PublishAsExisting(existingResourceName, existingResourceGroupName);

        var (ManifestNode, BicepText) = await AzureManifestUtils.GetManifestWithBicep(acr.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v1",
              "path": "acr.module.bicep",
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

            resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
              name: existingResourceName
            }

            output name string = existingResourceName

            output loginServer string = acr.properties.loginServer
            """;

        output.WriteLine(BicepText);
        Assert.Equal(expectedBicep, BicepText);
    }
}
