#pragma warning disable ASPIRECOMPUTE001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Azure.ContainerRegistry;
using Aspire.Hosting.Utils;
using Azure.Provisioning.ContainerRegistry;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureContainerRegistryTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddAzureContainerRegistry_AddsResourceAndImplementsIContainerRegistry()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        _ = builder.AddAzureContainerRegistry("acr");

        // Build & execute hooks
        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var registryResource = Assert.Single(model.Resources.OfType<AzureContainerRegistryResource>());
        var registryInterface = Assert.IsType<IContainerRegistry>(registryResource, exactMatch: false);

        Assert.NotNull(registryInterface);
        Assert.NotNull(registryInterface.Name);
        Assert.NotNull(registryInterface.Endpoint);
    }

    [Fact]
    public async Task WithRegistry_AttachesContainerRegistryReferenceAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var registryBuilder = builder.AddAzureContainerRegistry("acr");
        _ = builder.AddAzureContainerAppEnvironment("env")
                   .WithAzureContainerRegistry(registryBuilder); // Extension method under test

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var environment = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());

        Assert.True(environment.TryGetLastAnnotation<ContainerRegistryReferenceAnnotation>(out var annotation));
        Assert.Same(registryBuilder.Resource, annotation!.Registry);
    }

    [Fact]
    public async Task AddAzureContainerRegistry_GeneratesCorrectManifestAndBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var acr = builder.AddAzureContainerRegistry("acr");

        var manifest = await GetManifestWithBicep(acr.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "acr.module.bicep"
            }
            """;

        output.WriteLine(manifest.ManifestNode.ToString());
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
              name: take('acr${uniqueString(resourceGroup().id)}', 50)
              location: location
              sku: {
                name: 'Basic'
              }
              tags: {
                'aspire-resource-name': 'acr'
              }
            }

            output name string = acr.name

            output loginServer string = acr.properties.loginServer
            """;

        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task WithRoleAssignments_GeneratesCorrectRoleAssignmentBicep()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Add container app environment since it's required for role assignments
        builder.AddAzureContainerAppEnvironment("env");

        // Create a container registry and assign roles to a project
        var acr = builder.AddAzureContainerRegistry("acr");
        builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(acr, ContainerRegistryBuiltInRole.AcrPull, ContainerRegistryBuiltInRole.AcrPush);

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var rolesResource = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-roles-acr");

        var (rolesManifest, rolesBicep) = await GetManifestWithBicep(rolesResource);

        var expectedRolesManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api-roles-acr.module.bicep",
          "params": {
            "acr_outputs_name": "{acr.outputs.name}",
            "principalId": "{api-identity.outputs.principalId}"
          }
        }
        """;

        Assert.Equal(expectedRolesManifest, rolesManifest.ToString());

        var expectedRolesBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        param acr_outputs_name string

        param principalId string

        resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
          name: acr_outputs_name
        }

        resource acr_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
          name: guid(acr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
          properties: {
            principalId: principalId
            roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
            principalType: 'ServicePrincipal'
          }
          scope: acr
        }

        resource acr_AcrPush 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
          name: guid(acr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8311e382-0749-4cb8-b61a-304f252e45ec'))
          properties: {
            principalId: principalId
            roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8311e382-0749-4cb8-b61a-304f252e45ec')
            principalType: 'ServicePrincipal'
          }
          scope: acr
        }
        """;

        output.WriteLine(rolesBicep);
        Assert.Equal(expectedRolesBicep, rolesBicep);
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
