// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Roles;
using Azure.Provisioning.Search;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureBicepResourceTests(ITestOutputHelper output)
{
    [Fact]
    public void AddBicepResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("mytemplate", "content")
                                   .WithParameter("param1", "value1")
                                   .WithParameter("param2", "value2");

        Assert.Equal("content", bicepResource.Resource.TemplateString);
        Assert.Equal("value1", bicepResource.Resource.Parameters["param1"]);
        Assert.Equal("value2", bicepResource.Resource.Parameters["param2"]);
    }

    public static TheoryData<Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>>> AzureExtensions =>
        CreateAllAzureExtensions("x");

    private static TheoryData<Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>>> CreateAllAzureExtensions(string resourceName)
    {
        static void CreateInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var id = new UserAssignedIdentity("id");
            infrastructure.Add(id);
            infrastructure.Add(new ProvisioningOutput("cid", typeof(string)) { Value = id.ClientId });
        }

        return new()
        {
            { builder => builder.AddAzureAppConfiguration(resourceName) },
            { builder => builder.AddAzureApplicationInsights(resourceName) },
            { builder => builder.AddBicepTemplate(resourceName, "template.bicep") },
            { builder => builder.AddBicepTemplateString(resourceName, "content") },
            { builder => builder.AddAzureInfrastructure(resourceName, CreateInfrastructure) },
            { builder => builder.AddAzureOpenAI(resourceName) },
            { builder => builder.AddAzureCosmosDB(resourceName) },
            { builder => builder.AddAzureEventHubs(resourceName) },
            { builder => builder.AddAzureKeyVault(resourceName) },
            { builder => builder.AddAzureLogAnalyticsWorkspace(resourceName) },
#pragma warning disable CS0618 // Type or member is obsolete
            { builder => builder.AddPostgres(resourceName).AsAzurePostgresFlexibleServer() },
            { builder => builder.AddRedis(resourceName).AsAzureRedis() },
            { builder => builder.AddSqlServer(resourceName).AsAzureSqlDatabase() },
#pragma warning restore CS0618 // Type or member is obsolete
            { builder => builder.AddAzurePostgresFlexibleServer(resourceName) },
            { builder => builder.AddAzureRedis(resourceName) },
            { builder => builder.AddAzureSearch(resourceName) },
            { builder => builder.AddAzureServiceBus(resourceName) },
            { builder => builder.AddAzureSignalR(resourceName) },
            { builder => builder.AddAzureSqlServer(resourceName) },
            { builder => builder.AddAzureStorage(resourceName) },
            { builder => builder.AddAzureWebPubSub(resourceName) },
        };
    }

    [Theory]
    [MemberData(nameof(AzureExtensions))]
    public void AzureExtensionsAutomaticallyAddAzureProvisioning(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        addAzureResource(builder);

        var app = builder.Build();
        var hooks = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        Assert.Single(hooks.OfType<AzureProvisioner>());
    }

    [Theory]
    [MemberData(nameof(AzureExtensions))]
    public void BicepResourcesAreIdempotent(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var azureResourceBuilder = addAzureResource(builder);

        if (azureResourceBuilder.Resource is not AzureProvisioningResource bicepResource)
        {
            // Skip
            return;
        }

        // This makes sure that these don't throw
        bicepResource.GetBicepTemplateFile();
        bicepResource.GetBicepTemplateFile();
    }

    public static TheoryData<Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>>> AzureExtensionsWithHyphen =>
        CreateAllAzureExtensions("x-y");

    [Theory]
    [MemberData(nameof(AzureExtensionsWithHyphen))]
    public void AzureResourcesProduceValidBicep(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var azureResourceBuilder = addAzureResource(builder);

        if (azureResourceBuilder.Resource is not AzureProvisioningResource bicepResource)
        {
            // Skip
            return;
        }

        var bicep = bicepResource.GetBicepTemplateString();

        Assert.DoesNotContain("resource x-y", bicep);
    }

    [Fact]
    public void GetOutputReturnsOutputValue()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.Outputs["resourceEndpoint"] = "https://myendpoint";

        Assert.Equal("https://myendpoint", bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetSecretOutputReturnsSecretOutputValue()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.SecretOutputs["connectionString"] = "https://myendpoint;Key=43";

        Assert.Equal("https://myendpoint;Key=43", bicepResource.GetSecretOutput("connectionString").Value);
    }

    [Fact]
    public void GetOutputValueThrowsIfNoOutput()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        Assert.Throws<InvalidOperationException>(() => bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetSecretOutputValueThrowsIfNoOutput()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        Assert.Throws<InvalidOperationException>(() => bicepResource.GetSecretOutput("connectionString").Value);
    }

    [Fact]
    public async Task AssertManifestLayout()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

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
              "path": "templ.module.bicep",
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
    public async Task AddAzureCosmosDBEmulator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator(e =>
        {
            e.WithEndpoint("emulator", e => e.AllocatedEndpoint = new(e, "localost", 10001));
        });

        Assert.True(cosmos.Resource.IsContainer());

        var csExpr = cosmos.Resource.ConnectionStringExpression;
        var cs = await csExpr.GetValueAsync(CancellationToken.None);

        var prefix = "AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;AccountEndpoint=";
        Assert.Equal(prefix + "https://{cosmos.bindings.emulator.host}:{cosmos.bindings.emulator.port};DisableServerCertificateValidation=True;", csExpr.ValueExpression);
        Assert.Equal(prefix + "https://127.0.0.1:10001;DisableServerCertificateValidation=True;", cs);
        Assert.Equal(cs, await ((IResourceWithConnectionString)cosmos.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureCosmosDB_WithAccessKeyAuthentication_NoKeyVaultWithEmulator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddAzureCosmosDB("cosmos").WithAccessKeyAuthentication().RunAsEmulator();

#pragma warning disable ASPIRECOSMOSDB001
        builder.AddAzureCosmosDB("cosmos2").WithAccessKeyAuthentication().RunAsPreviewEmulator();
#pragma warning restore ASPIRECOSMOSDB001

        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, CancellationToken.None);

        Assert.Empty(model.Resources.OfType<AzureKeyVaultResource>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("mykeyvault")]
    public async Task AddAzureCosmosDBViaRunMode_WithAccessKeyAuthentication(string? kvName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        IEnumerable<CosmosDBSqlDatabase>? callbackDatabases = null;
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .ConfigureInfrastructure(infrastructure =>
            {
                callbackDatabases = infrastructure.GetProvisionableResources().OfType<CosmosDBSqlDatabase>();
            });

        if (kvName is null)
        {
            kvName = "cosmos-kv";
            cosmos.WithAccessKeyAuthentication();
        }
        else
        {
            cosmos.WithAccessKeyAuthentication(builder.AddAzureKeyVault(kvName));
        }

        var db = cosmos.AddCosmosDatabase("db", databaseName: "mydatabase");
        db.AddContainer("container", "mypartitionkeypath", containerName: "mycontainer");

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, CancellationToken.None);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var kv = model.Resources.OfType<AzureKeyVaultResource>().Single();

        Assert.Equal(kvName, kv.Name);

        var secrets = new Dictionary<string, string>
        {
            ["connectionstrings--cosmos"] = "mycosmosconnectionstring"
        };

        kv.SecretResolver = (secretRef, _) =>
        {
            if (!secrets.TryGetValue(secretRef.SecretName, out var value))
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(value);
        };

        var manifest = await AzureManifestUtils.GetManifestWithBicep(cosmos.Resource);

        var expectedManifest = $$"""
                               {
                                 "type": "azure.bicep.v0",
                                 "connectionString": "{{{kvName}}.secrets.connectionstrings--cosmos}",
                                 "path": "cosmos.module.bicep",
                                 "params": {
                                   "keyVaultName": "{{{kvName}}.outputs.name}"
                                 }
                               }
                               """;
        var m = manifest.ManifestNode.ToString();
        output.WriteLine(m);

        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyVaultName string

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
              name: take('cosmos-${uniqueString(resourceGroup().id)}', 44)
              location: location
              properties: {
                locations: [
                  {
                    locationName: location
                    failoverPriority: 0
                  }
                ]
                consistencyPolicy: {
                  defaultConsistencyLevel: 'Session'
                }
                databaseAccountOfferType: 'Standard'
                disableLocalAuth: false
              }
              kind: 'GlobalDocumentDB'
              tags: {
                'aspire-resource-name': 'cosmos'
              }
            }

            resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
              name: 'mydatabase'
              location: location
              properties: {
                resource: {
                  id: 'mydatabase'
                }
              }
              parent: cosmos
            }

            resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
              name: 'mycontainer'
              location: location
              properties: {
                resource: {
                  id: 'mycontainer'
                  partitionKey: {
                    paths: [
                      'mypartitionkeypath'
                    ]
                  }
                }
              }
              parent: db
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

            resource db_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--db'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydatabase'
              }
              parent: keyVault
            }

            resource container_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--container'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydatabase;Container=mycontainer'
              }
              parent: keyVault
            }

            output name string = cosmos.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        Assert.NotNull(callbackDatabases);
        Assert.Collection(
            callbackDatabases,
            (database) => Assert.Equal("mydatabase", database.Name.Value)
            );

        var connectionStringResource = (IResourceWithConnectionString)cosmos.Resource;

        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("mycosmosconnectionstring", await connectionStringResource.GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureCosmosDBViaRunMode_NoAccessKeyAuthentication()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        IEnumerable<CosmosDBSqlDatabase>? callbackDatabases = null;
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .ConfigureInfrastructure(infrastructure =>
            {
                callbackDatabases = infrastructure.GetProvisionableResources().OfType<CosmosDBSqlDatabase>();
            });
        var db = cosmos.AddCosmosDatabase("mydatabase");
        db.AddContainer("mycontainer", "mypartitionkeypath");

        cosmos.Resource.Outputs["connectionString"] = "mycosmosconnectionstring";

        var manifest = await AzureManifestUtils.GetManifestWithBicep(cosmos.Resource);

        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "connectionString": "{cosmos.outputs.connectionString}",
                                 "path": "cosmos.module.bicep"
                               }
                               """;

        output.WriteLine(manifest.ManifestNode.ToString());
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
              name: take('cosmos-${uniqueString(resourceGroup().id)}', 44)
              location: location
              properties: {
                locations: [
                  {
                    locationName: location
                    failoverPriority: 0
                  }
                ]
                consistencyPolicy: {
                  defaultConsistencyLevel: 'Session'
                }
                databaseAccountOfferType: 'Standard'
                disableLocalAuth: true
              }
              kind: 'GlobalDocumentDB'
              tags: {
                'aspire-resource-name': 'cosmos'
              }
            }

            resource mydatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
              name: 'mydatabase'
              location: location
              properties: {
                resource: {
                  id: 'mydatabase'
                }
              }
              parent: cosmos
            }

            resource mycontainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
              name: 'mycontainer'
              location: location
              properties: {
                resource: {
                  id: 'mycontainer'
                  partitionKey: {
                    paths: [
                      'mypartitionkeypath'
                    ]
                  }
                }
              }
              parent: mydatabase
            }

            output connectionString string = cosmos.properties.documentEndpoint

            output name string = cosmos.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        Assert.NotNull(callbackDatabases);
        Assert.Collection(
            callbackDatabases,
            (database) => Assert.Equal("mydatabase", database.Name.Value)
            );

        var connectionStringResource = (IResourceWithConnectionString)cosmos.Resource;

        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("mycosmosconnectionstring", await connectionStringResource.GetConnectionStringAsync());
    }

    [Theory]
    [InlineData("mykeyvault")]
    [InlineData(null)]
    public async Task AddAzureCosmosDBViaPublishMode_WithAccessKeyAuthentication(string? kvName)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        IEnumerable<CosmosDBSqlDatabase>? callbackDatabases = null;
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .ConfigureInfrastructure(infrastructure =>
            {
                callbackDatabases = infrastructure.GetProvisionableResources().OfType<CosmosDBSqlDatabase>();
            });

        if (kvName is null)
        {
            kvName = "cosmos-kv";
            cosmos.WithAccessKeyAuthentication();
        }
        else
        {
            cosmos.WithAccessKeyAuthentication(builder.AddAzureKeyVault(kvName));
        }

        var db = cosmos.AddCosmosDatabase("mydatabase");
        db.AddContainer("mycontainer", "mypartitionkeypath");

        var kv = builder.CreateResourceBuilder<AzureKeyVaultResource>(kvName);

        var secrets = new Dictionary<string, string>
        {
            ["connectionstrings--cosmos"] = "mycosmosconnectionstring"
        };

        kv.Resource.SecretResolver = (secretRef, _) =>
        {
            if (!secrets.TryGetValue(secretRef.SecretName, out var value))
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(value);
        };

        var manifest = await AzureManifestUtils.GetManifestWithBicep(cosmos.Resource);

        var expectedManifest = $$"""
                               {
                                 "type": "azure.bicep.v0",
                                 "connectionString": "{{{kvName}}.secrets.connectionstrings--cosmos}",
                                 "path": "cosmos.module.bicep",
                                 "params": {
                                   "keyVaultName": "{{{kvName}}.outputs.name}"
                                 }
                               }
                               """;

        var m = manifest.ManifestNode.ToString();

        output.WriteLine(m);
        Assert.Equal(expectedManifest, m);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyVaultName string

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
              name: take('cosmos-${uniqueString(resourceGroup().id)}', 44)
              location: location
              properties: {
                locations: [
                  {
                    locationName: location
                    failoverPriority: 0
                  }
                ]
                consistencyPolicy: {
                  defaultConsistencyLevel: 'Session'
                }
                databaseAccountOfferType: 'Standard'
                disableLocalAuth: false
              }
              kind: 'GlobalDocumentDB'
              tags: {
                'aspire-resource-name': 'cosmos'
              }
            }

            resource mydatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
              name: 'mydatabase'
              location: location
              properties: {
                resource: {
                  id: 'mydatabase'
                }
              }
              parent: cosmos
            }

            resource mycontainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
              name: 'mycontainer'
              location: location
              properties: {
                resource: {
                  id: 'mycontainer'
                  partitionKey: {
                    paths: [
                      'mypartitionkeypath'
                    ]
                  }
                }
              }
              parent: mydatabase
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

            resource mydatabase_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--mydatabase'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydatabase'
              }
              parent: keyVault
            }

            resource mycontainer_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--mycontainer'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydatabase;Container=mycontainer'
              }
              parent: keyVault
            }

            output name string = cosmos.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        Assert.NotNull(callbackDatabases);
        Assert.Collection(
            callbackDatabases,
            (database) => Assert.Equal("mydatabase", database.Name.Value)
            );

        var connectionStringResource = (IResourceWithConnectionString)cosmos.Resource;

        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("mycosmosconnectionstring", await connectionStringResource.GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureCosmosDBViaPublishMode_NoAccessKeyAuthentication()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        IEnumerable<CosmosDBSqlDatabase>? callbackDatabases = null;
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .ConfigureInfrastructure(infrastructure =>
            {
                callbackDatabases = infrastructure.GetProvisionableResources().OfType<CosmosDBSqlDatabase>();
            });
        var db = cosmos.AddCosmosDatabase("mydatabase");
        db.AddContainer("mycontainer", "mypartitionkeypath");

        cosmos.Resource.Outputs["connectionString"] = "mycosmosconnectionstring";

        var manifest = await AzureManifestUtils.GetManifestWithBicep(cosmos.Resource);

        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "connectionString": "{cosmos.outputs.connectionString}",
                                 "path": "cosmos.module.bicep"
                               }
                               """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
              name: take('cosmos-${uniqueString(resourceGroup().id)}', 44)
              location: location
              properties: {
                locations: [
                  {
                    locationName: location
                    failoverPriority: 0
                  }
                ]
                consistencyPolicy: {
                  defaultConsistencyLevel: 'Session'
                }
                databaseAccountOfferType: 'Standard'
                disableLocalAuth: true
              }
              kind: 'GlobalDocumentDB'
              tags: {
                'aspire-resource-name': 'cosmos'
              }
            }

            resource mydatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
              name: 'mydatabase'
              location: location
              properties: {
                resource: {
                  id: 'mydatabase'
                }
              }
              parent: cosmos
            }

            resource mycontainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
              name: 'mycontainer'
              location: location
              properties: {
                resource: {
                  id: 'mycontainer'
                  partitionKey: {
                    paths: [
                      'mypartitionkeypath'
                    ]
                  }
                }
              }
              parent: mydatabase
            }

            output connectionString string = cosmos.properties.documentEndpoint

            output name string = cosmos.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        Assert.NotNull(callbackDatabases);
        Assert.Collection(
            callbackDatabases,
            (database) => Assert.Equal("mydatabase", database.Name.Value)
            );

        var connectionStringResource = (IResourceWithConnectionString)cosmos.Resource;

        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("mycosmosconnectionstring", await connectionStringResource.GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureAppConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var appConfig = builder.AddAzureAppConfiguration("appConfig");
        appConfig.Resource.Outputs["appConfigEndpoint"] = "https://myendpoint";
        Assert.Equal("https://myendpoint", await appConfig.Resource.ConnectionStringExpression.GetValueAsync(default));

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, appConfig.Resource);

        var connectionStringResource = (IResourceWithConnectionString)appConfig.Resource;

        Assert.Equal("https://myendpoint", await connectionStringResource.GetConnectionStringAsync());

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{appConfig.outputs.appConfigEndpoint}",
              "path": "appConfig.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' = {
              name: take('appConfig-${uniqueString(resourceGroup().id)}', 50)
              location: location
              properties: {
                disableLocalAuth: true
              }
              sku: {
                name: 'standard'
              }
              tags: {
                'aspire-resource-name': 'appConfig'
              }
            }

            output appConfigEndpoint string = appConfig.properties.endpoint

            output name string = appConfig.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var appConfigRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "appConfig-roles");
        var appConfigRolesManifest = await GetManifestWithBicep(appConfigRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param appconfig_outputs_name string

            param principalType string

            param principalId string

            resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
              name: appconfig_outputs_name
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
            """;
        output.WriteLine(appConfigRolesManifest.BicepText);
        Assert.Equal(expectedBicep, appConfigRolesManifest.BicepText);
    }

    [Fact]
    public async Task AddApplicationInsightsWithoutExplicitLawGetsDefaultLawParameterInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var appInsights = builder.AddAzureApplicationInsights("appInsights");

        appInsights.Resource.Outputs["appInsightsConnectionString"] = "myinstrumentationkey";

        var connectionStringResource = (IResourceWithConnectionString)appInsights.Resource;

        Assert.Equal("appInsights", appInsights.Resource.Name);
        Assert.Equal("myinstrumentationkey", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{appInsights.outputs.appInsightsConnectionString}", appInsights.Resource.ConnectionStringExpression.ValueExpression);

        var appInsightsManifest = await AzureManifestUtils.GetManifestWithBicep(appInsights.Resource);
        var expectedManifest = """
           {
             "type": "azure.bicep.v0",
             "connectionString": "{appInsights.outputs.appInsightsConnectionString}",
             "path": "appInsights.module.bicep",
             "params": {
               "logAnalyticsWorkspaceId": ""
             }
           }
           """;
        Assert.Equal(expectedManifest, appInsightsManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param applicationType string = 'web'

            param kind string = 'web'

            param logAnalyticsWorkspaceId string

            resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
              name: take('appInsights-${uniqueString(resourceGroup().id)}', 260)
              kind: kind
              location: location
              properties: {
                Application_Type: applicationType
                WorkspaceResourceId: logAnalyticsWorkspaceId
              }
              tags: {
                'aspire-resource-name': 'appInsights'
              }
            }

            output appInsightsConnectionString string = appInsights.properties.ConnectionString
            """;
        output.WriteLine(appInsightsManifest.BicepText);
        Assert.Equal(expectedBicep, appInsightsManifest.BicepText);
    }

    [Fact]
    public async Task AddApplicationInsightsWithoutExplicitLawGetsDefaultLawParameterInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var appInsights = builder.AddAzureApplicationInsights("appInsights");

        appInsights.Resource.Outputs["appInsightsConnectionString"] = "myinstrumentationkey";

        var connectionStringResource = (IResourceWithConnectionString)appInsights.Resource;

        Assert.Equal("appInsights", appInsights.Resource.Name);
        Assert.Equal("myinstrumentationkey", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{appInsights.outputs.appInsightsConnectionString}", appInsights.Resource.ConnectionStringExpression.ValueExpression);

        var appInsightsManifest = await AzureManifestUtils.GetManifestWithBicep(appInsights.Resource);
        var expectedManifest = """
           {
             "type": "azure.bicep.v0",
             "connectionString": "{appInsights.outputs.appInsightsConnectionString}",
             "path": "appInsights.module.bicep"
           }
           """;
        Assert.Equal(expectedManifest, appInsightsManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param applicationType string = 'web'

            param kind string = 'web'

            resource law_appInsights 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
              name: take('lawappInsights-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                sku: {
                  name: 'PerGB2018'
                }
              }
              tags: {
                'aspire-resource-name': 'law_appInsights'
              }
            }

            resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
              name: take('appInsights-${uniqueString(resourceGroup().id)}', 260)
              kind: kind
              location: location
              properties: {
                Application_Type: applicationType
                WorkspaceResourceId: law_appInsights.id
              }
              tags: {
                'aspire-resource-name': 'appInsights'
              }
            }

            output appInsightsConnectionString string = appInsights.properties.ConnectionString
            """;
        output.WriteLine(appInsightsManifest.BicepText);
        Assert.Equal(expectedBicep, appInsightsManifest.BicepText);
    }

    [Fact]
    public async Task AddApplicationInsightsWithExplicitLawArgumentDoesntGetDefaultParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var law = builder.AddAzureLogAnalyticsWorkspace("mylaw");
        var appInsights = builder.AddAzureApplicationInsights("appInsights", law);

        appInsights.Resource.Outputs["appInsightsConnectionString"] = "myinstrumentationkey";

        var connectionStringResource = (IResourceWithConnectionString)appInsights.Resource;

        Assert.Equal("appInsights", appInsights.Resource.Name);
        Assert.Equal("myinstrumentationkey", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{appInsights.outputs.appInsightsConnectionString}", appInsights.Resource.ConnectionStringExpression.ValueExpression);

        var appInsightsManifest = await AzureManifestUtils.GetManifestWithBicep(appInsights.Resource);
        var expectedManifest = """
           {
             "type": "azure.bicep.v0",
             "connectionString": "{appInsights.outputs.appInsightsConnectionString}",
             "path": "appInsights.module.bicep",
             "params": {
               "logAnalyticsWorkspaceId": "{mylaw.outputs.logAnalyticsWorkspaceId}"
             }
           }
           """;
        Assert.Equal(expectedManifest, appInsightsManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param applicationType string = 'web'

            param kind string = 'web'

            param logAnalyticsWorkspaceId string

            resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
              name: take('appInsights-${uniqueString(resourceGroup().id)}', 260)
              kind: kind
              location: location
              properties: {
                Application_Type: applicationType
                WorkspaceResourceId: logAnalyticsWorkspaceId
              }
              tags: {
                'aspire-resource-name': 'appInsights'
              }
            }

            output appInsightsConnectionString string = appInsights.properties.ConnectionString
            """;
        output.WriteLine(appInsightsManifest.BicepText);
        Assert.Equal(expectedBicep, appInsightsManifest.BicepText);
    }

    [Fact]
    public async Task AddLogAnalyticsWorkspace()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var logAnalyticsWorkspace = builder.AddAzureLogAnalyticsWorkspace("logAnalyticsWorkspace");

        Assert.Equal("logAnalyticsWorkspace", logAnalyticsWorkspace.Resource.Name);
        Assert.Equal("{logAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId}", logAnalyticsWorkspace.Resource.WorkspaceId.ValueExpression);

        var appInsightsManifest = await AzureManifestUtils.GetManifestWithBicep(logAnalyticsWorkspace.Resource);
        var expectedManifest = """
           {
             "type": "azure.bicep.v0",
             "path": "logAnalyticsWorkspace.module.bicep"
           }
           """;
        Assert.Equal(expectedManifest, appInsightsManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
              name: take('logAnalyticsWorkspace-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                sku: {
                  name: 'PerGB2018'
                }
              }
              tags: {
                'aspire-resource-name': 'logAnalyticsWorkspace'
              }
            }

            output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
            """;
        output.WriteLine(appInsightsManifest.BicepText);
        Assert.Equal(expectedBicep, appInsightsManifest.BicepText);
    }

    [Fact]
    public async Task WithReferenceAppInsightsSetsEnvironmentVariable()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var appInsights = builder.AddAzureApplicationInsights("ai");

        appInsights.Resource.Outputs["appInsightsConnectionString"] = "myinstrumentationkey";

        var serviceA = builder.AddProject<ProjectA>("serviceA", o => o.ExcludeLaunchProfile = true)
            .WithReference(appInsights);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(serviceA.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.True(config.ContainsKey("APPLICATIONINSIGHTS_CONNECTION_STRING"));
        Assert.Equal("myinstrumentationkey", config["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
    }

    [Fact]
    public async Task AddAzureInfrastructureGeneratesCorrectManifestEntry()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var infrastructure1 = builder.AddAzureInfrastructure("infrastructure1", (infrastructure) =>
        {
            var storage = new StorageAccount("storage")
            {
                Kind = StorageKind.StorageV2,
                Sku = new StorageSku() { Name = StorageSkuName.StandardLrs }
            };
            infrastructure.Add(storage);
            infrastructure.Add(new ProvisioningOutput("storageAccountName", typeof(string)) { Value = storage.Name });
        });

        var manifest = await ManifestUtils.GetManifest(infrastructure1.Resource);
        Assert.Equal("azure.bicep.v0", manifest["type"]?.ToString());
        Assert.Equal("infrastructure1.module.bicep", manifest["path"]?.ToString());
    }

    [Fact]
    public async Task AssignParameterPopulatesParametersEverywhere()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:skuName"] = "Standard_ZRS";

        var skuName = builder.AddParameter("skuName");

        AzureResourceInfrastructure? moduleInfrastructure = null;
        var infrastructure1 = builder.AddAzureInfrastructure("infrastructure1", (infrastructure) =>
        {
            var storage = new StorageAccount("storage")
            {
                Kind = StorageKind.StorageV2,
                Sku = new StorageSku() { Name = skuName.AsProvisioningParameter(infrastructure) }
            };
            infrastructure.Add(storage);
            moduleInfrastructure = infrastructure;
        });

        var manifest = await ManifestUtils.GetManifest(infrastructure1.Resource);

        Assert.NotNull(moduleInfrastructure);
        var infrastructureParameters = moduleInfrastructure.GetParameters().DistinctBy(x => x.BicepIdentifier);
        var infrastructureParametersLookup = infrastructureParameters.ToDictionary(p => p.BicepIdentifier);
        Assert.True(infrastructureParametersLookup.ContainsKey("skuName"));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "infrastructure1.module.bicep",
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
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:skuName"] = "Standard_ZRS";

        var skuName = builder.AddParameter("skuName");

        AzureResourceInfrastructure? moduleInfrastructure = null;
        var infrastructure1 = builder.AddAzureInfrastructure("infrastructure1", (infrastructure) =>
        {
            var storage = new StorageAccount("storage")
            {
                Kind = StorageKind.StorageV2,
                Sku = new StorageSku() { Name = skuName.AsProvisioningParameter(infrastructure, parameterName: "sku") }
            };
            infrastructure.Add(storage);
            moduleInfrastructure = infrastructure;
        });

        var manifest = await ManifestUtils.GetManifest(infrastructure1.Resource);

        Assert.NotNull(moduleInfrastructure);
        var infrastructureParameters = moduleInfrastructure.GetParameters().DistinctBy(x => x.BicepIdentifier);
        var infrastructureParametersLookup = infrastructureParameters.ToDictionary(p => p.BicepIdentifier);
        Assert.True(infrastructureParametersLookup.ContainsKey("sku"));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "infrastructure1.module.bicep",
              "params": {
                "sku": "{skuName.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task PublishAsRedisPublishesRedisAsAzureRedisInfrastructure()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

#pragma warning disable CS0618 // Type or member is obsolete
        var redis = builder.AddRedis("cache")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
            .PublishAsAzureRedis();
#pragma warning restore CS0618 // Type or member is obsolete

        Assert.True(redis.Resource.IsContainer());
        Assert.NotNull(redis.Resource.PasswordParameter);

        Assert.Equal($"localhost:12455,password={redis.Resource.PasswordParameter.Value}", await redis.Resource.GetConnectionStringAsync());

        var manifest = await AzureManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{cache.secretOutputs.connectionString}",
              "path": "cache.module.bicep",
              "params": {
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyVaultName string

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

            resource cache 'Microsoft.Cache/redis@2024-03-01' = {
              name: take('cache-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                sku: {
                  name: 'Basic'
                  family: 'C'
                  capacity: 1
                }
                enableNonSslPort: false
                minimumTlsVersion: '1.2'
              }
              tags: {
                'aspire-resource-name': 'cache'
              }
            }

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionString'
              properties: {
                value: '${cache.properties.hostName},ssl=true,password=${cache.listKeys().primaryKey}'
              }
              parent: keyVault
            }
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AsAzureSqlDatabaseViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

#pragma warning disable CS0618 // Type or member is obsolete
        var sql = builder.AddSqlServer("sql").AsAzureSqlDatabase();
#pragma warning restore CS0618 // Type or member is obsolete
        sql.AddDatabase("db", "dbName");

        var manifest = await AzureManifestUtils.GetManifestWithBicep(sql.Resource);

        Assert.True(sql.Resource.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation));
        var azureSql = (AzureSqlServerResource)connectionStringAnnotation.Resource;
        azureSql.Outputs["sqlServerFqdn"] = "myserver";

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
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = $$"""
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalName string

            param principalType string

            resource sql 'Microsoft.Sql/servers@2021-11-01' = {
              name: take('sql-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                administrators: {
                  administratorType: 'ActiveDirectory'
                  principalType: principalType
                  login: principalName
                  sid: principalId
                  tenantId: subscription().tenantId
                  azureADOnlyAuthentication: true
                }
                minimalTlsVersion: '1.2'
                publicNetworkAccess: 'Enabled'
                version: '12.0'
              }
              tags: {
                'aspire-resource-name': 'sql'
              }
            }

            resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: sql
            }

            resource sqlFirewallRule_AllowAllIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllIps'
              properties: {
                endIpAddress: '255.255.255.255'
                startIpAddress: '0.0.0.0'
              }
              parent: sql
            }

            resource db 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'dbName'
              location: location
              parent: sql
            }

            output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName

            output name string = sql.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AsAzureSqlDatabaseViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

