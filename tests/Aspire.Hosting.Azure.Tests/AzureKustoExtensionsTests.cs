// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Kusto;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureKustoExtensionsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddAzureKustoCluster()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var kusto = builder.AddAzureKustoCluster("kusto");
        kusto.Resource.Outputs["clusterUri"] = "https://kusto-cluster.eastus.kusto.windows.net";
        Assert.Equal("https://kusto-cluster.eastus.kusto.windows.net", await kusto.Resource.ConnectionStringExpression.GetValueAsync(default));

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, kusto.Resource);

        var connectionStringResource = (IResourceWithConnectionString)kusto.Resource;

        Assert.Equal("https://kusto-cluster.eastus.kusto.windows.net", await connectionStringResource.GetConnectionStringAsync());

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{kusto.outputs.clusterUri}",
              "path": "kusto.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource kusto 'Microsoft.Kusto/clusters@2024-04-13' = {
              name: take('kusto-${uniqueString(resourceGroup().id)}', 50)
              location: location
              properties: {
              }
              tags: {
                'aspire-resource-name': 'kusto'
              }
            }

            output clusterUri string = kusto.uri

            output name string = kusto.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var kustoRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "kusto-roles");
        var kustoRolesManifest = await GetManifestWithBicep(kustoRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param kusto_outputs_name string

            param principalType string

            param principalId string

            resource kusto 'Microsoft.Kusto/clusters@2024-04-13' existing = {
              name: kusto_outputs_name
            }

            resource kusto_Contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(kusto.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
                principalType: principalType
              }
              scope: kusto
            }
            """;
        output.WriteLine(kustoRolesManifest.BicepText);
        Assert.Equal(expectedBicep, kustoRolesManifest.BicepText);
    }

    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureKustoClusterResource()
    {
        // Arrange
        var kustoResource = new AzureKustoClusterResource("test-kusto", _ => { });
        var infrastructure = new AzureResourceInfrastructure(kustoResource, "test-kusto");

        // Act - Call AddAsExistingResource twice
        var firstResult = kustoResource.AddAsExistingResource(infrastructure);
        var secondResult = kustoResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public async Task AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureKustoClusterResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var existingName = builder.AddParameter("existing-kusto-name");
        var existingResourceGroup = builder.AddParameter("existing-kusto-rg");

        var kusto = builder.AddAzureKustoCluster("test-kusto")
            .AsExisting(existingName, existingResourceGroup);

        var module = builder.AddAzureInfrastructure("mymodule", infra =>
        {
            _ = kusto.Resource.AddAsExistingResource(infra);
        });

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(module.Resource, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
             .AppendContentAsFile(bicep, "bicep");
    }
}