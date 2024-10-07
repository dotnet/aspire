// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001 // Because we are testing CDK callbacks.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Roles;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Storage;
using Azure.Provisioning.Sql;
using Azure.Provisioning.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using System.Net.Sockets;

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
        static void CreateConstruct(ResourceModuleConstruct construct)
        {
            var id = new UserAssignedIdentity("id");
            construct.Add(id);
            construct.Add(new ProvisioningOutput("cid", typeof(string)) { Value = id.ClientId });
        }

        return new()
        {
            { builder => builder.AddAzureAppConfiguration(resourceName) },
            { builder => builder.AddAzureApplicationInsights(resourceName) },
            { builder => builder.AddBicepTemplate(resourceName, "template.bicep") },
            { builder => builder.AddBicepTemplateString(resourceName, "content") },
            { builder => builder.AddAzureConstruct(resourceName, CreateConstruct) },
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

        if (azureResourceBuilder.Resource is not AzureConstructResource bicepResource)
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

        if (azureResourceBuilder.Resource is not AzureConstructResource bicepResource)
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyVaultName string

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

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

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionString'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey}'
              }
              parent: keyVault
            }
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param keyVaultName string

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }

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

            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionString'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey}'
              }
              parent: keyVault
            }
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalType string

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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param applicationType string = 'web'

            param kind string = 'web'

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

        var appInsightsManifest = await ManifestUtils.GetManifestWithBicep(logAnalyticsWorkspace.Resource);
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
    public async Task AddAzureConstructGenertesCorrectManifestEntry()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var construct1 = builder.AddAzureConstruct("construct1", (construct) =>
        {
            var storage = new StorageAccount("storage")
            {
                Kind = StorageKind.StorageV2,
                Sku = new StorageSku() { Name = StorageSkuName.StandardLrs }
            };
            construct.Add(storage);
            construct.Add(new ProvisioningOutput("storageAccountName", typeof(string)) { Value = storage.Name });
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
            var storage = new StorageAccount("storage")
            {
                Kind = StorageKind.StorageV2,
                Sku = new StorageSku() { Name = skuName.AsProvisioningParameter(construct) }
            };
            construct.Add(storage);
            moduleConstruct = construct;
        });

        var manifest = await ManifestUtils.GetManifest(construct1.Resource);

        Assert.NotNull(moduleConstruct);
        var constructParameters = moduleConstruct.GetParameters().DistinctBy(x => x.IdentifierName);
        var constructParametersLookup = constructParameters.ToDictionary(p => p.IdentifierName);
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
            var storage = new StorageAccount("storage")
            {
                Kind = StorageKind.StorageV2,
                Sku = new StorageSku() { Name = skuName.AsProvisioningParameter(construct, parameterName: "sku") }
            };
            construct.Add(storage);
            moduleConstruct = construct;
        });

        var manifest = await ManifestUtils.GetManifest(construct1.Resource);

        Assert.NotNull(moduleConstruct);
        var constructParameters = moduleConstruct.GetParameters().DistinctBy(x => x.IdentifierName);
        var constructParametersLookup = constructParameters.ToDictionary(p => p.IdentifierName);
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

#pragma warning disable CS0618 // Type or member is obsolete
        var redis = builder.AddRedis("cache")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
            .PublishAsAzureRedis();
#pragma warning restore CS0618 // Type or member is obsolete

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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalType string

            resource mykv 'Microsoft.KeyVault/vaults@2023-07-01' = {
              name: take('mykv-${uniqueString(resourceGroup().id)}', 24)
              location: location
              properties: {
                tenantId: tenant().tenantId
                sku: {
                  family: 'A'
                  name: 'standard'
                }
                enableRbacAuthorization: true
              }
              tags: {
                'aspire-resource-name': 'mykv'
              }
            }

            resource mykv_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(mykv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
                principalType: principalType
              }
              scope: mykv
            }

            output vaultUri string = mykv.properties.vaultUri
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalType string

            resource mykv 'Microsoft.KeyVault/vaults@2023-07-01' = {
              name: take('mykv-${uniqueString(resourceGroup().id)}', 24)
              location: location
              properties: {
                tenantId: tenant().tenantId
                sku: {
                  family: 'A'
                  name: 'standard'
                }
                enableRbacAuthorization: true
              }
              tags: {
                'aspire-resource-name': 'mykv'
              }
            }

            resource mykv_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(mykv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
                principalType: principalType
              }
              scope: mykv
            }

            output vaultUri string = mykv.properties.vaultUri
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalType string

            resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' = {
              name: take('signalr-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                cors: {
                  allowedOrigins: [
                    '*'
                  ]
                }
                features: [
                  {
                    flag: 'ServiceMode'
                    value: 'Default'
                  }
                ]
              }
              kind: 'SignalR'
              sku: {
                name: 'Free_F1'
                capacity: 1
              }
              tags: {
                'aspire-resource-name': 'signalr'
              }
            }

            resource signalr_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
                principalType: principalType
              }
              scope: signalr
            }

            output hostName string = signalr.properties.hostName
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsAzureSqlDatabaseViaRunMode(bool overrideDefaultTlsVersion)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