#pragma warning disable CS0618 // Type or member is obsolete
        var sql = builder.AddSqlServer("sql").AsAzureSqlDatabase();
#pragma warning restore CS0618 // Type or member is obsolete
        sql.AddDatabase("db", "dbName");

        var manifest = await AzureManifestUtils.GetManifestWithBicep(sql.Resource);

        Assert.True(sql.Resource.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation));
        var azureSql = (AzureSqlServerResource)connectionStringAnnotation.Resource;
        azureSql.Outputs["sqlServerFqdn"] = "myserver";

        Assert.Equal("Server=tcp:myserver,1433;Encrypt=True;Authentication=\"Active Directory Default\"", await sql.Resource.GetConnectionStringAsync(default));
        Assert.Equal("Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.ConnectionStringExpression.ValueExpression);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sql.module.bicep",
              "params": {
                "principalId": "",
                "principalName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = $$"""
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalName string

            resource sql 'Microsoft.Sql/servers@2021-11-01' = {
              name: take('sql-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                administrators: {
                  administratorType: 'ActiveDirectory'
                  login: principalName
                  sid: principalId
                  tenantId: subscription().tenantId
                  azureADOnlyAuthentication: true
                }
                minimalTlsVersion: '1.2'
                publicNetworkAccess: 'Enabled'
                version: '12.0'
              }
              tags: {
                'aspire-resource-name': 'sql'
              }
            }

            resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: sql
            }

            resource db 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'dbName'
              location: location
              parent: sql
            }

            output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName

            output name string = sql.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AsAzurePostgresFlexibleServerViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["Parameters:usr"] = "user";
        builder.Configuration["Parameters:pwd"] = "password";

        var usr = builder.AddParameter("usr");
        var pwd = builder.AddParameter("pwd", secret: true);

