// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureCosmosDBExtensionsTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedPort(int? port = null)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.WithGatewayPort(port);
        });

        var endpointAnnotation = cosmos.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
        Assert.NotNull(endpointAnnotation);

        var actualPort = endpointAnnotation.Port;
        Assert.Equal(port, actualPort);
    }

    [Theory]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedImageTag(string imageTag)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.WithImageTag(imageTag);
        });

        var containerImageAnnotation = cosmos.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        var actualTag = containerImageAnnotation.Tag;
        Assert.Equal(imageTag ?? "latest", actualTag);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(12)]
    public async Task AddAzureCosmosDBWithPartitionCountCanOverrideNumberOfPartitions(int partitionCount)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(r => r.WithPartitionCount(partitionCount));
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(cosmos.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(partitionCount.ToString(CultureInfo.InvariantCulture), config["AZURE_COSMOS_EMULATOR_PARTITION_COUNT"]);
    }

    [Fact]
    public void AddAzureCosmosDBWithDataExplorer()
    {
#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        cosmos.RunAsPreviewEmulator(e => e.WithDataExplorer());

        var endpoint = cosmos.GetEndpoint("data-explorer");
        Assert.NotNull(endpoint);
        Assert.Equal(1234, endpoint.TargetPort);

        // WithDataExplorer doesn't work against the non-preview emulator
        var cosmos2 = builder.AddAzureCosmosDB("cosmos2");
        Assert.Throws<NotSupportedException>(() => cosmos2.RunAsEmulator(e => e.WithDataExplorer()));
#pragma warning restore ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
    }

    [Fact]
    public void AzureCosmosDBHasCorrectConnectionStrings_ForAccountEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        Assert.Equal("{cosmos.outputs.connectionString}", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        // Endpoint-based connection info gets passed as a connection string to
        // support setting the correct properties on child resources.
        Assert.Equal("AccountEndpoint={cosmos.outputs.connectionString};Database=db1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("AccountEndpoint={cosmos.outputs.connectionString};Database=db1;Container=container1", container1.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureCosmosDBHasCorrectConnectionStrings(bool useAccessKeyAuth)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
        if (useAccessKeyAuth)
        {
            cosmos.WithAccessKeyAuthentication();
        }
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        var cosmos1 = builder.AddAzureCosmosDB("cosmos1").RunAsEmulator();
        if (useAccessKeyAuth)
        {
            cosmos1.WithAccessKeyAuthentication();
        }
        var db2 = cosmos1.AddCosmosDatabase("db2", "db");
        var container2 = db2.AddContainer("container2", "id", "container");

        Assert.DoesNotContain(";Database=db1", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.DoesNotContain(";Database=db1;Container=container1", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Contains(";Database=db1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Contains(";Database=db1;Container=container1", container1.Resource.ConnectionStringExpression.ValueExpression);
        // Validate behavior when resource name and container/database name are different
        Assert.Contains(";Database=db", db2.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Contains(";Database=db;Container=container", container2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AzureCosmosDBAppliesAzureFunctionsConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        var target = new Dictionary<string, object>();
        ((IResourceWithAzureFunctionsConfig)cosmos.Resource).ApplyAzureFunctionsConfiguration(target, "cosmos");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__cosmos__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__cosmos__AccountEndpoint", k),
            k => Assert.Equal("cosmos__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)db1.Resource).ApplyAzureFunctionsConfiguration(target, "db1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__db1__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__db1__DatabaseName", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__db1__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__db1__DatabaseName", k),
            k => Assert.Equal("db1__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)container1.Resource).ApplyAzureFunctionsConfiguration(target, "container1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__container1__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__container1__ContainerName", k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__container1__DatabaseName", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__container1__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__container1__ContainerName", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__container1__DatabaseName", k),
            k => Assert.Equal("container1__accountEndpoint", k));
    }

    /// <summary>
    /// Test both with and without ACA infrastructure because the role assignments
    /// are handled differently between the two. This ensures that the bicep is generated
    /// consistently regardless of the infrastructure used in RunMode.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAzureCosmosDB(bool useAcaInfrastructure)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        if (useAcaInfrastructure)
        {
            builder.AddAzureContainerAppEnvironment("env");
        }

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        builder.AddContainer("api", "myimage")
            .WithReference(cosmos);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, cosmos.Resource);

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

            output connectionString string = cosmos.properties.documentEndpoint

            output name string = cosmos.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var cosmosRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "cosmos-roles");
        var cosmosRolesManifest = await GetManifestWithBicep(cosmosRoles, skipPreparer: true);
        expectedBicep = """
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
            """;
        output.WriteLine(cosmosRolesManifest.BicepText);
        Assert.Equal(expectedBicep, cosmosRolesManifest.BicepText);
    }

    [Fact]
    public async Task AddAzureCosmosDatabase_WorksWithAccessKeyAuth_ChildResources()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .WithAccessKeyAuthentication();
        var database = cosmos.AddCosmosDatabase("db1");
        var container = database.AddContainer("container1", "id");

        builder.AddContainer("api", "myimage")
            .WithReference(cosmos);

        Assert.Equal("{cosmos-kv.secrets.connectionstrings--cosmos}", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{cosmos-kv.secrets.connectionstrings--db1}", database.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{cosmos-kv.secrets.connectionstrings--container1}", container.Resource.ConnectionStringExpression.ValueExpression);

        var manifest = await GetManifestWithBicep(cosmos.Resource);

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

            resource db1 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
              name: 'db1'
              location: location
              properties: {
                resource: {
                  id: 'db1'
                }
              }
              parent: cosmos
            }

            resource container1 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
              name: 'container1'
              location: location
              properties: {
                resource: {
                  id: 'container1'
                  partitionKey: {
                    paths: [
                      'id'
                    ]
                  }
                }
              }
              parent: db1
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

            resource db1_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--db1'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=db1'
              }
              parent: keyVault
            }

            resource container1_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--container1'
              properties: {
                value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=db1;Container=container1'
              }
              parent: keyVault
            }

            output name string = cosmos.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }
}
