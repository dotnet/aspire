// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureContainerAppEnvironmentExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureContainerAppEnvironmentResource()
    {
        // Arrange
        var containerAppEnvironmentResource = new AzureContainerAppEnvironmentResource("test-container-app-env", _ => { });
        var infrastructure = new AzureResourceInfrastructure(containerAppEnvironmentResource, "test-container-app-env");

        // Act - Call AddAsExistingResource twice
        var firstResult = containerAppEnvironmentResource.AddAsExistingResource(infrastructure);
        var secondResult = containerAppEnvironmentResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public async Task AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureContainerAppEnvironmentResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var existingName = builder.AddParameter("existing-env-name");
        var existingResourceGroup = builder.AddParameter("existing-env-rg");

        var containerAppEnvironment = builder.AddAzureContainerAppEnvironment("test-container-app-env")
            .AsExisting(existingName, existingResourceGroup);

        var module = builder.AddAzureInfrastructure("mymodule", infra =>
        {
            _ = containerAppEnvironment.Resource.AddAsExistingResource(infra);
        });

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(module.Resource, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
             .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task WithAzureLogAnalyticsWorkspace_RespectsExistingWorkspaceInDifferentResourceGroup()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Create parameters for existing Log Analytics Workspace in resource group "X"
        var lawName = builder.AddParameter("log-env-shared-name");
        var lawResourceGroup = builder.AddParameter("log-env-shared-rg"); // resource group "X"

        // Create Log Analytics Workspace resource marked as existing in resource group "X"
        var logAnalyticsWorkspace = builder
            .AddAzureLogAnalyticsWorkspace("log-env-shared")
            .AsExisting(lawName, lawResourceGroup);

        // Create Container App Environment in resource group "Y" that references the existing LAW
        var containerAppEnvironment = builder
            .AddAzureContainerAppEnvironment("app-host")  // This will be deployed to default resource group "Y"
            .WithAzureLogAnalyticsWorkspace(logAnalyticsWorkspace);

        // Verify that the LAW has the ExistingAzureResourceAnnotation
        Assert.True(logAnalyticsWorkspace.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAnnotation));
        Assert.Equal(lawName.Resource, existingAnnotation.Name);
        Assert.Equal(lawResourceGroup.Resource, existingAnnotation.ResourceGroup);

        // Verify that the Container App Environment has the AzureLogAnalyticsWorkspaceReferenceAnnotation
        Assert.True(containerAppEnvironment.Resource.TryGetLastAnnotation<AzureLogAnalyticsWorkspaceReferenceAnnotation>(out var workspaceRef));
        Assert.Same(logAnalyticsWorkspace.Resource, workspaceRef.Workspace);

        // Act & Assert - Generate bicep and verify using snapshot testing
        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(containerAppEnvironment.Resource);

        await Verify(bicep, extension: "bicep")
            .AppendContentAsFile(manifest.ToString(), "json");
    }
}