#pragma warning disable CS0618 // Type or member is obsolete
        var postgres = builder.AddPostgres("postgres", usr, pwd).AsAzurePostgresFlexibleServer();
        postgres.AddDatabase("db", "dbName");

        Assert.True(postgres.Resource.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation));
        var azurePostgres = (AzurePostgresResource)connectionStringAnnotation.Resource;
#pragma warning restore CS0618 // Type or member is obsolete

        var manifest = await AzureManifestUtils.GetManifestWithBicep(postgres.Resource);

        // Setup to verify that connection strings is acquired via resource connectionstring redirct.
        Assert.NotNull(azurePostgres);
        azurePostgres.SecretOutputs["connectionString"] = "myconnectionstring";
        Assert.Equal("myconnectionstring", await postgres.Resource.GetConnectionStringAsync(default));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres.secretOutputs.connectionString}",
              "path": "postgres.module.bicep",
              "params": {
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}",
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param administratorLogin string

            @secure()
            param administratorLoginPassword string

            param keyVaultName string

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

            resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
              name: take('postgres-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                administratorLogin: administratorLogin
                administratorLoginPassword: administratorLoginPassword
                availabilityZone: '1'
                backup: {
                  backupRetentionDays: 7
                  geoRedundantBackup: 'Disabled'
                }
                highAvailability: {
                  mode: 'Disabled'
                }
                storage: {
                  storageSizeGB: 32
                }
                version: '16'
              }
              sku: {
                name: 'Standard_B1ms'
                tier: 'Burstable'
              }
              tags: {
                'aspire-resource-name': 'postgres'
              }
            }

            resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: postgres
            }

            resource postgreSqlFirewallRule_AllowAllIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllIps'
              properties: {
                endIpAddress: '255.255.255.255'
                startIpAddress: '0.0.0.0'
              }
              parent: postgres
            }

            resource db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
              name: 'dbName'
              parent: postgres
            }

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionString'
              properties: {
                value: 'Host=${postgres.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
              }
              parent: keyVault
            }
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AsAzurePostgresFlexibleServerViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.Configuration["Parameters:usr"] = "user";
        builder.Configuration["Parameters:pwd"] = "password";

        var usr = builder.AddParameter("usr");
        var pwd = builder.AddParameter("pwd", secret: true);

