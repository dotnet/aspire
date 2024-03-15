// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Storage.Models;
using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureBicepResourceTests
{
    [Fact]
    public void AddBicepResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("mytemplate", "content")
                                   .WithParameter("param1", "value1")
                                   .WithParameter("param2", "value2");

        Assert.Equal("content", bicepResource.Resource.TemplateString);
        Assert.Equal("value1", bicepResource.Resource.Parameters["param1"]);
        Assert.Equal("value2", bicepResource.Resource.Parameters["param2"]);
    }

    [Fact]
    public void GetOutputReturnsOutputValue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.Outputs["resourceEndpoint"] = "https://myendpoint";

        Assert.Equal("https://myendpoint", bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetSecretOutputReturnsSecretOutputValue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.SecretOutputs["connectionString"] = "https://myendpoint;Key=43";

        Assert.Equal("https://myendpoint;Key=43", bicepResource.GetSecretOutput("connectionString").Value);
    }

    [Fact]
    public void GetOutputValueThrowsIfNoOutput()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        Assert.Throws<InvalidOperationException>(() => bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetSecretOutputValueThrowsIfNoOutput()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        Assert.Throws<InvalidOperationException>(() => bicepResource.GetSecretOutput("connectionString").Value);
    }

    [Fact]
    public async Task AssertManifestLayout()
    {
        var builder = DistributedApplication.CreateBuilder();

        var param = builder.AddParameter("p1");

        var b2 = builder.AddBicepTemplateString("temp2", "content");

        var bicepResource = builder.AddBicepTemplateString("templ", "content")
                                    .WithParameter("param1", "value1")
                                    .WithParameter("param2", ["1", "2"])
                                    .WithParameter("param3", new JsonObject() { ["value"] = "nested" })
                                    .WithParameter("param4", param)
                                    .WithParameter("param5", b2.GetOutput("value1"))
                                    .WithParameter("param6", () => b2.GetOutput("value2"));

        bicepResource.Resource.TempDirectory = Environment.CurrentDirectory;

        var manifest = await ManifestUtils.GetManifest(bicepResource.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "templ.bicep",
              "params": {
                "param1": "value1",
                "param2": [
                  "1",
                  "2"
                ],
                "param3": {
                  "value": "nested"
                },
                "param4": "{p1.value}",
                "param5": "{temp2.outputs.value1}",
                "param6": "{temp2.outputs.value2}"
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task AddAzureCosmosDB()
    {
        var builder = DistributedApplication.CreateBuilder();

        IEnumerable<CosmosDBSqlDatabase>? callbackDatabases = null;
        var cosmos = builder.AddAzureCosmosDB("cosmos", (resource, construct, account, databases) =>
        {
            callbackDatabases = databases;
        });
        cosmos.AddDatabase("mydatabase");

        cosmos.Resource.SecretOutputs["connectionString"] = "mycosmosconnectionstring";

        var manifest = await ManifestUtils.GetManifestWithBicep(cosmos.Resource);

        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "connectionString": "{cosmos.secretOutputs.connectionString}",
                                 "path": "cosmos.module.bicep",
                                 "params": {
                                   "keyVaultName": ""
                                 }
                               }
                               """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param keyVaultName string


            resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
              name: keyVaultName
            }

            resource cosmosDBAccount_5pKmb8KAZ 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
              name: toLower(take(concat('cosmos', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'cosmos'
              }
              kind: 'GlobalDocumentDB'
              properties: {
                databaseAccountOfferType: 'Standard'
                consistencyPolicy: {
                  defaultConsistencyLevel: 'Session'
                }
                locations: [
                  {
                    locationName: location
                    failoverPriority: 0
                  }
                ]
              }
            }

            resource cosmosDBSqlDatabase_TRuxXYh2M 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
              parent: cosmosDBAccount_5pKmb8KAZ
              name: 'mydatabase'
              location: location
              properties: {
                resource: {
                  id: 'mydatabase'
                }
              }
            }

            resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
              parent: keyVault_IeF8jZvXV
              name: 'connectionString'
              location: location
              properties: {
                value: 'AccountEndpoint=${cosmosDBAccount_5pKmb8KAZ.properties.documentEndpoint};AccountKey=${cosmosDBAccount_5pKmb8KAZ.listkeys(cosmosDBAccount_5pKmb8KAZ.apiVersion).primaryMasterKey}'
              }
            }
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);

        Assert.NotNull(callbackDatabases);
        Assert.Collection(
            callbackDatabases,
            (database) => Assert.Equal("mydatabase", database.Properties.Name)
            );

        var connectionStringResource = (IResourceWithConnectionString)cosmos.Resource;

        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("mycosmosconnectionstring", await connectionStringResource.GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureAppConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appConfig = builder.AddAzureAppConfiguration("appConfig");
        appConfig.Resource.Outputs["appConfigEndpoint"] = "https://myendpoint";
        Assert.Equal("https://myendpoint", await appConfig.Resource.ConnectionStringExpression.GetValueAsync(default));

        var manifest = await ManifestUtils.GetManifestWithBicep(appConfig.Resource);

        var connectionStringResource = (IResourceWithConnectionString)appConfig.Resource;

        Assert.Equal("https://myendpoint", await connectionStringResource.GetConnectionStringAsync());

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{appConfig.outputs.appConfigEndpoint}",
              "path": "appConfig.module.bicep",
              "params": {
                "principalId": "",
                "principalType": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param principalId string

            @description('')
            param principalType string


            resource appConfigurationStore_j2IqAZkBh 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
              name: toLower(take(concat('appConfig', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'appConfig'
              }
              sku: {
                name: 'standard'
              }
              properties: {
              }
            }

            resource roleAssignment_umUNaNdeG 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: appConfigurationStore_j2IqAZkBh
              name: guid(appConfigurationStore_j2IqAZkBh.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
                principalId: principalId
                principalType: principalType
              }
            }

            output appConfigEndpoint string = appConfigurationStore_j2IqAZkBh.properties.endpoint
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddApplicationInsights()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appInsights = builder.AddAzureApplicationInsights("appInsights");

        appInsights.Resource.Outputs["appInsightsConnectionString"] = "myinstrumentationkey";

        var connectionStringResource = (IResourceWithConnectionString)appInsights.Resource;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.appinsights.bicep", appInsights.Resource.TemplateResourceName);
        Assert.Equal("appInsights", appInsights.Resource.Name);
        Assert.Equal("appinsights", appInsights.Resource.Parameters["appInsightsName"]);
        Assert.True(appInsights.Resource.Parameters.ContainsKey(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId));
        Assert.Equal("myinstrumentationkey", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{appInsights.outputs.appInsightsConnectionString}", appInsights.Resource.ConnectionStringExpression.ValueExpression);

        var appInsightsManifest = await ManifestUtils.GetManifest(appInsights.Resource);
        Assert.Equal("{appInsights.outputs.appInsightsConnectionString}", appInsightsManifest["connectionString"]?.ToString());
        Assert.Equal("azure.bicep.v0", appInsightsManifest["type"]?.ToString());
        Assert.Equal("aspire.hosting.azure.bicep.appinsights.bicep", appInsightsManifest["path"]?.ToString());
    }

    [Fact]
    public async Task WithReferenceAppInsightsSetsEnvironmentVariable()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appInsights = builder.AddAzureApplicationInsights("ai");

        appInsights.Resource.Outputs["appInsightsConnectionString"] = "myinstrumentationkey";

        var serviceA = builder.AddProject<Projects.ServiceA>("serviceA")
            .WithReference(appInsights);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(serviceA.Resource);

        Assert.True(config.ContainsKey("APPLICATIONINSIGHTS_CONNECTION_STRING"));
        Assert.Equal("myinstrumentationkey", config["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
    }

    [Fact]
    public async Task AddAzureConstructGenertesCorrectManifestEntry()
    {
        var builder = DistributedApplication.CreateBuilder();
        var construct1 = builder.AddAzureConstruct("construct1", (construct) =>
        {
            var storage = construct.AddStorageAccount(
                kind: StorageKind.StorageV2,
                sku: StorageSkuName.StandardLrs
                );
            storage.AddOutput("storageAccountName", sa => sa.Name);
        });

        var manifest = await ManifestUtils.GetManifest(construct1.Resource);
        Assert.Equal("azure.bicep.v0", manifest["type"]?.ToString());
        Assert.Equal("construct1.module.bicep", manifest["path"]?.ToString());
    }

    [Fact]
    public async Task AssignParameterPopulatesParametersEverywhere()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["Parameters:skuName"] = "Standard_ZRS";

        var skuName = builder.AddParameter("skuName");

        ResourceModuleConstruct? moduleConstruct = null;
        var construct1 = builder.AddAzureConstruct("construct1", (construct) =>
        {
            var storage = construct.AddStorageAccount(
                kind: StorageKind.StorageV2,
                sku: StorageSkuName.StandardLrs
                );
            storage.AssignProperty(sa => sa.Sku.Name, skuName);
            moduleConstruct = construct;
        });

        var manifest = await ManifestUtils.GetManifest(construct1.Resource);

        Assert.NotNull(moduleConstruct);
        var constructParameters = moduleConstruct.GetParameters(false).DistinctBy(x => x.Name);
        var constructParametersLookup = constructParameters.ToDictionary(p => p.Name);
        Assert.True(constructParametersLookup.ContainsKey("skuName"));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "construct1.module.bicep",
              "params": {
                "skuName": "{skuName.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task AssignParameterWithSpecifiedNamePopulatesParametersEverywhere()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["Parameters:skuName"] = "Standard_ZRS";

        var skuName = builder.AddParameter("skuName");

        ResourceModuleConstruct? moduleConstruct = null;
        var construct1 = builder.AddAzureConstruct("construct1", (construct) =>
        {
            var storage = construct.AddStorageAccount(
                kind: StorageKind.StorageV2,
                sku: StorageSkuName.StandardLrs
                );
            storage.AssignProperty(sa => sa.Sku.Name, skuName, parameterName: "sku");
            moduleConstruct = construct;
        });

        var manifest = await ManifestUtils.GetManifest(construct1.Resource);

        Assert.NotNull(moduleConstruct);
        var constructParameters = moduleConstruct.GetParameters(false).DistinctBy(x => x.Name);
        var constructParametersLookup = constructParameters.ToDictionary(p => p.Name);
        Assert.True(constructParametersLookup.ContainsKey("sku"));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "construct1.module.bicep",
              "params": {
                "sku": "{skuName.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task PublishAsRedisPublishesRedisAsAzureRedisConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddRedis("cache")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
            .PublishAsAzureRedis();

        Assert.True(redis.Resource.IsContainer());

        Assert.Equal("localhost:12455", await redis.Resource.GetConnectionStringAsync());

        var manifest = await ManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{cache.secretOutputs.connectionString}",
              "path": "cache.module.bicep",
              "params": {
                "principalId": "",
                "keyVaultName": "",
                "principalType": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param keyVaultName string

            @description('')
            param principalId string

            @description('')
            param principalType string


            resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
              name: keyVaultName
            }

            resource redisCache_p9fE6TK3F 'Microsoft.Cache/Redis@2020-06-01' = {
              name: toLower(take(concat('cache', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'cache'
              }
              properties: {
                enableNonSslPort: false
                minimumTlsVersion: '1.2'
                sku: {
                  name: 'Basic'
                  family: 'C'
                  capacity: 1
                }
              }
            }

            resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
              parent: keyVault_IeF8jZvXV
              name: 'connectionString'
              location: location
              properties: {
                value: '${redisCache_p9fE6TK3F.properties.hostName},ssl=true,password=${redisCache_p9fE6TK3F.listKeys(redisCache_p9fE6TK3F.apiVersion).primaryKey}'
              }
            }
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddKeyVault()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mykv = builder.AddAzureKeyVault("mykv");

        var manifest = await ManifestUtils.GetManifestWithBicep(mykv.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{mykv.outputs.vaultUri}",
              "path": "mykv.module.bicep",
              "params": {
                "principalId": "",
                "principalType": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param principalId string

            @description('')
            param principalType string


            resource keyVault_IKWI2x0B5 'Microsoft.KeyVault/vaults@2022-07-01' = {
              name: toLower(take(concat('mykv', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'mykv'
              }
              properties: {
                tenantId: tenant().tenantId
                sku: {
                  name: 'standard'
                  family: 'A'
                }
                enableRbacAuthorization: true
              }
            }

            resource roleAssignment_Z4xb36awa 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: keyVault_IKWI2x0B5
              name: guid(keyVault_IKWI2x0B5.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
                principalId: principalId
                principalType: principalType
              }
            }

            output vaultUri string = keyVault_IKWI2x0B5.properties.vaultUri
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureSignalR()
    {
        var builder = DistributedApplication.CreateBuilder();

        var signalr = builder.AddAzureSignalR("signalr");

        var manifest = await ManifestUtils.GetManifestWithBicep(signalr.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Endpoint=https://{signalr.outputs.hostName};AuthType=azure",
              "path": "signalr.module.bicep",
              "params": {
                "principalId": "",
                "principalType": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param principalId string

            @description('')
            param principalType string


            resource signalRService_hoCuRhvyj 'Microsoft.SignalRService/signalR@2022-02-01' = {
              name: toLower(take(concat('signalr', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'signalr'
              }
              sku: {
                name: 'Free_F1'
                capacity: 1
              }
              kind: 'SignalR'
              properties: {
                features: [
                  {
                    flag: 'ServiceMode'
                    value: 'Default'
                  }
                ]
                cors: {
                  allowedOrigins: [
                    '*'
                  ]
                }
              }
            }

            resource roleAssignment_O1jxNBUgA 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: signalRService_hoCuRhvyj
              name: guid(signalRService_hoCuRhvyj.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalId: principalId
                principalType: principalType
              }
            }

            output hostName string = signalRService_hoCuRhvyj.properties.hostName
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async void AsAzureSqlDatabase()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sql = builder.AddSqlServer("sql").AsAzureSqlDatabase((azureSqlBuilder, _, _, _) =>
        {
            azureSqlBuilder.Resource.Outputs["sqlServerFqdn"] = "myserver";
        });
        sql.AddDatabase("db", "dbName");

        var manifest = await ManifestUtils.GetManifestWithBicep(sql.Resource);

        Assert.Equal("Server=tcp:myserver,1433;Encrypt=True;Authentication=\"Active Directory Default\"", await sql.Resource.GetConnectionStringAsync(default));
        Assert.Equal("Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.ConnectionStringExpression.ValueExpression);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sql.module.bicep",
              "params": {
                "principalId": "",
                "principalName": "",
                "principalType": ""
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 22,
                      "minLower": 1,
                      "minUpper": 1,
                      "minNumeric": 1
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param principalId string

            @description('')
            param principalName string

            @description('')
            param principalType string


            resource sqlServer_l5O9GRsSn 'Microsoft.Sql/servers@2020-11-01-preview' = {
              name: toLower(take(concat('sql', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'sql'
              }
              properties: {
                version: '12.0'
                minimalTlsVersion: '1.2'
                publicNetworkAccess: 'Enabled'
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

            resource sqlFirewallRule_Kr30BcxQt 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
              parent: sqlServer_l5O9GRsSn
              name: 'AllowAllAzureIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '0.0.0.0'
              }
            }

            resource sqlFirewallRule_fA0ew2DcB 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
              parent: sqlServer_l5O9GRsSn
              name: 'fw'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '255.255.255.255'
              }
            }

            resource sqlDatabase_F6FwuheAS 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
              parent: sqlServer_l5O9GRsSn
              name: 'dbName'
              location: location
              properties: {
              }
            }

            output sqlServerFqdn string = sqlServer_l5O9GRsSn.properties.fullyQualifiedDomainName
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AsAzurePostgresFlexibleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.Configuration["Parameters:usr"] = "user";
        builder.Configuration["Parameters:pwd"] = "password";

        var usr = builder.AddParameter("usr");
        var pwd = builder.AddParameter("pwd", secret: true);

        IResourceBuilder<AzurePostgresResource>? azurePostgres = null;
        var postgres = builder.AddPostgres("postgres").AsAzurePostgresFlexibleServer((resource, _, _) =>
        {
            Assert.NotNull(resource);
            azurePostgres = resource;
        },
        usr, pwd
        );
        postgres.AddDatabase("db", "dbName");

        var manifest = await ManifestUtils.GetManifestWithBicep(postgres.Resource);

        // Setup to verify that connection strings is acquired via resource connectionstring redirct.
        Assert.NotNull(azurePostgres);
        azurePostgres.Resource.SecretOutputs["connectionString"] = "myconnectionstring";
        Assert.Equal("myconnectionstring", await postgres.Resource.GetConnectionStringAsync(default));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres.secretOutputs.connectionString}",
              "path": "postgres.module.bicep",
              "params": {
                "principalId": "",
                "keyVaultName": "",
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}",
                "principalType": ""
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 22
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param administratorLogin string

            @secure()
            @description('')
            param administratorLoginPassword string

            @description('')
            param principalId string

            @description('')
            param keyVaultName string

            @description('')
            param principalType string


            resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
              name: keyVaultName
            }

            resource postgreSqlFlexibleServer_NYWb9Nbel 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
              name: toLower(take(concat('postgres', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'postgres'
              }
              sku: {
                name: 'Standard_B1ms'
                tier: 'Burstable'
              }
              properties: {
                administratorLogin: administratorLogin
                administratorLoginPassword: administratorLoginPassword
                version: '16'
                storage: {
                  storageSizeGB: 32
                }
                backup: {
                  backupRetentionDays: 7
                  geoRedundantBackup: 'Disabled'
                }
                highAvailability: {
                  mode: 'Disabled'
                }
                availabilityZone: '1'
              }
            }

            resource postgreSqlFirewallRule_2vbo6vMGo 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_NYWb9Nbel
              name: 'AllowAllAzureIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '0.0.0.0'
              }
            }

            resource postgreSqlFirewallRule_oFtHmDYkz 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_NYWb9Nbel
              name: 'AllowAllIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '255.255.255.255'
              }
            }

            resource postgreSqlFlexibleServerDatabase_TDYmKfyJc 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_NYWb9Nbel
              name: 'dbName'
              properties: {
              }
            }

            resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
              parent: keyVault_IeF8jZvXV
              name: 'connectionString'
              location: location
              properties: {
                value: 'Host=${postgreSqlFlexibleServer_NYWb9Nbel.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
              }
            }
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task PublishAsAzurePostgresFlexibleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.Configuration["Parameters:usr"] = "user";
        builder.Configuration["Parameters:pwd"] = "password";

        var usr = builder.AddParameter("usr");
        var pwd = builder.AddParameter("pwd", secret: true);

        var postgres = builder.AddPostgres("postgres").PublishAsAzurePostgresFlexibleServer(usr, pwd);
        postgres.AddDatabase("db");

        var manifest = await ManifestUtils.GetManifestWithBicep(postgres.Resource);

        // Verify that when PublishAs variant is used, connection string acquisition
        // still uses the local endpoint.
        postgres.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 1234));
        var expectedConnectionString = $"Host=localhost;Port=1234;Username=postgres;Password={postgres.Resource.Password}";
        Assert.Equal(expectedConnectionString, await postgres.Resource.GetConnectionStringAsync(default));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres.secretOutputs.connectionString}",
              "path": "postgres.module.bicep",
              "params": {
                "principalId": "",
                "keyVaultName": "",
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}",
                "principalType": ""
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 22
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());
    }

    [Fact]
    public async Task PublishAsAzurePostgresFlexibleServerNoUserPassParams()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres = builder.AddPostgres("postgres1")
            .PublishAsAzurePostgresFlexibleServer(); // Because of InternalsVisibleTo

        var manifest = await ManifestUtils.GetManifest(postgres.Resource);
        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres1.secretOutputs.connectionString}",
              "path": "postgres1.module.bicep",
              "params": {
                "principalId": "",
                "keyVaultName": "",
                "administratorLogin": "{postgres1.inputs.username}",
                "administratorLoginPassword": "{postgres1.inputs.password}",
                "principalType": ""
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 22
                    }
                  }
                },
                "username": {
                  "type": "string",
                  "default": {
                    "generate": {
                      "minLength": 10,
                      "numeric": false,
                      "special": false
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        var param = builder.AddParameter("param");

        postgres = builder.AddPostgres("postgres2")
            .PublishAsAzurePostgresFlexibleServer(administratorLogin: param);

        manifest = await ManifestUtils.GetManifest(postgres.Resource);
        expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres2.secretOutputs.connectionString}",
              "path": "postgres2.module.bicep",
              "params": {
                "principalId": "",
                "keyVaultName": "",
                "administratorLogin": "{param.value}",
                "administratorLoginPassword": "{postgres2.inputs.password}",
                "principalType": ""
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 22
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        postgres = builder.AddPostgres("postgres3")
            .PublishAsAzurePostgresFlexibleServer(administratorLoginPassword: param);

        manifest = await ManifestUtils.GetManifest(postgres.Resource);
        expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres3.secretOutputs.connectionString}",
              "path": "postgres3.module.bicep",
              "params": {
                "principalId": "",
                "keyVaultName": "",
                "administratorLogin": "{postgres3.inputs.username}",
                "administratorLoginPassword": "{param.value}",
                "principalType": ""
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 22
                    }
                  }
                },
                "username": {
                  "type": "string",
                  "default": {
                    "generate": {
                      "minLength": 10,
                      "numeric": false,
                      "special": false
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task AddAzureServiceBus()
    {
        var builder = DistributedApplication.CreateBuilder();
        var serviceBus = builder.AddAzureServiceBus("sb");

        serviceBus
            .AddQueue("queue1")
            .AddQueue("queue2")
            .AddTopic("t1")
            .AddTopic("t2")
            .AddSubscription("t1", "s3");

        serviceBus.Resource.Outputs["serviceBusEndpoint"] = "mynamespaceEndpoint";

        var connectionStringResource = (IResourceWithConnectionString)serviceBus.Resource;

        Assert.Equal("sb", serviceBus.Resource.Name);
        Assert.Equal("mynamespaceEndpoint", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", connectionStringResource.ConnectionStringExpression.ValueExpression);

        var manifest = await ManifestUtils.GetManifestWithBicep(serviceBus.Resource);
        var expected = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{sb.outputs.serviceBusEndpoint}",
              "path": "sb.module.bicep",
              "params": {
                "principalId": "",
                "principalType": ""
              }
            }
            """;
        Assert.Equal(expected, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param sku string = 'Standard'

            @description('')
            param principalId string

            @description('')
            param principalType string


            resource serviceBusNamespace_RuSlLOK64 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
              name: toLower(take(concat('sb', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'sb'
              }
              sku: {
                name: sku
              }
              properties: {
                minimumTlsVersion: '1.2'
              }
            }

            resource roleAssignment_IS9HJzhT8 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: serviceBusNamespace_RuSlLOK64
              name: guid(serviceBusNamespace_RuSlLOK64.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalId: principalId
                principalType: principalType
              }
            }

            resource serviceBusQueue_XlB4dhrJO 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
              parent: serviceBusNamespace_RuSlLOK64
              name: 'queue1'
              location: location
              properties: {
              }
            }

            resource serviceBusQueue_Q6ytJFbRX 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
              parent: serviceBusNamespace_RuSlLOK64
              name: 'queue2'
              location: location
              properties: {
              }
            }

            resource serviceBusTopic_Ghv0Edotu 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
              parent: serviceBusNamespace_RuSlLOK64
              name: 't1'
              location: location
              properties: {
              }
            }

            resource serviceBusSubscription_uPeK9Nyv8 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
              parent: serviceBusTopic_Ghv0Edotu
              name: 's3'
              location: location
              properties: {
              }
            }

            resource serviceBusTopic_v5qGIuxZg 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
              parent: serviceBusNamespace_RuSlLOK64
              name: 't2'
              location: location
              properties: {
              }
            }

            output serviceBusEndpoint string = serviceBusNamespace_RuSlLOK64.properties.serviceBusEndpoint
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureStorage()
    {
        var builder = DistributedApplication.CreateBuilder();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage", (_, _, sa) =>
        {
            sa.AssignProperty(x => x.Sku.Name, storagesku);
        });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        var storageManifest = await ManifestUtils.GetManifestWithBicep(storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "principalId": "",
                "principalType": "",
                "storagesku": "{storagesku.value}"
              }
            }
            """;
        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param principalId string

            @description('')
            param principalType string

            @description('')
            param storagesku string


            resource storageAccount_65zdmu5tK 'Microsoft.Storage/storageAccounts@2022-09-01' = {
              name: toLower(take(concat('storage', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'storage'
              }
              sku: {
                name: storagesku
              }
              kind: 'StorageV2'
              properties: {
                accessTier: 'Hot'
              }
            }

            resource blobService_24WqMwYy8 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
              parent: storageAccount_65zdmu5tK
              name: 'default'
              properties: {
              }
            }

            resource roleAssignment_ryHNwVXTs 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_65zdmu5tK
              name: guid(storageAccount_65zdmu5tK.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_hqRD0luQx 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_65zdmu5tK
              name: guid(storageAccount_65zdmu5tK.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_5PGf5zmoW 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_65zdmu5tK
              name: guid(storageAccount_65zdmu5tK.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
                principalId: principalId
                principalType: principalType
              }
            }

            output blobEndpoint string = storageAccount_65zdmu5tK.properties.primaryEndpoints.blob
            output queueEndpoint string = storageAccount_65zdmu5tK.properties.primaryEndpoints.queue
            output tableEndpoint string = storageAccount_65zdmu5tK.properties.primaryEndpoints.table

            """;
        Assert.Equal(expectedBicep, storageManifest.BicepText);

        // Check blob resource.
        var blob = storage.AddBlobs("blob");

        var connectionStringBlobResource = (IResourceWithConnectionString)blob.Resource;

        Assert.Equal("https://myblob", await connectionStringBlobResource.GetConnectionStringAsync());
        var expectedBlobManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.blobEndpoint}"
            }
            """;
        var blobManifest = await ManifestUtils.GetManifest(blob.Resource);
        Assert.Equal(expectedBlobManifest, blobManifest.ToString());

        // Check queue resource.
        var queue = storage.AddQueues("queue");

        var connectionStringQueueResource = (IResourceWithConnectionString)queue.Resource;

        Assert.Equal("https://myqueue", await connectionStringQueueResource.GetConnectionStringAsync());
        var expectedQueueManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.queueEndpoint}"
            }
            """;
        var queueManifest = await ManifestUtils.GetManifest(queue.Resource);
        Assert.Equal(expectedQueueManifest, queueManifest.ToString());

        // Check table resource.
        var table = storage.AddTables("table");

        var connectionStringTableResource = (IResourceWithConnectionString)table.Resource;

        Assert.Equal("https://mytable", await connectionStringTableResource.GetConnectionStringAsync());
        var expectedTableManifest = """
            {
              "type": "value.v0",
              "connectionString": "{storage.outputs.tableEndpoint}"
            }
            """;
        var tableManifest = await ManifestUtils.GetManifest(table.Resource);
        Assert.Equal(expectedTableManifest, tableManifest.ToString());

    }

    [Fact]
    public async Task AddAzureSearch()
    {
        var builder = DistributedApplication.CreateBuilder();

        // Add search and parameterize the SKU
        var sku = builder.AddParameter("searchSku");
        var search = builder.AddAzureSearch("search", (_, _, search) =>
            search.AssignProperty(me => me.SkuName, sku));

        // Pretend we deployed it
        const string fakeConnectionString = "mysearchconnectionstring";
        search.Resource.Outputs["connectionString"] = fakeConnectionString;

        var connectionStringResource = (IResourceWithConnectionString)search.Resource;

        // Validate the resource
        Assert.Equal("search", search.Resource.Name);
        Assert.Equal("{search.outputs.connectionString}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(fakeConnectionString, await connectionStringResource.GetConnectionStringAsync());

        var manifest = await ManifestUtils.GetManifestWithBicep(search.Resource);

        // Validate the manifest
        var expectedManifest =
            """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{search.outputs.connectionString}",
              "path": "search.module.bicep",
              "params": {
                "principalId": "",
                "principalType": "",
                "searchSku": "{searchSku.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param principalId string

            @description('')
            param principalType string

            @description('')
            param searchSku string


            resource searchService_7WkaGluF0 'Microsoft.Search/searchServices@2023-11-01' = {
              name: toLower(take(concat('search', uniqueString(resourceGroup().id)), 24))
              location: location
              tags: {
                'aspire-resource-name': 'search'
              }
              sku: {
                name: 'basic'
              }
              properties: {
                replicaCount: 1
                partitionCount: 1
                hostingMode: 'default'
                disableLocalAuth: true
              }
            }

            resource roleAssignment_7uytIREoa 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: searchService_7WkaGluF0
              name: guid(searchService_7WkaGluF0.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_QpFzCj55x 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: searchService_7WkaGluF0
              name: guid(searchService_7WkaGluF0.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
                principalId: principalId
                principalType: principalType
              }
            }

            output connectionString string = 'Endpoint=https://${searchService_7WkaGluF0.name}.search.windows.net'
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task PublishAsConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();

        var ai = builder.AddAzureApplicationInsights("ai").PublishAsConnectionString();
        var serviceBus = builder.AddAzureServiceBus("servicebus").PublishAsConnectionString();

        var serviceA = builder.AddProject<Projects.ServiceA>("serviceA")
            .WithReference(ai)
            .WithReference(serviceBus);

        var aiManifest = await ManifestUtils.GetManifest(ai.Resource);
        Assert.Equal("{ai.value}", aiManifest["connectionString"]?.ToString());
        Assert.Equal("parameter.v0", aiManifest["type"]?.ToString());

        var serviceBusManifest = await ManifestUtils.GetManifest(serviceBus.Resource);
        Assert.Equal("{servicebus.value}", serviceBusManifest["connectionString"]?.ToString());
        Assert.Equal("parameter.v0", serviceBusManifest["type"]?.ToString());

        var serviceManifest = await ManifestUtils.GetManifest(serviceA.Resource);
        Assert.Equal("{ai.connectionString}", serviceManifest["env"]?["APPLICATIONINSIGHTS_CONNECTION_STRING"]?.ToString());
        Assert.Equal("{servicebus.connectionString}", serviceManifest["env"]?["ConnectionStrings__servicebus"]?.ToString());
    }

    [Fact]
    public async Task AddAzureOpenAI()
    {
        var builder = DistributedApplication.CreateBuilder();

        var openai = builder.AddAzureOpenAI("openai")
            .AddDeployment(new("mymodel", "gpt-35-turbo", "0613", "Basic", 4));

        var manifest = await ManifestUtils.GetManifestWithBicep(openai.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{openai.outputs.connectionString}",
              "path": "openai.module.bicep",
              "params": {
                "principalId": "",
                "principalType": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param principalId string

            @description('')
            param principalType string


            resource cognitiveServicesAccount_6g8jyEjX5 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
              name: toLower(take(concat('openai', uniqueString(resourceGroup().id)), 24))
              location: location
              kind: 'OpenAI'
              sku: {
                name: 'S0'
              }
              properties: {
                customSubDomainName: toLower(take(concat('openai', uniqueString(resourceGroup().id)), 24))
                publicNetworkAccess: 'Enabled'
              }
            }

            resource roleAssignment_X7ie0XqR2 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: cognitiveServicesAccount_6g8jyEjX5
              name: guid(cognitiveServicesAccount_6g8jyEjX5.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
                principalId: principalId
                principalType: principalType
              }
            }

            resource cognitiveServicesAccountDeployment_paT2Ndfh7 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
              parent: cognitiveServicesAccount_6g8jyEjX5
              name: 'mymodel'
              sku: {
                name: 'Basic'
                capacity: 4
              }
              properties: {
                model: {
                  name: 'gpt-35-turbo'
                  format: 'OpenAI'
                  version: '0613'
                }
              }
            }

            output connectionString string = 'Endpoint=${cognitiveServicesAccount_6g8jyEjX5.properties.endpoint}'
            
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }
}