#pragma warning disable CS0618 // Type or member is obsolete
        var sql = builder.AddSqlServer("sql").AsAzureSqlDatabase((azureSqlBuilder, _, sql, _) =>
        {
            azureSqlBuilder.Resource.Outputs["sqlServerFqdn"] = "myserver";

            if (overrideDefaultTlsVersion)
            {
                sql.MinTlsVersion = SqlMinimalTlsVersion.Tls1_3;
            }
        });
#pragma warning restore CS0618 // Type or member is obsolete
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
                minimalTlsVersion: '{{(overrideDefaultTlsVersion ? "1.3" : "1.2")}}'
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
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsAzureSqlDatabaseViaPublishMode(bool overrideDefaultTlsVersion)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

#pragma warning disable CS0618 // Type or member is obsolete
        var sql = builder.AddSqlServer("sql").AsAzureSqlDatabase((azureSqlBuilder, _, sql, _) =>
        {
            azureSqlBuilder.Resource.Outputs["sqlServerFqdn"] = "myserver";

            if (overrideDefaultTlsVersion)
            {
                sql.MinTlsVersion = SqlMinimalTlsVersion.Tls1_3;
            }
        });
#pragma warning restore CS0618 // Type or member is obsolete
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
                minimalTlsVersion: '{{(overrideDefaultTlsVersion ? "1.3" : "1.2")}}'
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
        IResourceBuilder<AzurePostgresResource>? azurePostgres = null;
        var postgres = builder.AddPostgres("postgres", usr, pwd).AsAzurePostgresFlexibleServer((resource, _, _) =>
        {
            Assert.NotNull(resource);
            azurePostgres = resource;
        });
        postgres.AddDatabase("db", "dbName");
#pragma warning restore CS0618 // Type or member is obsolete

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
        IResourceBuilder<AzurePostgresResource>? azurePostgres = null;
        var postgres = builder.AddPostgres("postgres", usr, pwd).AsAzurePostgresFlexibleServer((resource, _, _) =>
        {
            Assert.NotNull(resource);
            azurePostgres = resource;
        });
        postgres.AddDatabase("db", "dbName");
