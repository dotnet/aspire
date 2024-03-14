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
    public async Task AddAzureCosmosDb()
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        cosmos.AddDatabase("mydatabase");

        cosmos.Resource.SecretOutputs["connectionString"] = "mycosmosconnectionstring";

        var databases = cosmos.Resource.Parameters["databases"] as IEnumerable<string>;
        var connectionStringResource = (IResourceWithConnectionString)cosmos.Resource;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.cosmosdb.bicep", cosmos.Resource.TemplateResourceName);
        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("cosmos", cosmos.Resource.Parameters["databaseAccountName"]);
        Assert.NotNull(databases);
        Assert.Equal(["mydatabase"], databases);
        Assert.Equal("mycosmosconnectionstring", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{cosmos.secretOutputs.connectionString}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task AddAzureCosmosDbConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        IEnumerable<CosmosDBSqlDatabase>? callbackDatabases = null;
        var cosmos = builder.AddAzureCosmosDBConstruct("cosmos", (resource, construct, account, databases) =>
        {
            callbackDatabases = databases;
        });
        cosmos.AddDatabase("mydatabase");

        cosmos.Resource.SecretOutputs["connectionString"] = "mycosmosconnectionstring";

        var connectionStringResource = (IResourceWithConnectionString)cosmos.Resource;

        var manifest = await ManifestUtils.GetManifest(cosmos.Resource);
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

        Assert.Equal(expectedManifest, manifest.ToString());

        Assert.NotNull(callbackDatabases);
        Assert.Collection(
            callbackDatabases,
            (database) => Assert.Equal("mydatabase", database.Properties.Name)
            );

        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("mycosmosconnectionstring", await connectionStringResource.GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureAppConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appConfig = builder.AddAzureAppConfiguration("appConfig");

        appConfig.Resource.Outputs["appConfigEndpoint"] = "https://myendpoint";

        var connectionStringResource = (IResourceWithConnectionString)appConfig.Resource;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.appconfig.bicep", appConfig.Resource.TemplateResourceName);
        Assert.Equal("appConfig", appConfig.Resource.Name);
        Assert.Equal("appconfig", appConfig.Resource.Parameters["configName"]);
        Assert.Equal("https://myendpoint", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{appConfig.outputs.appConfigEndpoint}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task AddAzureAppConfigurationConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appConfig = builder.AddAzureAppConfigurationConstruct("appConfig");

        appConfig.Resource.Outputs["appConfigEndpoint"] = "https://myendpoint";

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

        var manifest = await ManifestUtils.GetManifest(appConfig.Resource);
        Assert.Equal(expectedManifest, manifest.ToString());
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

        var keyVault = builder.AddAzureKeyVault("keyVault");

        keyVault.Resource.Outputs["vaultUri"] = "https://myvault";

        var connectionStringResource = (IResourceWithConnectionString)keyVault.Resource;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.keyvault.bicep", keyVault.Resource.TemplateResourceName);
        Assert.Equal("keyVault", keyVault.Resource.Name);
        Assert.Equal("keyvault", keyVault.Resource.Parameters["vaultName"]);
        Assert.Equal("https://myvault", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{keyVault.outputs.vaultUri}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task AddKeyVaultConstruct()
    {
        var builder = DistributedApplication.CreateBuilder();

        global::Azure.Provisioning.KeyVaults.KeyVault? cdkKeyVault = null;
        var mykv = builder.AddAzureKeyVaultConstruct("mykv", (construct, cdkResource) =>
        {
            cdkKeyVault = cdkResource;
        });

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

        Assert.Equal("mykv", mykv.Resource.Name);
        var manifest = await ManifestUtils.GetManifest(mykv.Resource);
        Assert.Equal(expectedManifest, manifest.ToString());

        Assert.NotNull(cdkKeyVault);
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
        Assert.Equal("Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.ConnectionStringExpression.ValueExpression);
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

        Assert.Equal("{postgres.secretOutputs.connectionString}", azurePostgres.Resource.ConnectionStringExpression.ValueExpression);
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

        Assert.Equal("{postgres.secretOutputs.connectionString}", azurePostgres.Resource.ConnectionStringExpression.ValueExpression);

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

        var connectionStringResource = (IResourceWithConnectionString)serviceBus.Resource;

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
        Assert.Equal("mynamespaceEndpoint", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", connectionStringResource.ConnectionStringExpression.ValueExpression);
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

        var connectionStringResource = (IResourceWithConnectionString)serviceBus.Resource;

        Assert.Equal("sb", serviceBus.Resource.Name);
        Assert.Equal("mynamespaceEndpoint", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", connectionStringResource.ConnectionStringExpression.ValueExpression);

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

        var storage = builder.AddAzureStorage("storage");

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        var blob = storage.AddBlobs("blob");
        var queue = storage.AddQueues("queue");
        var table = storage.AddTables("table");

        var connectionStringBlobResource = (IResourceWithConnectionString)blob.Resource;
        var connectionStringQueueResource = (IResourceWithConnectionString)queue.Resource;
        var connectionStringTableResource = (IResourceWithConnectionString)table.Resource;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.storage.bicep", storage.Resource.TemplateResourceName);
        Assert.Equal("storage", storage.Resource.Name);
        Assert.Equal("storage", storage.Resource.Parameters["storageName"]);

        Assert.Equal("https://myblob", await connectionStringBlobResource.GetConnectionStringAsync());
        Assert.Equal("https://myqueue", await connectionStringQueueResource.GetConnectionStringAsync());
        Assert.Equal("https://mytable", await connectionStringTableResource.GetConnectionStringAsync());
        Assert.Equal("{storage.outputs.blobEndpoint}", connectionStringBlobResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{storage.outputs.queueEndpoint}", connectionStringQueueResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{storage.outputs.tableEndpoint}", connectionStringTableResource.ConnectionStringExpression.ValueExpression);

        var blobManifest = await ManifestUtils.GetManifest(blob.Resource);
        Assert.Equal("{storage.outputs.blobEndpoint}", blobManifest["connectionString"]?.ToString());

        var queueManifest = await ManifestUtils.GetManifest(queue.Resource);
        Assert.Equal("{storage.outputs.queueEndpoint}", queueManifest["connectionString"]?.ToString());

        var tableManifest = await ManifestUtils.GetManifest(table.Resource);
        Assert.Equal("{storage.outputs.tableEndpoint}", tableManifest["connectionString"]?.ToString());
    }

    [Fact]
    public async Task AddAzureConstructStorage()
    {
        var builder = DistributedApplication.CreateBuilder();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureConstructStorage("storage", (_, sa) =>
        {
            sa.AssignProperty(x => x.Sku.Name, storagesku);
        });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);
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
        var storageManifest = await ManifestUtils.GetManifest(storage.Resource);
        Assert.Equal(expectedStorageManifest, storageManifest.ToString());

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

        var search = builder.AddAzureSearch("search");

        search.Resource.Outputs["connectionString"] = "mysearchconnectionstring";

        var connectionStringResource = (IResourceWithConnectionString)search.Resource;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.search.bicep", search.Resource.TemplateResourceName);
        Assert.Equal("search", search.Resource.Name);
        Assert.Equal("mysearchconnectionstring", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{search.outputs.connectionString}", connectionStringResource.ConnectionStringExpression.ValueExpression);
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

        var connectionStringResource = (IResourceWithConnectionString)search.Resource;

        // Validate the resource
        Assert.Equal("search", search.Resource.Name);
        Assert.Equal("{search.outputs.connectionString}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(fakeConnectionString, await connectionStringResource.GetConnectionStringAsync());

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
            .WithDeployment(new("mymodel", "gpt-35-turbo", "0613", "Basic", 4));

        openai.Resource.Outputs["connectionString"] = "myopenaiconnectionstring";

        var connectionStringResource = (IResourceWithConnectionString)openai.Resource;

        var callback = openai.Resource.Parameters["deployments"] as Func<object?>;
        var deployments = callback?.Invoke() as JsonArray;
        var deployment = deployments?.FirstOrDefault();

        Assert.Equal("Aspire.Hosting.Azure.Bicep.openai.bicep", openai.Resource.TemplateResourceName);
        Assert.Equal("openai", openai.Resource.Name);
        Assert.Equal("myopenaiconnectionstring", await connectionStringResource.GetConnectionStringAsync(default));
        Assert.Equal("{openai.outputs.connectionString}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.NotNull(deployment);
        Assert.Equal("mymodel", deployment["name"]?.ToString());
        Assert.Equal("Basic", deployment["sku"]?["name"]?.ToString());
        Assert.Equal(4, deployment["sku"]?["capacity"]?.GetValue<int>());
        Assert.Equal("OpenAI", deployment["model"]?["format"]?.ToString());
        Assert.Equal("gpt-35-turbo", deployment["model"]?["name"]?.ToString());
        Assert.Equal("0613", deployment["model"]?["version"]?.ToString());
    }
}