#pragma warning disable CS0618 // Type or member is obsolete
        var postgres = builder.AddPostgres("postgres", usr, pwd).AsAzurePostgresFlexibleServer();
        postgres.AddDatabase("db", "dbName");

        Assert.True(postgres.Resource.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation));
        var azurePostgres = (AzurePostgresResource)connectionStringAnnotation.Resource;
#pragma warning restore CS0618 // Type or member is obsolete

        var manifest = await AzureManifestUtils.GetManifestWithBicep(postgres.Resource);

        // Setup to verify that connection strings is acquired via resource connectionstring redirct.
        Assert.NotNull(azurePostgres);
        azurePostgres.SecretOutputs["connectionString"] = "myconnectionstring";
        Assert.Equal("myconnectionstring", await postgres.Resource.GetConnectionStringAsync(default));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres.secretOutputs.connectionString}",
              "path": "postgres.module.bicep",
              "params": {
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}",
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param administratorLogin string

            @secure()
            param administratorLoginPassword string

            param keyVaultName string

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

            resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
              name: take('postgres-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                administratorLogin: administratorLogin
                administratorLoginPassword: administratorLoginPassword
                availabilityZone: '1'
                backup: {
                  backupRetentionDays: 7
                  geoRedundantBackup: 'Disabled'
                }
                highAvailability: {
                  mode: 'Disabled'
                }
                storage: {
                  storageSizeGB: 32
                }
                version: '16'
              }
              sku: {
                name: 'Standard_B1ms'
                tier: 'Burstable'
              }
              tags: {
                'aspire-resource-name': 'postgres'
              }
            }

            resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: postgres
            }

            resource db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
              name: 'dbName'
              parent: postgres
            }

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionString'
              properties: {
                value: 'Host=${postgres.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
              }
              parent: keyVault
            }
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task PublishAsAzurePostgresFlexibleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["Parameters:usr"] = "user";
        builder.Configuration["Parameters:pwd"] = "password";

        var usr = builder.AddParameter("usr");
        var pwd = builder.AddParameter("pwd", secret: true);