#pragma warning restore CS0618 // Type or member is obsolete

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
#pragma warning restore CS0618 // Type or member is obsolete

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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalId string

            param principalType string

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

            resource sb_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: sb
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

            resource t2 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
              name: 't2'
              parent: sb
            }

            resource s3 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
              name: 's3'
              parent: t1
            }

            output serviceBusEndpoint string = sb.properties.serviceBusEndpoint
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
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
              "path": "wps1.module.bicep",
              "params": {
                "principalId": "",
                "principalType": ""
              }
            }
            """;

        var connectionStringResource = (IResourceWithConnectionString)wps.Resource;

        Assert.Equal("https://mywebpubsubendpoint", await connectionStringResource.GetConnectionStringAsync());
        var manifest = await ManifestUtils.GetManifestWithBicep(wps.Resource);
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        Assert.Equal("wps1", wps.Resource.Name);
        output.WriteLine(manifest.BicepText);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            param principalId string

            param principalType string

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

            resource wps1_WebPubSubServiceOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(wps1.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
                principalType: principalType
              }
              scope: wps1
            }

            output endpoint string = 'https://${wps1.properties.hostName}'
            """;
        Assert.Equal(expectedBicep, manifest.BicepText);
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
                "principalId": "",
                "principalType": "",
                "sku": "Standard_S1",
                "capacity": 2
              }
            }
            """;
        var manifest = await ManifestUtils.GetManifestWithBicep(wps.Resource);
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        Assert.Equal("wps1", wps.Resource.Name);
        output.WriteLine(manifest.BicepText);
        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Free_F1'

            param capacity int = 1

            param principalId string

            param principalType string

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

            resource wps1_WebPubSubServiceOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(wps1.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
                principalType: principalType
              }
              scope: wps1
            }

            output endpoint string = 'https://${wps1.properties.hostName}'
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

        Assert.Equal(Resolve(blobqs, "blob", 10000), await((IResourceWithConnectionString)blob.Resource).GetConnectionStringAsync());
        Assert.Equal(Resolve(queueqs, "queue", 10001), await ((IResourceWithConnectionString)queue.Resource).GetConnectionStringAsync());
        Assert.Equal(Resolve(tableqs, "table", 10002), await ((IResourceWithConnectionString)table.Resource).GetConnectionStringAsync());
    }

    [Fact]
    public async Task AddAzureStorageViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage", (_, construct, sa) =>
        {
            sa.Sku = new StorageSku()
            {
                Name = storagesku.AsProvisioningParameter(construct)
            };
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            param principalId string

            param principalType string

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
        var storage = builder.AddAzureStorage("storage", (_, construct, sa) =>
        {
            sa.Sku = new StorageSku()
            {
                Name = storagesku.AsProvisioningParameter(construct)
            };
            sa.AllowSharedKeyAccess = true;
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            param principalId string

            param principalType string

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
        var storage = builder.AddAzureStorage("storage", (_, construct, sa) =>
        {
            sa.Sku = new StorageSku()
            {
                Name = storagesku.AsProvisioningParameter(construct)
            };
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            param principalId string

            param principalType string

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
    public async Task AddAzureStorageViaPublishModeEnableAllowSharedKeyAccessOverridesDefaultFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storagesku = builder.AddParameter("storagesku");
        var storage = builder.AddAzureStorage("storage", (_, construct, sa) =>
        {
            sa.Sku = new StorageSku()
            {
                Name = storagesku.AsProvisioningParameter(construct)
            };
            sa.AllowSharedKeyAccess = true;
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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storagesku string

            param principalId string

            param principalType string

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
        var search = builder.AddAzureSearch("search", (_, construct, search) =>
            search.SearchSkuName = sku.AsProvisioningParameter(construct));

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
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param searchSku string

            param principalId string

            param principalType string

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

            output connectionString string = 'Endpoint=https://${search.name}.search.windows.net'
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAzureOpenAI(bool overrideLocalAuthDefault)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        IEnumerable<CognitiveServicesAccountDeployment>? aiDeployments = null;
        var openai = builder.AddAzureOpenAI("openai", (_, _, account, deployments) =>
        {
            aiDeployments = deployments;

            if (overrideLocalAuthDefault)
            {
                account.Properties.Value!.DisableLocalAuth = false;
            }
        })
            .AddDeployment(new("mymodel", "gpt-35-turbo", "0613", "Basic", 4))
            .AddDeployment(new("embedding-model", "text-embedding-ada-002", "2", "Basic", 4));

        var manifest = await ManifestUtils.GetManifestWithBicep(openai.Resource);

        Assert.NotNull(aiDeployments);
        Assert.Collection(
            aiDeployments,
            deployment => Assert.Equal("mymodel", deployment.Name.Value),
            deployment => Assert.Equal("embedding-model", deployment.Name.Value));

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

        var expectedBicep = $$"""
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalType string

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

            resource openai_CognitiveServicesOpenAIContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(openai.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
                principalType: principalType
              }
              scope: openai
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
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public void ConfigureConstructMustNotBeNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var constructResource = builder.AddAzureConstruct("construct", r =>
        {
            r.Add(new KeyVaultService("kv"));
        });

        var ex = Assert.Throws<ArgumentNullException>(() => constructResource.ConfigureConstruct(null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public async Task ConstructCanBeMutatedAfterCreation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var constructResource = builder.AddAzureConstruct("construct", r =>
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
        .ConfigureConstruct(r =>
        {
            var vault = r.GetResources().OfType<KeyVaultService>().Single();
            Assert.NotNull(vault);

            r.Add(new ProvisioningOutput("vaultUri", typeof(string))
            {
                Value =
                    new MemberExpression(
                        new MemberExpression(
                            new IdentifierExpression(vault.IdentifierName),
                            "properties"),
                        "vaultUri")
                // TODO: this should be
                //Value = keyVault.VaultUri
            });
        })
        .ConfigureConstruct(r =>
        {
            var vault = r.GetResources().OfType<KeyVaultService>().Single();
            Assert.NotNull(vault);

            r.Add(new KeyVaultSecret("secret")
            {
                Parent = vault,
                Name = "kvs",
                Properties = new SecretProperties { Value = "00000000-0000-0000-0000-000000000000" }
            });
        });

        var (manifest, bicep) = await ManifestUtils.GetManifestWithBicep(constructResource.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "construct.module.bicep"
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

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";
    }
}
