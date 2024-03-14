// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.Sql;
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
#pragma warning disable CA2252 // This API requires opting into preview features
        var cosmos = builder.AddAzureCosmosDB("cosmos", (resource, construct, account, databases) =>
        {
            callbackDatabases = databases;
        });
#pragma warning restore CA2252 // This API requires opting into preview features
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

        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("mycosmosconnectionstring", await cosmos.Resource.GetConnectionStringAsync(default));
    }

    [Fact]
    public async Task AddAzureAppConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appConfig = builder.AddAzureAppConfiguration("appConfig");
        appConfig.Resource.Outputs["appConfigEndpoint"] = "https://myendpoint";
        Assert.Equal("https://myendpoint", await appConfig.Resource.GetConnectionStringAsync(default));

        var manifest = await ManifestUtils.GetManifestWithBicep(appConfig.Resource);

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

        Assert.Equal("Aspire.Hosting.Azure.Bicep.appinsights.bicep", appInsights.Resource.TemplateResourceName);
        Assert.Equal("appInsights", appInsights.Resource.Name);
        Assert.Equal("appinsights", appInsights.Resource.Parameters["appInsightsName"]);
        Assert.True(appInsights.Resource.Parameters.ContainsKey(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId));
        Assert.Equal("myinstrumentationkey", await appInsights.Resource.GetConnectionStringAsync(default));
        Assert.Equal("{appInsights.outputs.appInsightsConnectionString}", appInsights.Resource.ConnectionStringExpression);

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
    public async Task PublishAsRedisPublishesRedisAsAzureRedis()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddRedis("cache")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
            .PublishAsAzureRedis();

        Assert.True(redis.Resource.IsContainer());

        Assert.Equal("localhost:12455", await redis.Resource.GetConnectionStringAsync());

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        Assert.Equal("azure.bicep.v0", manifest["type"]?.ToString());
        Assert.Equal("{cache.secretOutputs.connectionString}", manifest["connectionString"]?.ToString());
    }

    [Fact]
    public async Task PublishAsRedisPublishesRedisAsAzureRedisConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddRedis("cache")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
            .PublishAsAzureRedisConstruct(useProvisioner: false); // Resolving abiguity due to InternalsVisibleTo

        Assert.True(redis.Resource.IsContainer());

        Assert.Equal("localhost:12455", await redis.Resource.GetConnectionStringAsync());

        var manifest = await ManifestUtils.GetManifest(redis.Resource);
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

        Assert.Equal(expectedManifest, manifest.ToString());
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
    public async Task AddSignalRConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        var signalr = builder.AddAzureSignalRConstruct("signalr");

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

        var manifest = await ManifestUtils.GetManifest(signalr.Resource);
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task AsAzureSqlDatabase()
    {
        var builder = DistributedApplication.CreateBuilder();

        IResourceBuilder<AzureSqlServerResource>? azureSql = null;
        var sql = builder.AddSqlServer("sql").AsAzureSqlDatabase(resource =>
        {
            azureSql = resource;
        });
        sql.AddDatabase("db", "dbName");

        Assert.NotNull(azureSql);
        azureSql.Resource.Outputs["sqlServerFqdn"] = "myserver";

        var databasesCallback = azureSql.Resource.Parameters["databases"] as Func<object?>;
        Assert.NotNull(databasesCallback);
        var databases = databasesCallback() as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.sql.bicep", azureSql.Resource.TemplateResourceName);
        Assert.Equal("sql", sql.Resource.Name);
        Assert.Equal("sql", azureSql.Resource.Parameters["serverName"]);
        Assert.NotNull(databases);
        Assert.Equal(["dbName"], databases);
        Assert.Equal("Server=tcp:myserver,1433;Encrypt=True;Authentication=\"Active Directory Default\"", await sql.Resource.GetConnectionStringAsync(default));
        Assert.Equal("Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.ConnectionStringExpression);
    }

    [Fact]
    public async void AsAzureSqlDatabaseConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        global::Azure.Provisioning.Sql.SqlServer? cdkSqlServer = null;
        AzureSqlServerConstructResource? azureSql = null;
        List<SqlDatabase>? cdkSqlDatabases = null;
        var sql = builder.AddSqlServer("sql").AsAzureSqlDatabaseConstruct((construct, sqlServer, databases) =>
        {
            azureSql = construct.Resource as AzureSqlServerConstructResource;
            cdkSqlServer = sqlServer;
            cdkSqlDatabases = databases.ToList();
        });
        sql.AddDatabase("db", "dbName");

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
        var manifest = await ManifestUtils.GetManifest(sql.Resource);
        Assert.Equal(expectedManifest, manifest.ToString());

        Assert.NotNull(cdkSqlServer);
        Assert.NotNull(azureSql);
        Assert.NotNull(cdkSqlDatabases);
        Assert.Equal("dbName", cdkSqlDatabases[0].Properties.Name);
        Assert.Equal("sql", sql.Resource.Name);
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
        var postgres = builder.AddPostgres("postgres").AsAzurePostgresFlexibleServer(usr, pwd, (resource) =>
        {
            Assert.NotNull(resource);
            azurePostgres = resource;
        });
        postgres.AddDatabase("db", "dbName");

        Assert.NotNull(azurePostgres);

        var databasesCallback = azurePostgres.Resource.Parameters["databases"] as Func<object?>;
        Assert.NotNull(databasesCallback);
        var databases = databasesCallback() as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.postgres.bicep", azurePostgres.Resource.TemplateResourceName);
        Assert.Equal("postgres", postgres.Resource.Name);
        Assert.Equal("postgres", azurePostgres.Resource.Parameters["serverName"]);
        Assert.Same(usr.Resource, azurePostgres.Resource.Parameters["administratorLogin"]);
        Assert.Same(pwd.Resource, azurePostgres.Resource.Parameters["administratorLoginPassword"]);
        Assert.True(azurePostgres.Resource.Parameters.ContainsKey(AzureBicepResource.KnownParameters.KeyVaultName));
        Assert.NotNull(databases);
        Assert.Equal(["dbName"], databases);

        // Setup to verify that connection strings is acquired via resource connectionstring redirct.
        azurePostgres.Resource.SecretOutputs["connectionString"] = "myconnectionstring";
        Assert.Equal("myconnectionstring", await postgres.Resource.GetConnectionStringAsync(default));

        Assert.Equal("{postgres.secretOutputs.connectionString}", azurePostgres.Resource.ConnectionStringExpression);
    }

    [Fact]
    public async Task PublishAsAzurePostgresFlexibleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.Configuration["Parameters:usr"] = "user";
        builder.Configuration["Parameters:pwd"] = "password";

        var usr = builder.AddParameter("usr");
        var pwd = builder.AddParameter("pwd", secret: true);

        IResourceBuilder<AzurePostgresResource>? azurePostgres = null;
        var postgres = builder.AddPostgres("postgres").PublishAsAzurePostgresFlexibleServer(usr, pwd, (resource) =>
        {
            azurePostgres = resource;
        });
        postgres.AddDatabase("db");

        Assert.NotNull(azurePostgres);

        var databasesCallback = azurePostgres.Resource.Parameters["databases"] as Func<object?>;
        Assert.NotNull(databasesCallback);
        var databases = databasesCallback() as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.postgres.bicep", azurePostgres.Resource.TemplateResourceName);
        Assert.Equal("postgres", postgres.Resource.Name);
        Assert.Equal("postgres", azurePostgres.Resource.Parameters["serverName"]);
        Assert.Same(usr.Resource, azurePostgres.Resource.Parameters["administratorLogin"]);
        Assert.Same(pwd.Resource, azurePostgres.Resource.Parameters["administratorLoginPassword"]);
        Assert.True(azurePostgres.Resource.Parameters.ContainsKey(AzureBicepResource.KnownParameters.KeyVaultName));
        Assert.NotNull(databases);
        Assert.Equal(["db"], databases);

        // Verify that when PublishAs variant is used, connection string acquisition
        // still uses the local endpoint.
        postgres.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 1234));
        var expectedConnectionString = $"Host=localhost;Port=1234;Username=postgres;Password={postgres.Resource.Password}";
        Assert.Equal(expectedConnectionString, await postgres.Resource.GetConnectionStringAsync(default));

        Assert.Equal("{postgres.secretOutputs.connectionString}", azurePostgres.Resource.ConnectionStringExpression);

        var manifest = await ManifestUtils.GetManifest(postgres.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres.secretOutputs.connectionString}",
              "path": "aspire.hosting.azure.bicep.postgres.bicep",
              "params": {
                "serverName": "postgres",
                "keyVaultName": "",
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}",
                "databases": [
                  "db"
                ]
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
    }

    [Fact]
    public async Task PublishAsAzurePostgresFlexibleServerNoUserPassParams()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres = builder.AddPostgres("postgres1")
            .PublishAsAzurePostgresFlexibleServer();

        var manifest = await ManifestUtils.GetManifest(postgres.Resource);
        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres1.secretOutputs.connectionString}",
              "path": "aspire.hosting.azure.bicep.postgres.bicep",
              "params": {
                "serverName": "postgres1",
                "keyVaultName": "",
                "administratorLogin": "{postgres1.inputs.username}",
                "administratorLoginPassword": "{postgres1.inputs.password}",
                "databases": []
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
              "path": "aspire.hosting.azure.bicep.postgres.bicep",
              "params": {
                "serverName": "postgres2",
                "keyVaultName": "",
                "administratorLogin": "{param.value}",
                "administratorLoginPassword": "{postgres2.inputs.password}",
                "databases": []
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
              "path": "aspire.hosting.azure.bicep.postgres.bicep",
              "params": {
                "serverName": "postgres3",
                "keyVaultName": "",
                "administratorLogin": "{postgres3.inputs.username}",
                "administratorLoginPassword": "{param.value}",
                "databases": []
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
            .AddTopic("t1", ["s1", "s2"])
            .AddTopic("t2", [])
            .AddTopic("t3", ["s3"]);

        serviceBus.Resource.Outputs["serviceBusEndpoint"] = "mynamespaceEndpoint";

        var queuesCallback = serviceBus.Resource.Parameters["queues"] as Func<object?>;
        var topicsCallback = serviceBus.Resource.Parameters["topics"] as Func<object?>;
        Assert.NotNull(queuesCallback);
        Assert.NotNull(topicsCallback);
        var queues = queuesCallback() as IEnumerable<string>;
        var topics = topicsCallback() as JsonNode;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.servicebus.bicep", serviceBus.Resource.TemplateResourceName);
        Assert.Equal("sb", serviceBus.Resource.Name);
        Assert.Equal("sb", serviceBus.Resource.Parameters["serviceBusNamespaceName"]);
        Assert.NotNull(queues);
        Assert.Equal(["queue1", "queue2"], queues);
        Assert.NotNull(topics);
        Assert.Equal("""[{"name":"t1","subscriptions":["s1","s2"]},{"name":"t2","subscriptions":[]},{"name":"t3","subscriptions":["s3"]}]""", topics.ToJsonString());
        Assert.Equal("mynamespaceEndpoint", await serviceBus.Resource.GetConnectionStringAsync(default));
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", serviceBus.Resource.ConnectionStringExpression);
    }

    [Fact]
    public async Task AddAzureServiceBusConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();
        var serviceBus = builder.AddAzureServiceBusConstruct("sb");

        serviceBus
            .AddQueue("queue1")
            .AddQueue("queue2")
            .AddTopic("t1")
            .AddTopic("t2")
            .AddSubscription("t1", "s3");

        serviceBus.Resource.Outputs["serviceBusEndpoint"] = "mynamespaceEndpoint";

        Assert.Equal("sb", serviceBus.Resource.Name);
        Assert.Equal("mynamespaceEndpoint", await serviceBus.Resource.GetConnectionStringAsync());
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", serviceBus.Resource.ConnectionStringExpression);

        var manifest = await ManifestUtils.GetManifest(serviceBus.Resource);
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
        Assert.Equal(expected, manifest.ToString());
    }

    [Fact]
    public async Task AddAzureStorage()
    {
        var builder = DistributedApplication.CreateBuilder();

        var storagesku = builder.AddParameter("storagesku");
#pragma warning disable CA2252 // This API requires opting into preview features
        var storage = builder.AddAzureStorage("storage", (_, _, sa) =>
        {
            sa.AssignProperty(x => x.Sku.Name, storagesku);
        });
#pragma warning restore CA2252 // This API requires opting into preview features

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
        Assert.Equal("https://myblob", await blob.Resource.GetConnectionStringAsync());
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
        Assert.Equal("https://myqueue", await queue.Resource.GetConnectionStringAsync());
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
        Assert.Equal("https://mytable", await table.Resource.GetConnectionStringAsync());
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

        var search = builder.AddAzureSearch("search");

        search.Resource.Outputs["connectionString"] = "mysearchconnectionstring";

        Assert.Equal("Aspire.Hosting.Azure.Bicep.search.bicep", search.Resource.TemplateResourceName);
        Assert.Equal("search", search.Resource.Name);
        Assert.Equal("mysearchconnectionstring", await search.Resource.GetConnectionStringAsync(default));
        Assert.Equal("{search.outputs.connectionString}", search.Resource.ConnectionStringExpression);
    }

    [Fact]
    public async Task AddAzureSearchConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        // Add search and parameterize the SKU
        var sku = builder.AddParameter("searchSku");
        var search = builder.AddAzureConstructSearch("search", (_, search) =>
            search.AssignProperty(me => me.SkuName, sku));

        // Pretend we deployed it
        const string fakeConnectionString = "mysearchconnectionstring";
        search.Resource.Outputs["connectionString"] = fakeConnectionString;

        // Validate the resource
        Assert.Equal("search", search.Resource.Name);
        Assert.Equal("{search.outputs.connectionString}", search.Resource.ConnectionStringExpression);
        Assert.Equal(fakeConnectionString, await search.Resource.GetConnectionStringAsync());

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
        var actualManifest = (await ManifestUtils.GetManifest(search.Resource)).ToString();
        Assert.Equal(expectedManifest, actualManifest);
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