#pragma warning disable CS0618 // Type or member is obsolete
        var postgres = builder.AddPostgres("postgres", usr, pwd).PublishAsAzurePostgresFlexibleServer();
        postgres.AddDatabase("db");
#pragma warning restore CS0618 // Type or member is obsolete

        var manifest = await AzureManifestUtils.GetManifestWithBicep(postgres.Resource);

        // Verify that when PublishAs variant is used, connection string acquisition
        // still uses the local endpoint.
        postgres.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 1234));
        var expectedConnectionString = $"Host=localhost;Port=1234;Username=user;Password=password";
        Assert.Equal(expectedConnectionString, await postgres.Resource.GetConnectionStringAsync());

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres.secretOutputs.connectionString}",
              "path": "postgres.module.bicep",
              "params": {
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}",
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());
    }

    [Fact]
    public async Task PublishAsAzurePostgresFlexibleServerNoUserPassParams()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

#pragma warning disable CS0618 // Type or member is obsolete
        var postgres = builder.AddPostgres("postgres1")
            .PublishAsAzurePostgresFlexibleServer(); // Because of InternalsVisibleTo

        var manifest = await ManifestUtils.GetManifest(postgres.Resource);
        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres1.secretOutputs.connectionString}",
              "path": "postgres1.module.bicep",
              "params": {
                "administratorLogin": "{postgres1-username.value}",
                "administratorLoginPassword": "{postgres1-password.value}",
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        var param = builder.AddParameter("param");

        postgres = builder.AddPostgres("postgres2", userName: param)
            .PublishAsAzurePostgresFlexibleServer();

        manifest = await ManifestUtils.GetManifest(postgres.Resource);
        expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres2.secretOutputs.connectionString}",
              "path": "postgres2.module.bicep",
              "params": {
                "administratorLogin": "{param.value}",
                "administratorLoginPassword": "{postgres2-password.value}",
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        postgres = builder.AddPostgres("postgres3", password: param)
            .PublishAsAzurePostgresFlexibleServer();
#pragma warning restore CS0618 // Type or member is obsolete

        manifest = await ManifestUtils.GetManifest(postgres.Resource);
        expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres3.secretOutputs.connectionString}",
              "path": "postgres3.module.bicep",
              "params": {
                "administratorLogin": "{postgres3-username.value}",
                "administratorLoginPassword": "{param.value}",
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzureServiceBus(bool useObsoleteMethods)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

        if (useObsoleteMethods)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            serviceBus
                .AddQueue("queue1")
                .AddQueue("queue2")
                .AddTopic("t1")
                .AddTopic("t2")
                .AddSubscription("t1", "s3");
#pragma warning restore CS0618 // Type or member is obsolete
        }
        else
        {
            serviceBus.AddServiceBusQueue("queue1");
            serviceBus.AddServiceBusQueue("queue2");
            serviceBus.AddServiceBusTopic("t1")
                .AddServiceBusSubscription("s3");
            serviceBus.AddServiceBusTopic("t2");
        }

        serviceBus.Resource.Outputs["serviceBusEndpoint"] = "mynamespaceEndpoint";

        var connectionStringResource = (IResourceWithConnectionString)serviceBus.Resource;

        Assert.Equal("sb", serviceBus.Resource.Name);
        Assert.Equal("mynamespaceEndpoint", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", connectionStringResource.ConnectionStringExpression.ValueExpression);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, serviceBus.Resource);

        var expected = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{sb.outputs.serviceBusEndpoint}",
              "path": "sb.module.bicep"
            }
            """;
        Assert.Equal(expected, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
              name: take('sb-${uniqueString(resourceGroup().id)}', 50)
              location: location
              properties: {
                disableLocalAuth: true
              }
              sku: {
                name: sku
              }
              tags: {
                'aspire-resource-name': 'sb'
              }
            }

            resource queue1 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue1'
              parent: sb
            }

            resource queue2 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queue2'
              parent: sb
            }

            resource t1 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
              name: 't1'
              parent: sb
            }

            resource s3 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
              name: 's3'
              parent: t1
            }

            resource t2 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
              name: 't2'
              parent: sb
            }

            output serviceBusEndpoint string = sb.properties.serviceBusEndpoint

            output name string = sb.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var sbRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "sb-roles");
        var sbRolesManifest = await GetManifestWithBicep(sbRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sb_outputs_name string

            param principalType string

            param principalId string

            resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
              name: sb_outputs_name
            }

            resource sb_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: sb
            }
            """;
        output.WriteLine(sbRolesManifest.BicepText);
        Assert.Equal(expectedBicep, sbRolesManifest.BicepText);
    }

    [Fact]
    public async Task AddDefaultAzureWebPubSub()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var wps = builder.AddAzureWebPubSub("wps1");

        wps.Resource.Outputs["endpoint"] = "https://mywebpubsubendpoint";

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{wps1.outputs.endpoint}",
              "path": "wps1.module.bicep"
            }
            """;

        var connectionStringResource = (IResourceWithConnectionString)wps.Resource;

        Assert.Equal("https://mywebpubsubendpoint", await connectionStringResource.GetConnectionStringAsync());

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, wps.Resource);

        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        Assert.Equal("wps1", wps.Resource.Name);
        output.WriteLine(manifest.BicepText);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
              name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
              location: location
              sku: {
                name: sku
                capacity: capacity
              }
              tags: {
                'aspire-resource-name': 'wps1'
              }
            }

            output endpoint string = 'https://${wps1.properties.hostName}'

            output name string = wps1.name
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);

        var wpsRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "wps1-roles");
        var wpsRolesManifest = await GetManifestWithBicep(wpsRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param wps1_outputs_name string

            param principalType string

            param principalId string

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' existing = {
              name: wps1_outputs_name
            }

            resource wps1_WebPubSubServiceOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(wps1.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
                principalType: principalType
              }
              scope: wps1
            }
            """;
        output.WriteLine(wpsRolesManifest.BicepText);
        Assert.Equal(expectedBicep, wpsRolesManifest.BicepText);
    }

    [Fact]
    public async Task AddAzureWebPubSubWithParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var wps = builder.AddAzureWebPubSub("wps1")
        .WithParameter("sku", "Standard_S1")
        .WithParameter("capacity", 2);

        wps.Resource.Outputs["endpoint"] = "https://mywebpubsubendpoint";

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{wps1.outputs.endpoint}",
              "path": "wps1.module.bicep",
              "params": {
                "sku": "Standard_S1",
                "capacity": 2
              }
            }
            """;
        var manifest = await AzureManifestUtils.GetManifestWithBicep(wps.Resource);
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        Assert.Equal("wps1", wps.Resource.Name);
        output.WriteLine(manifest.BicepText);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
              name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
              location: location
              sku: {
                name: sku
                capacity: capacity
              }
              tags: {
                'aspire-resource-name': 'wps1'
              }
            }

            output endpoint string = 'https://${wps1.properties.hostName}'

            output name string = wps1.name
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureStorageEmulator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage").RunAsEmulator(e =>
        {
            e.WithEndpoint("blob", e => e.AllocatedEndpoint = new(e, "localhost", 10000));
            e.WithEndpoint("queue", e => e.AllocatedEndpoint = new(e, "localhost", 10001));
            e.WithEndpoint("table", e => e.AllocatedEndpoint = new(e, "localhost", 10002));
        });

        Assert.True(storage.Resource.IsContainer());

        var blob = storage.AddBlobs("blob");
        var queue = storage.AddQueues("queue");
        var table = storage.AddTables("table");

        EndpointReference GetEndpointReference(string name, int port)
            => new(storage.Resource, new EndpointAnnotation(ProtocolType.Tcp, name: name, targetPort: port));

        var blobqs = AzureStorageEmulatorConnectionString.Create(blobEndpoint: GetEndpointReference("blob", 10000)).ValueExpression;
        var queueqs = AzureStorageEmulatorConnectionString.Create(queueEndpoint: GetEndpointReference("queue", 10001)).ValueExpression;
        var tableqs = AzureStorageEmulatorConnectionString.Create(tableEndpoint: GetEndpointReference("table", 10002)).ValueExpression;

        Assert.Equal(blobqs, blob.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(queueqs, queue.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(tableqs, table.Resource.ConnectionStringExpression.ValueExpression);

        string Resolve(string? qs, string name, int port) =>
            qs!.Replace("{storage.bindings." + name + ".host}", "127.0.0.1")
               .Replace("{storage.bindings." + name + ".port}", port.ToString());

        Assert.Equal(Resolve(blobqs, "blob", 10000), await ((IResourceWithConnectionString)blob.Resource).GetConnectionStringAsync());
        Assert.Equal(Resolve(queueqs, "queue", 10001), await ((IResourceWithConnectionString)queue.Resource).GetConnectionStringAsync());
        Assert.Equal(Resolve(tableqs, "table", 10002), await ((IResourceWithConnectionString)table.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureStorageViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
            });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        var storageManifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;
        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
              name: take('storage${uniqueString(resourceGroup().id)}', 24)
              kind: 'StorageV2'
              location: location
              sku: {
                name: storagesku
              }
              properties: {
                accessTier: 'Hot'
                allowSharedKeyAccess: false
                minimumTlsVersion: 'TLS1_2'
                networkAcls: {
                  defaultAction: 'Allow'
                }
              }
              tags: {
                'aspire-resource-name': 'storage'
              }
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
        output.WriteLine(storageManifest.BicepText);
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
    public async Task AddAzureStorageViaRunModeAllowSharedKeyAccessOverridesDefaultFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
                sa.AllowSharedKeyAccess = true;
            });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        var storageManifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;
        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
              name: take('storage${uniqueString(resourceGroup().id)}', 24)
              kind: 'StorageV2'
              location: location
              sku: {
                name: storagesku
              }
              properties: {
                accessTier: 'Hot'
                allowSharedKeyAccess: true
                minimumTlsVersion: 'TLS1_2'
                networkAcls: {
                  defaultAction: 'Allow'
                }
              }
              tags: {
                'aspire-resource-name': 'storage'
              }
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
        output.WriteLine(storageManifest.BicepText);
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
    public async Task AddAzureStorageViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
            });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var storageManifest = await GetManifestWithBicep(model, storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;
        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
              name: take('storage${uniqueString(resourceGroup().id)}', 24)
              kind: 'StorageV2'
              location: location
              sku: {
                name: storagesku
              }
              properties: {
                accessTier: 'Hot'
                allowSharedKeyAccess: false
                minimumTlsVersion: 'TLS1_2'
                networkAcls: {
                  defaultAction: 'Allow'
                }
              }
              tags: {
                'aspire-resource-name': 'storage'
              }
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
        output.WriteLine(storageManifest.BicepText);
        Assert.Equal(expectedBicep, storageManifest.BicepText);

        var storageRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "storage-roles");
        var storageRolesManifest = await GetManifestWithBicep(storageRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storage_outputs_name string

            param principalType string

            param principalId string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
              name: storage_outputs_name
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
            """;
        output.WriteLine(storageRolesManifest.BicepText);
        Assert.Equal(expectedBicep, storageRolesManifest.BicepText);

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
    public async Task AddAzureStorageViaPublishModeEnableAllowSharedKeyAccessOverridesDefaultFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage")
            .ConfigureInfrastructure(infrastructure =>
            {
                var sa = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
                sa.Sku = new StorageSku()
                {
                    Name = storagesku.AsProvisioningParameter(infrastructure)
                };
                sa.AllowSharedKeyAccess = true;
            });

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        // Check storage resource.
        Assert.Equal("storage", storage.Resource.Name);

        var storageManifest = await AzureManifestUtils.GetManifestWithBicep(storage.Resource);

        var expectedStorageManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "storage.module.bicep",
              "params": {
                "storagesku": "{storagesku.value}"
              }
            }
            """;

        Assert.Equal(expectedStorageManifest, storageManifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
              name: take('storage${uniqueString(resourceGroup().id)}', 24)
              kind: 'StorageV2'
              location: location
              sku: {
                name: storagesku
              }
              properties: {
                accessTier: 'Hot'
                allowSharedKeyAccess: true
                minimumTlsVersion: 'TLS1_2'
                networkAcls: {
                  defaultAction: 'Allow'
                }
              }
              tags: {
                'aspire-resource-name': 'storage'
              }
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
        output.WriteLine(storageManifest.BicepText);
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
        using var builder = TestDistributedApplicationBuilder.Create();

        // Add search and parameterize the SKU
        var sku = builder.AddParameter("searchSku");
        var search = builder.AddAzureSearch("search")
            .ConfigureInfrastructure(infrastructure =>
            {
                var search = infrastructure.GetProvisionableResources().OfType<SearchService>().Single();
                search.SearchSkuName = sku.AsProvisioningParameter(infrastructure);
            });

        // Pretend we deployed it
        const string fakeConnectionString = "mysearchconnectionstring";
        search.Resource.Outputs["connectionString"] = fakeConnectionString;

        var connectionStringResource = (IResourceWithConnectionString)search.Resource;

        // Validate the resource
        Assert.Equal("search", search.Resource.Name);
        Assert.Equal("{search.outputs.connectionString}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(fakeConnectionString, await connectionStringResource.GetConnectionStringAsync());

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, search.Resource);

        // Validate the manifest
        var expectedManifest =
            """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{search.outputs.connectionString}",
              "path": "search.module.bicep",
              "params": {
                "searchSku": "{searchSku.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param searchSku string

            resource search 'Microsoft.Search/searchServices@2023-11-01' = {
              name: take('search-${uniqueString(resourceGroup().id)}', 60)
              location: location
              properties: {
                hostingMode: 'default'
                disableLocalAuth: true
                partitionCount: 1
                replicaCount: 1
              }
              sku: {
                name: searchSku
              }
              tags: {
                'aspire-resource-name': 'search'
              }
            }

            output connectionString string = 'Endpoint=https://${search.name}.search.windows.net'

            output name string = search.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var searchRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "search-roles");
        var searchRolesManifest = await GetManifestWithBicep(searchRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param search_outputs_name string

            param principalType string

            param principalId string

            resource search 'Microsoft.Search/searchServices@2023-11-01' existing = {
              name: search_outputs_name
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
            """;
        output.WriteLine(searchRolesManifest.BicepText);
        Assert.Equal(expectedBicep, searchRolesManifest.BicepText);
    }

    [Fact]
    public async Task PublishAsConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ai = builder.AddAzureApplicationInsights("ai").PublishAsConnectionString();
        var serviceBus = builder.AddAzureServiceBus("servicebus").PublishAsConnectionString();

        var serviceA = builder.AddProject<ProjectA>("serviceA", o => o.ExcludeLaunchProfile = true)
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AddAzureOpenAI(bool overrideLocalAuthDefault, bool useObsoleteApis)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        IEnumerable<CognitiveServicesAccountDeployment>? aiDeployments = null;
        var openai = builder.AddAzureOpenAI("openai")
            .ConfigureInfrastructure(infrastructure =>
            {
                aiDeployments = infrastructure.GetProvisionableResources().OfType<CognitiveServicesAccountDeployment>();

                if (overrideLocalAuthDefault)
                {
                    var account = infrastructure.GetProvisionableResources().OfType<CognitiveServicesAccount>().Single();
                    account.Properties.DisableLocalAuth = false;
                }
            });

        if (useObsoleteApis)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            openai.AddDeployment(new("mymodel", "gpt-35-turbo", "0613", "Basic", 4))
                .AddDeployment(new("embedding-model", "text-embedding-ada-002", "2", "Basic", 4));
#pragma warning restore CS0618 // Type or member is obsolete
        }
        else
        {
            openai.AddDeployment("mymodel", "gpt-35-turbo", "0613")
                .WithProperties(d =>
                {
                    d.SkuName = "Basic";
                    d.SkuCapacity = 4;
                });
            openai.AddDeployment("embedding-model", "text-embedding-ada-002", "2")
                .WithProperties(d =>
                {
                    d.SkuName = "Basic";
                    d.SkuCapacity = 4;
                });
        }

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, openai.Resource);

        Assert.NotNull(aiDeployments);
        Assert.Collection(
            aiDeployments,
            deployment => Assert.Equal("mymodel", deployment.Name.Value),
            deployment => Assert.Equal("embedding-model", deployment.Name.Value));

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{openai.outputs.connectionString}",
              "path": "openai.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = $$"""
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
              name: take('openai-${uniqueString(resourceGroup().id)}', 64)
              location: location
              kind: 'OpenAI'
              properties: {
                customSubDomainName: toLower(take(concat('openai', uniqueString(resourceGroup().id)), 24))
                publicNetworkAccess: 'Enabled'
                disableLocalAuth: {{(overrideLocalAuthDefault ? "false" : "true")}}
              }
              sku: {
                name: 'S0'
              }
              tags: {
                'aspire-resource-name': 'openai'
              }
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
              parent: openai
            }

            resource embedding_model 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
              name: 'embedding-model'
              properties: {
                model: {
                  format: 'OpenAI'
                  name: 'text-embedding-ada-002'
                  version: '2'
                }
              }
              sku: {
                name: 'Basic'
                capacity: 4
              }
              parent: openai
              dependsOn: [
                mymodel
              ]
            }

            output connectionString string = 'Endpoint=${openai.properties.endpoint}'

            output name string = openai.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var openaiRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "openai-roles");
        var openaiRolesManifest = await GetManifestWithBicep(openaiRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param openai_outputs_name string

            param principalType string

            param principalId string

            resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
              name: openai_outputs_name
            }

            resource openai_CognitiveServicesOpenAIContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(openai.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
                principalType: principalType
              }
              scope: openai
            }
            """;
        output.WriteLine(openaiRolesManifest.BicepText);
        Assert.Equal(expectedBicep, openaiRolesManifest.BicepText);
    }

    [Fact]
    public void ConfigureInfrastructureMustNotBeNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var provisioningResource = builder.AddAzureInfrastructure("infrastructure", r =>
        {
            r.Add(new KeyVaultService("kv"));
        });

        var ex = Assert.Throws<ArgumentNullException>(() => provisioningResource.ConfigureInfrastructure(null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public async Task InfrastructureCanBeMutatedAfterCreation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var provisioningResource = builder.AddAzureInfrastructure("infrastructure", r =>
        {
            r.Add(new KeyVaultService("kv")
            {
                Properties = new KeyVaultProperties()
                {
                    TenantId = BicepFunction.GetTenant().TenantId,
                    Sku = new KeyVaultSku()
                    {
                        Family = KeyVaultSkuFamily.A,
                        Name = KeyVaultSkuName.Standard
                    },
                    EnableRbacAuthorization = true
                }
            });
        })
        .ConfigureInfrastructure(r =>
        {
            var vault = r.GetProvisionableResources().OfType<KeyVaultService>().Single();
            Assert.NotNull(vault);

            r.Add(new ProvisioningOutput("vaultUri", typeof(string))
            {
                Value = vault.Properties.VaultUri
            });
        })
        .ConfigureInfrastructure(r =>
        {
            var vault = r.GetProvisionableResources().OfType<KeyVaultService>().Single();
            Assert.NotNull(vault);

            r.Add(new KeyVaultSecret("secret")
            {
                Parent = vault,
                Name = "kvs",
                Properties = new SecretProperties { Value = "00000000-0000-0000-0000-000000000000" }
            });
        });

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(provisioningResource.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "infrastructure.module.bicep"
            }
            """;

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
              name: take('kv-${uniqueString(resourceGroup().id)}', 24)
              location: location
              properties: {
                tenantId: tenant().tenantId
                sku: {
                  family: 'A'
                  name: 'standard'
                }
                enableRbacAuthorization: true
              }
            }

            resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'kvs'
              properties: {
                value: '00000000-0000-0000-0000-000000000000'
              }
              parent: kv
            }

            output vaultUri string = kv.properties.vaultUri
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
        Assert.Equal(expectedBicep, bicep);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";
    }
}
