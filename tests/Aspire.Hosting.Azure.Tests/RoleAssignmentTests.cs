// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.AppConfiguration;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.EventHubs;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Search;
using Azure.Provisioning.ServiceBus;
using Azure.Provisioning.SignalR;
using Azure.Provisioning.WebPubSub;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class RoleAssignmentTests(ITestOutputHelper output)
{
    [Fact]
    public Task ServiceBusSupport()
    {
        return RoleAssignmentTest("sb",
            builder =>
            {
                var sb = builder.AddAzureServiceBus("sb");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(sb, ServiceBusBuiltInRole.AzureServiceBusDataReceiver, ServiceBusBuiltInRole.AzureServiceBusDataSender);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sb_outputs_name string

            param principalId string

            resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: sb_outputs_name
            }

            resource sb_AzureServiceBusDataReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
                principalType: 'ServicePrincipal'
              }
              scope: sb
            }

            resource sb_AzureServiceBusDataSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
                principalType: 'ServicePrincipal'
              }
              scope: sb
            }
            """);
    }

    [Fact]
    public Task AppConfigurationSupport()
    {
        return RoleAssignmentTest("config",
            builder =>
            {
                var config = builder.AddAzureAppConfiguration("config");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(config, AppConfigurationBuiltInRole.AppConfigurationDataReader);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location
            
            param config_outputs_name string
            
            param principalId string
            
            resource config 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
              name: config_outputs_name
            }
            
            resource config_AppConfigurationDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(config.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071')
                principalType: 'ServicePrincipal'
              }
              scope: config
            }
            """);
    }

    [Fact]
    public Task OpenAISupport()
    {
        return RoleAssignmentTest("openai",
            builder =>
            {
                var openai = builder.AddAzureOpenAI("openai");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(openai, CognitiveServicesBuiltInRole.CognitiveServicesOpenAIUser);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param openai_outputs_name string

            param principalId string

            resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
              name: openai_outputs_name
            }

            resource openai_CognitiveServicesOpenAIUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(openai.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
                principalType: 'ServicePrincipal'
              }
              scope: openai
            }
            """);
    }

    [Fact]
    public Task EventHubsSupport()
    {
        return RoleAssignmentTest("eventhubs",
            builder =>
            {
                var eventhubs = builder.AddAzureEventHubs("eventhubs");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(eventhubs, EventHubsBuiltInRole.AzureEventHubsDataReceiver);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param eventhubs_outputs_name string

            param principalId string

            resource eventhubs 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
              name: eventhubs_outputs_name
            }

            resource eventhubs_AzureEventHubsDataReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(eventhubs.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde')
                principalType: 'ServicePrincipal'
              }
              scope: eventhubs
            }
            """);
    }

    [Fact]
    public Task KeyVaultSupport()
    {
        return RoleAssignmentTest("keyvault",
            builder =>
            {
                var keyvault = builder.AddAzureKeyVault("keyvault");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(keyvault, KeyVaultBuiltInRole.KeyVaultSecretsUser);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyvault_outputs_name string

            param principalId string

            resource keyvault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyvault_outputs_name
            }

            resource keyvault_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(keyvault.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
                principalType: 'ServicePrincipal'
              }
              scope: keyvault
            }
            """);
    }

    [Fact]
    public Task SearchSupport()
    {
        return RoleAssignmentTest("search",
            builder =>
            {
                var search = builder.AddAzureSearch("search");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(search, SearchBuiltInRole.SearchIndexDataReader);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param search_outputs_name string

            param principalId string

            resource search 'Microsoft.Search/searchServices@2023-11-01' existing = {
              name: search_outputs_name
            }

            resource search_SearchIndexDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(search.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')
                principalType: 'ServicePrincipal'
              }
              scope: search
            }
            """);
    }

    [Fact]
    public Task SignalRSupport()
    {
        return RoleAssignmentTest("signalr",
            builder =>
            {
                var signalr = builder.AddAzureSignalR("signalr");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(signalr, SignalRBuiltInRole.SignalRContributor);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param signalr_outputs_name string

            param principalId string

            resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
              name: signalr_outputs_name
            }

            resource signalr_SignalRContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761')
                principalType: 'ServicePrincipal'
              }
              scope: signalr
            }
            """);
    }

    [Fact]
    public Task WebPubSubSupport()
    {
        return RoleAssignmentTest("webpubsub",
            builder =>
            {
                var webpubsub = builder.AddAzureWebPubSub("webpubsub");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithRoleAssignments(webpubsub, WebPubSubBuiltInRole.WebPubSubServiceReader);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param webpubsub_outputs_name string

            param principalId string

            resource webpubsub 'Microsoft.SignalRService/webPubSub@2024-03-01' existing = {
              name: webpubsub_outputs_name
            }

            resource webpubsub_WebPubSubServiceReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(webpubsub.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'bfb1c7d2-fb1a-466b-b2ba-aee63b92deaf'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'bfb1c7d2-fb1a-466b-b2ba-aee63b92deaf')
                principalType: 'ServicePrincipal'
              }
              scope: webpubsub
            }
            """);
    }

    [Fact]
    public Task CosmosDBSupport()
    {
        return RoleAssignmentTest("cosmos",
            builder =>
            {
                var redis = builder.AddAzureCosmosDB("cosmos");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param cosmos_outputs_name string

            param principalId string

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
              name: cosmos_outputs_name
            }

            resource cosmos_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
              name: '00000000-0000-0000-0000-000000000002'
              parent: cosmos
            }

            resource cosmos_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
              name: guid(principalId, cosmos_roleDefinition.id, cosmos.id)
              properties: {
                principalId: principalId
                roleDefinitionId: cosmos_roleDefinition.id
                scope: cosmos.id
              }
              parent: cosmos
            }
            """);
    }

    [Fact]
    public Task RedisSupport()
    {
        return RoleAssignmentTest("redis",
            builder =>
            {
                var redis = builder.AddAzureRedis("redis");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param redis_outputs_name string

            param principalId string

            param principalName string

            resource redis 'Microsoft.Cache/redis@2024-03-01' existing = {
              name: redis_outputs_name
            }

            resource redis_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-03-01' = {
              name: guid(redis.id, principalId, 'Data Contributor')
              properties: {
                accessPolicyName: 'Data Contributor'
                objectId: principalId
                objectIdAlias: principalName
              }
              parent: redis
            }
            """,
            includePrincipalName: true);
    }

    [Fact]
    public Task PostgresSupport()
    {
        return RoleAssignmentTest("postgres",
            builder =>
            {
                var redis = builder.AddAzurePostgresFlexibleServer("postgres");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param postgres_outputs_name string

            param principalId string

            param principalName string

            resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
              name: postgres_outputs_name
            }

            resource postgres_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
              name: principalId
              properties: {
                principalName: principalName
                principalType: 'ServicePrincipal'
              }
              parent: postgres
            }
            """,
            includePrincipalName: true);
    }

    [Fact]
    public Task SqlSupport()
    {
        return RoleAssignmentTest("sql",
            builder =>
            {
                var redis = builder.AddAzureSqlServer("sql");

                builder.AddProject<Project>("api", launchProfileName: null)
                    .WithReference(redis);
            },
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sql_outputs_name string

            param principalId string

            param principalName string

            resource sql 'Microsoft.Sql/servers@2021-11-01' existing = {
              name: sql_outputs_name
            }
            
            resource sql_admin 'Microsoft.Sql/servers/administrators@2021-11-01' = {
              name: 'ActiveDirectory'
              properties: {
                login: principalName
                sid: principalId
              }
              parent: sql
            }
            """,
            includePrincipalName: true);
    }

    private async Task RoleAssignmentTest(
        string azureResourceName,
        Action<IDistributedApplicationBuilder> configureBuilder,
        string expectedRolesBicep,
        bool includePrincipalName = false)
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        configureBuilder(builder);

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var projRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == $"api-roles-{azureResourceName}");

        var (rolesManifest, rolesBicep) = await GetManifestWithBicep(projRoles);

        var principalNameSegment = includePrincipalName ? """
            ,
                "principalName": "{api-identity.outputs.principalName}"
            """ : null;

        var expectedRolesManifest =
            $$"""
            {
              "type": "azure.bicep.v0",
              "path": "api-roles-{{azureResourceName}}.module.bicep",
              "params": {
                "{{azureResourceName}}_outputs_name": "{{{azureResourceName}}.outputs.name}",
                "principalId": "{api-identity.outputs.principalId}"{{principalNameSegment}}
              }
            }
            """;
        Assert.Equal(expectedRolesManifest, rolesManifest.ToString());

        output.WriteLine(rolesBicep);
        Assert.Equal(expectedRolesBicep, rolesBicep);
    }

    private static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource) =>
        AzureManifestUtils.GetManifestWithBicep(resource, skipPreparer: true);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
