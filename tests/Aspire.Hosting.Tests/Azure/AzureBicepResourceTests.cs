// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001 // Because we are testing CDK callbacks.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Storage.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests.Azure;

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

    public static TheoryData<Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>>> AzureExtensions
    {

        get
        {
            static void CreateConstruct(ResourceModuleConstruct construct)
            {
                var id = new UserAssignedIdentity(construct);
                id.AddOutput("cid", c => c.ClientId);
            }

            return new()
            {
                { builder => builder.AddAzureAppConfiguration("x") },
                { builder => builder.AddAzureApplicationInsights("x") },
                { builder => builder.AddBicepTemplate("x", "template.bicep") },
                { builder => builder.AddBicepTemplateString("x", "content") },
                { builder => builder.AddAzureConstruct("x", CreateConstruct) },
                { builder => builder.AddAzureOpenAI("x") },
                { builder => builder.AddAzureCosmosDB("x") },
                { builder => builder.AddAzureEventHubs("x") },
                { builder => builder.AddAzureKeyVault("x") },
                { builder => builder.AddAzureLogAnalyticsWorkspace("x") },
                { builder => builder.AddPostgres("x").AsAzurePostgresFlexibleServer() },
                { builder => builder.AddRedis("x").AsAzureRedis() },
                { builder => builder.AddAzureSearch("x") },
                { builder => builder.AddAzureServiceBus("x") },
                { builder => builder.AddAzureSignalR("x") },
                { builder => builder.AddSqlServer("x").AsAzureSqlDatabase() },
                { builder => builder.AddAzureStorage("x") },
            };
        }
    }

    [Theory]
    [MemberData(nameof(AzureExtensions))]
    public void AzureExtensionsAutomaticallyAddAzureProvisioning(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        addAzureResource(builder);

        var app = builder.Build();
        var hooks = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        Assert.Single(hooks.OfType<AzureProvisioner>());
    }

    [Theory]
    [MemberData(nameof(AzureExtensions))]
    public void BicepResourcesAreIdempotent(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        var azureResourceBuilder = addAzureResource(builder);

        if (azureResourceBuilder.Resource is not AzureConstructResource bicepResource)
        {
            // Skip
            return;
        }

        // This makes sure that these don't throw
        bicepResource.GetBicepTemplateFile();
        bicepResource.GetBicepTemplateFile();
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

        var cs = AzureCosmosDBEmulatorConnectionString.Create(10001);

        Assert.Equal(cs, cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(cs, await ((IResourceWithConnectionString)cosmos.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureCosmosDBViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

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

            resource cosmosDBAccount_MZyw35gqp 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
              name: toLower(take('cosmos${uniqueString(resourceGroup().id)}', 24))
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

            resource cosmosDBSqlDatabase_2kiHyuwCU 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
              parent: cosmosDBAccount_MZyw35gqp
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
                value: 'AccountEndpoint=${cosmosDBAccount_MZyw35gqp.properties.documentEndpoint};AccountKey=${cosmosDBAccount_MZyw35gqp.listkeys(cosmosDBAccount_MZyw35gqp.apiVersion).primaryMasterKey}'
              }
            }

            """;
        output.WriteLine(manifest.BicepText);
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
    public async Task AddAzureCosmosDBViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

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

            resource cosmosDBAccount_MZyw35gqp 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
              name: toLower(take('cosmos${uniqueString(resourceGroup().id)}', 24))
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

            resource cosmosDBSqlDatabase_2kiHyuwCU 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
              parent: cosmosDBAccount_MZyw35gqp
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
                value: 'AccountEndpoint=${cosmosDBAccount_MZyw35gqp.properties.documentEndpoint};AccountKey=${cosmosDBAccount_MZyw35gqp.listkeys(cosmosDBAccount_MZyw35gqp.apiVersion).primaryMasterKey}'
              }
            }

            """;
        output.WriteLine(manifest.BicepText);
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
        using var builder = TestDistributedApplicationBuilder.Create();

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


            resource appConfigurationStore_xM7mBhesj 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
              name: toLower(take('appConfig${uniqueString(resourceGroup().id)}', 24))
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

            resource roleAssignment_3uatMWw7h 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: appConfigurationStore_xM7mBhesj
              name: guid(appConfigurationStore_xM7mBhesj.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
                principalId: principalId
                principalType: principalType
              }
            }

            output appConfigEndpoint string = appConfigurationStore_xM7mBhesj.properties.endpoint

            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
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

        var appInsightsManifest = await ManifestUtils.GetManifestWithBicep(appInsights.Resource);
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
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param applicationType string = 'web'

            @description('')
            param kind string = 'web'

            @description('')
            param logAnalyticsWorkspaceId string


            resource applicationInsightsComponent_eYAu4rv7j 'Microsoft.Insights/components@2020-02-02' = {
              name: toLower(take('appInsights${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'appInsights'
              }
              kind: kind
              properties: {
                Application_Type: applicationType
                WorkspaceResourceId: logAnalyticsWorkspaceId
              }
            }

            output appInsightsConnectionString string = applicationInsightsComponent_eYAu4rv7j.properties.ConnectionString

            """;
        output.WriteLine(appInsightsManifest.BicepText);
        Assert.Equal(expectedBicep, appInsightsManifest.BicepText);
    }

    [Fact]
    public async Task AddApplicationInsightsWithoutExplicitLawGetsDefaultLawParameterInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var appInsights = builder.AddAzureApplicationInsights("appInsights");

        appInsights.Resource.Outputs["appInsightsConnectionString"] = "myinstrumentationkey";

        var connectionStringResource = (IResourceWithConnectionString)appInsights.Resource;

        Assert.Equal("appInsights", appInsights.Resource.Name);
        Assert.Equal("myinstrumentationkey", await connectionStringResource.GetConnectionStringAsync());
        Assert.Equal("{appInsights.outputs.appInsightsConnectionString}", appInsights.Resource.ConnectionStringExpression.ValueExpression);

        var appInsightsManifest = await ManifestUtils.GetManifestWithBicep(appInsights.Resource);
        var expectedManifest = """
           {
             "type": "azure.bicep.v0",
             "connectionString": "{appInsights.outputs.appInsightsConnectionString}",
             "path": "appInsights.module.bicep"
           }
           """;
        Assert.Equal(expectedManifest, appInsightsManifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param applicationType string = 'web'

            @description('')
            param kind string = 'web'


            resource applicationInsightsComponent_eYAu4rv7j 'Microsoft.Insights/components@2020-02-02' = {
              name: toLower(take('appInsights${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'appInsights'
              }
              kind: kind
              properties: {
                Application_Type: applicationType
                WorkspaceResourceId: operationalInsightsWorkspace_smwjw0Wga.id
              }
            }

            resource operationalInsightsWorkspace_smwjw0Wga 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
              name: toLower(take('law-appInsights${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'law-appInsights'
              }
              properties: {
                sku: {
                  name: 'PerGB2018'
                }
              }
            }

            output appInsightsConnectionString string = applicationInsightsComponent_eYAu4rv7j.properties.ConnectionString
            
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

        var appInsightsManifest = await ManifestUtils.GetManifestWithBicep(appInsights.Resource);
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
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location

            @description('')
            param applicationType string = 'web'

            @description('')
            param kind string = 'web'

            @description('')
            param logAnalyticsWorkspaceId string


            resource applicationInsightsComponent_eYAu4rv7j 'Microsoft.Insights/components@2020-02-02' = {
              name: toLower(take('appInsights${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'appInsights'
              }
              kind: kind
              properties: {
                Application_Type: applicationType
                WorkspaceResourceId: logAnalyticsWorkspaceId
              }
            }

            output appInsightsConnectionString string = applicationInsightsComponent_eYAu4rv7j.properties.ConnectionString

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

        var appInsightsManifest = await ManifestUtils.GetManifestWithBicep(logAnalyticsWorkspace.Resource);
        var expectedManifest = """
           {
             "type": "azure.bicep.v0",
             "path": "logAnalyticsWorkspace.module.bicep"
           }
           """;
        Assert.Equal(expectedManifest, appInsightsManifest.ManifestNode.ToString());

        var expectedBicep = """
            targetScope = 'resourceGroup'

            @description('')
            param location string = resourceGroup().location


            resource operationalInsightsWorkspace_DuWNVIPPL 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
              name: toLower(take('logAnalyticsWorkspace${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'logAnalyticsWorkspace'
              }
              properties: {
                sku: {
                  name: 'PerGB2018'
                }
              }
            }

            output logAnalyticsWorkspaceId string = operationalInsightsWorkspace_DuWNVIPPL.id

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

        var serviceA = builder.AddProject<ProjectA>("serviceA")
            .WithReference(appInsights);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(serviceA.Resource);

        Assert.True(config.ContainsKey("APPLICATIONINSIGHTS_CONNECTION_STRING"));
        Assert.Equal("myinstrumentationkey", config["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
    }

    [Fact]
    public async Task AddAzureConstructGenertesCorrectManifestEntry()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
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
        using var builder = TestDistributedApplicationBuilder.Create();
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
        using var builder = TestDistributedApplicationBuilder.Create();
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
        using var builder = TestDistributedApplicationBuilder.Create();

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

            resource redisCache_enclX3umP 'Microsoft.Cache/Redis@2020-06-01' = {
              name: toLower(take('cache${uniqueString(resourceGroup().id)}', 24))
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
                value: '${redisCache_enclX3umP.properties.hostName},ssl=true,password=${redisCache_enclX3umP.listKeys(redisCache_enclX3umP.apiVersion).primaryKey}'
              }
            }

            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddKeyVaultViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

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


            resource keyVault_aMZbuK3Sy 'Microsoft.KeyVault/vaults@2022-07-01' = {
              name: toLower(take('mykv${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'mykv'
              }
              properties: {
                tenantId: tenant().tenantId
                sku: {
                  family: 'A'
                  name: 'standard'
                }
                enableRbacAuthorization: true
              }
            }

            resource roleAssignment_hVU9zjQV1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: keyVault_aMZbuK3Sy
              name: guid(keyVault_aMZbuK3Sy.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
                principalId: principalId
                principalType: principalType
              }
            }

            output vaultUri string = keyVault_aMZbuK3Sy.properties.vaultUri

            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddKeyVaultViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

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


            resource keyVault_aMZbuK3Sy 'Microsoft.KeyVault/vaults@2022-07-01' = {
              name: toLower(take('mykv${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'mykv'
              }
              properties: {
                tenantId: tenant().tenantId
                sku: {
                  family: 'A'
                  name: 'standard'
                }
                enableRbacAuthorization: true
              }
            }

            resource roleAssignment_hVU9zjQV1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: keyVault_aMZbuK3Sy
              name: guid(keyVault_aMZbuK3Sy.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
                principalId: principalId
                principalType: principalType
              }
            }

            output vaultUri string = keyVault_aMZbuK3Sy.properties.vaultUri

            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

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


            resource signalRService_iD3Yrl49T 'Microsoft.SignalRService/signalR@2022-02-01' = {
              name: toLower(take('signalr${uniqueString(resourceGroup().id)}', 24))
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

            resource roleAssignment_35voRFfVj 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: signalRService_iD3Yrl49T
              name: guid(signalRService_iD3Yrl49T.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalId: principalId
                principalType: principalType
              }
            }

            output hostName string = signalRService_iD3Yrl49T.properties.hostName

            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AsAzureSqlDatabaseViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

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


            resource sqlServer_lF9QWGqAt 'Microsoft.Sql/servers@2020-11-01-preview' = {
              name: toLower(take('sql${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'sql'
              }
              properties: {
                version: '12.0'
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

            resource sqlFirewallRule_vcw7qNn72 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
              parent: sqlServer_lF9QWGqAt
              name: 'AllowAllAzureIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '0.0.0.0'
              }
            }

            resource sqlFirewallRule_IgqbBC6Hr 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
              parent: sqlServer_lF9QWGqAt
              name: 'fw'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '255.255.255.255'
              }
            }

            resource sqlDatabase_m3U42g9Y8 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
              parent: sqlServer_lF9QWGqAt
              name: 'dbName'
              location: location
              properties: {
              }
            }

            output sqlServerFqdn string = sqlServer_lF9QWGqAt.properties.fullyQualifiedDomainName

            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AsAzureSqlDatabaseViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

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
                "principalName": ""
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


            resource sqlServer_lF9QWGqAt 'Microsoft.Sql/servers@2020-11-01-preview' = {
              name: toLower(take('sql${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'sql'
              }
              properties: {
                version: '12.0'
                publicNetworkAccess: 'Enabled'
                administrators: {
                  administratorType: 'ActiveDirectory'
                  login: principalName
                  sid: principalId
                  tenantId: subscription().tenantId
                  azureADOnlyAuthentication: true
                }
              }
            }

            resource sqlFirewallRule_vcw7qNn72 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
              parent: sqlServer_lF9QWGqAt
              name: 'AllowAllAzureIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '0.0.0.0'
              }
            }

            resource sqlDatabase_m3U42g9Y8 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
              parent: sqlServer_lF9QWGqAt
              name: 'dbName'
              location: location
              properties: {
              }
            }

            output sqlServerFqdn string = sqlServer_lF9QWGqAt.properties.fullyQualifiedDomainName

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

        IResourceBuilder<AzurePostgresResource>? azurePostgres = null;
        var postgres = builder.AddPostgres("postgres", usr, pwd).AsAzurePostgresFlexibleServer((resource, _, _) =>
        {
            Assert.NotNull(resource);
            azurePostgres = resource;
        });
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
                "keyVaultName": "",
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}"
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
            param keyVaultName string


            resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
              name: keyVaultName
            }

            resource postgreSqlFlexibleServer_hFZg1J8nf 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
              name: toLower(take('postgres${uniqueString(resourceGroup().id)}', 24))
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

            resource postgreSqlFirewallRule_t5EgXW1q4 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_hFZg1J8nf
              name: 'AllowAllAzureIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '0.0.0.0'
              }
            }

            resource postgreSqlFirewallRule_T9qS4dcOa 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_hFZg1J8nf
              name: 'AllowAllIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '255.255.255.255'
              }
            }

            resource postgreSqlFlexibleServerDatabase_QJSbpnLQ9 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_hFZg1J8nf
              name: 'dbName'
              properties: {
              }
            }

            resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
              parent: keyVault_IeF8jZvXV
              name: 'connectionString'
              location: location
              properties: {
                value: 'Host=${postgreSqlFlexibleServer_hFZg1J8nf.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
              }
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

        IResourceBuilder<AzurePostgresResource>? azurePostgres = null;
        var postgres = builder.AddPostgres("postgres", usr, pwd).AsAzurePostgresFlexibleServer((resource, _, _) =>
        {
            Assert.NotNull(resource);
            azurePostgres = resource;
        });
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
                "keyVaultName": "",
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}"
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
            param keyVaultName string


            resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
              name: keyVaultName
            }

            resource postgreSqlFlexibleServer_hFZg1J8nf 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
              name: toLower(take('postgres${uniqueString(resourceGroup().id)}', 24))
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

            resource postgreSqlFirewallRule_t5EgXW1q4 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_hFZg1J8nf
              name: 'AllowAllAzureIps'
              properties: {
                startIpAddress: '0.0.0.0'
                endIpAddress: '0.0.0.0'
              }
            }

            resource postgreSqlFlexibleServerDatabase_QJSbpnLQ9 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
              parent: postgreSqlFlexibleServer_hFZg1J8nf
              name: 'dbName'
              properties: {
              }
            }

            resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
              parent: keyVault_IeF8jZvXV
              name: 'connectionString'
              location: location
              properties: {
                value: 'Host=${postgreSqlFlexibleServer_hFZg1J8nf.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
              }
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

        var postgres = builder.AddPostgres("postgres", usr, pwd).PublishAsAzurePostgresFlexibleServer();
        postgres.AddDatabase("db");

        var manifest = await ManifestUtils.GetManifestWithBicep(postgres.Resource);

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
                "keyVaultName": "",
                "administratorLogin": "{usr.value}",
                "administratorLoginPassword": "{pwd.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());
    }

    [Fact]
    public async Task PublishAsAzurePostgresFlexibleServerNoUserPassParams()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres = builder.AddPostgres("postgres1")
            .PublishAsAzurePostgresFlexibleServer(); // Because of InternalsVisibleTo

        var manifest = await ManifestUtils.GetManifest(postgres.Resource);
        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres1.secretOutputs.connectionString}",
              "path": "postgres1.module.bicep",
              "params": {
                "keyVaultName": "",
                "administratorLogin": "{postgres1-username.value}",
                "administratorLoginPassword": "{postgres1-password.value}"
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
                "keyVaultName": "",
                "administratorLogin": "{param.value}",
                "administratorLoginPassword": "{postgres2-password.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());

        postgres = builder.AddPostgres("postgres3", password: param)
            .PublishAsAzurePostgresFlexibleServer();

        manifest = await ManifestUtils.GetManifest(postgres.Resource);
        expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres3.secretOutputs.connectionString}",
              "path": "postgres3.module.bicep",
              "params": {
                "keyVaultName": "",
                "administratorLogin": "{postgres3-username.value}",
                "administratorLoginPassword": "{param.value}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task AddAzureServiceBus()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
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


            resource serviceBusNamespace_1RzZvI0LZ 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
              name: toLower(take('sb${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'sb'
              }
              sku: {
                name: sku
              }
              properties: {
              }
            }

            resource roleAssignment_GAWCqJpjI 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: serviceBusNamespace_1RzZvI0LZ
              name: guid(serviceBusNamespace_1RzZvI0LZ.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalId: principalId
                principalType: principalType
              }
            }

            resource serviceBusQueue_kQwbucWhl 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
              parent: serviceBusNamespace_1RzZvI0LZ
              name: 'queue1'
              location: location
              properties: {
              }
            }

            resource serviceBusQueue_4iiWSBLWy 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
              parent: serviceBusNamespace_1RzZvI0LZ
              name: 'queue2'
              location: location
              properties: {
              }
            }

            resource serviceBusTopic_6HCaIBS2e 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
              parent: serviceBusNamespace_1RzZvI0LZ
              name: 't1'
              location: location
              properties: {
              }
            }

            resource serviceBusSubscription_ZeDpI38Lv 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
              parent: serviceBusTopic_6HCaIBS2e
              name: 's3'
              location: location
              properties: {
              }
            }

            resource serviceBusTopic_VUuvZGvsD 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
              parent: serviceBusNamespace_1RzZvI0LZ
              name: 't2'
              location: location
              properties: {
              }
            }

            output serviceBusEndpoint string = serviceBusNamespace_1RzZvI0LZ.properties.serviceBusEndpoint

            """;
        output.WriteLine(manifest.BicepText);
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

        var blobqs = AzureStorageEmulatorConnectionString.Create(blobPort: 10000);
        var queueqs = AzureStorageEmulatorConnectionString.Create(queuePort: 10001);
        var tableqs = AzureStorageEmulatorConnectionString.Create(tablePort: 10002);

        Assert.Equal(blobqs, blob.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(queueqs, queue.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(tableqs, table.Resource.ConnectionStringExpression.ValueExpression);

        Assert.Equal(blobqs, await ((IResourceWithConnectionString)blob.Resource).GetConnectionStringAsync());
        Assert.Equal(queueqs, await ((IResourceWithConnectionString)queue.Resource).GetConnectionStringAsync());
        Assert.Equal(tableqs, await ((IResourceWithConnectionString)table.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureStorageViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

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


            resource storageAccount_1XR3Um8QY 'Microsoft.Storage/storageAccounts@2022-09-01' = {
              name: toLower(take('storage${uniqueString(resourceGroup().id)}', 24))
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
                networkAcls: {
                  defaultAction: 'Allow'
                }
              }
            }

            resource blobService_vTLU20GRg 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
              parent: storageAccount_1XR3Um8QY
              name: 'default'
              properties: {
              }
            }

            resource roleAssignment_Gz09cEnxb 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_1XR3Um8QY
              name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_HRj6MDafS 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_1XR3Um8QY
              name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_r0wA6OpKE 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_1XR3Um8QY
              name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
                principalId: principalId
                principalType: principalType
              }
            }

            output blobEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.blob
            output queueEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.queue
            output tableEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.table
            
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


            resource storageAccount_1XR3Um8QY 'Microsoft.Storage/storageAccounts@2022-09-01' = {
              name: toLower(take('storage${uniqueString(resourceGroup().id)}', 24))
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
                networkAcls: {
                  defaultAction: 'Allow'
                }
              }
            }

            resource blobService_vTLU20GRg 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
              parent: storageAccount_1XR3Um8QY
              name: 'default'
              properties: {
              }
            }

            resource roleAssignment_Gz09cEnxb 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_1XR3Um8QY
              name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_HRj6MDafS 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_1XR3Um8QY
              name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_r0wA6OpKE 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: storageAccount_1XR3Um8QY
              name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
                principalId: principalId
                principalType: principalType
              }
            }

            output blobEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.blob
            output queueEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.queue
            output tableEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.table
            
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


            resource searchService_j3umigYGT 'Microsoft.Search/searchServices@2023-11-01' = {
              name: toLower(take('search${uniqueString(resourceGroup().id)}', 24))
              location: location
              tags: {
                'aspire-resource-name': 'search'
              }
              sku: {
                name: searchSku
              }
              properties: {
                replicaCount: 1
                partitionCount: 1
                hostingMode: 'default'
                disableLocalAuth: true
              }
            }

            resource roleAssignment_f77ijNEYF 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: searchService_j3umigYGT
              name: guid(searchService_j3umigYGT.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
                principalId: principalId
                principalType: principalType
              }
            }

            resource roleAssignment_s0J7B4aGN 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: searchService_j3umigYGT
              name: guid(searchService_j3umigYGT.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
                principalId: principalId
                principalType: principalType
              }
            }

            output connectionString string = 'Endpoint=https://${searchService_j3umigYGT.name}.search.windows.net'

            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task PublishAsConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ai = builder.AddAzureApplicationInsights("ai").PublishAsConnectionString();
        var serviceBus = builder.AddAzureServiceBus("servicebus").PublishAsConnectionString();

        var serviceA = builder.AddProject<ProjectA>("serviceA")
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
        using var builder = TestDistributedApplicationBuilder.Create();

        IEnumerable<CognitiveServicesAccountDeployment>? aiDeployments = null;
        var openai = builder.AddAzureOpenAI("openai", (_, _, _, deployments) =>
        {
            aiDeployments = deployments;
        })
            .AddDeployment(new("mymodel", "gpt-35-turbo", "0613", "Basic", 4))
            .AddDeployment(new("embedding-model", "text-embedding-ada-002", "2", "Basic", 4));

        var manifest = await ManifestUtils.GetManifestWithBicep(openai.Resource);

        Assert.NotNull(aiDeployments);
        Assert.Collection(
            aiDeployments,
            deployment => Assert.Equal("mymodel", deployment.Properties.Name),
            deployment => Assert.Equal("embedding-model", deployment.Properties.Name));

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


            resource cognitiveServicesAccount_wXAGTFUId 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
              name: toLower(take('openai${uniqueString(resourceGroup().id)}', 24))
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

            resource roleAssignment_Hsk8rxWY8 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              scope: cognitiveServicesAccount_wXAGTFUId
              name: guid(cognitiveServicesAccount_wXAGTFUId.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
              properties: {
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
                principalId: principalId
                principalType: principalType
              }
            }

            resource cognitiveServicesAccountDeployment_5K9aRgiZP 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
              parent: cognitiveServicesAccount_wXAGTFUId
              name: 'mymodel'
              sku: {
                name: 'Basic'
                capacity: 4
              }
              properties: {
                model: {
                  format: 'OpenAI'
                  name: 'gpt-35-turbo'
                  version: '0613'
                }
              }
            }

            resource cognitiveServicesAccountDeployment_mdCAJJRlf 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
              parent: cognitiveServicesAccount_wXAGTFUId
              dependsOn: [
                cognitiveServicesAccountDeployment_5K9aRgiZP
              ]
              name: 'embedding-model'
              sku: {
                name: 'Basic'
                capacity: 4
              }
              properties: {
                model: {
                  format: 'OpenAI'
                  name: 'text-embedding-ada-002'
                  version: '2'
                }
              }
            }

            output connectionString string = 'Endpoint=${cognitiveServicesAccount_wXAGTFUId.properties.endpoint}'
            
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }
}
