// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE003 // Type is for evaluation purposes only and is subject to change or removal in future updates.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureContainerAppEnvironmentExtensionsTests
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
        using var builder = TestDistributedApplicationBuilder.Create();
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

    [Fact]
    public void ContainerRegistry_ReturnsDefaultContainerRegistry()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var containerAppEnvironment = builder.AddAzureContainerAppEnvironment("env");

        // The environment should have a default container registry set up
        var registry = containerAppEnvironment.Resource.ContainerRegistry;
        Assert.NotNull(registry);
        Assert.IsType<AzureContainerRegistryResource>(registry);
    }

    [Fact]
    public void ContainerRegistry_PrefersExplicitContainerRegistry()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var acr = builder.AddAzureContainerRegistry("myacr");
        var containerAppEnvironment = builder.AddAzureContainerAppEnvironment("env")
            .WithAzureContainerRegistry(acr);

        // Should return the explicitly set registry
        var registry = containerAppEnvironment.Resource.ContainerRegistry;
        Assert.Same(acr.Resource, registry);
    }

    [Fact]
    public void ContainerRegistry_ReturnsNullWhenNoRegistryConfigured()
    {
        // Create an environment resource without the builder to avoid automatic registry setup
        var environment = new AzureContainerAppEnvironmentResource("env", _ => { });

        Assert.Null(environment.ContainerRegistry);
    }

    [Fact]
    public void ContainerRegistry_ThrowsWhenNonAzureRegistryConfigured()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var dockerRegistry = builder.AddContainerRegistry("docker-hub", "docker.io", "myuser");
        var containerAppEnvironment = builder.AddAzureContainerAppEnvironment("env")
            .WithContainerRegistry(dockerRegistry);

        // Should throw because a non-Azure registry is configured
        var exception = Assert.Throws<InvalidOperationException>(() => containerAppEnvironment.Resource.ContainerRegistry);
        Assert.Contains("not an Azure Container Registry", exception.Message);
        Assert.Contains("env", exception.Message);
    }

    [Fact]
    public async Task MultipleAzureContainerRegistries_WithoutExplicitRegistry_ThrowsException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Add two Azure Container Registries
        builder.AddAzureContainerRegistry("acr1");
        builder.AddAzureContainerRegistry("acr2");

        // Create Container App Environment without explicit registry
        var environment = builder.AddAzureContainerAppEnvironment("app-host");

        // Act & Assert - Building the app (which triggers infrastructure configuration) should throw
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(environment.Resource);
        });

        Assert.Contains("Azure Container App environment 'app-host' has multiple Azure Container Registries available", ex.Message);
        Assert.Contains("acr1", ex.Message);
        Assert.Contains("acr2", ex.Message);
        Assert.Contains("WithContainerRegistry", ex.Message);
    }

    [Fact]
    public async Task MultipleAzureContainerRegistries_WithExplicitRegistry_Succeeds()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Add two Azure Container Registries
        var acr1 = builder.AddAzureContainerRegistry("acr1");
        builder.AddAzureContainerRegistry("acr2");

        // Act - Creating Container App Environment with explicit registry should succeed
        var environment = builder.AddAzureContainerAppEnvironment("app-host")
            .WithContainerRegistry(acr1);

        // Assert - Should not throw when building
        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(environment.Resource);
        Assert.NotNull(manifest);
        Assert.NotNull(bicep);
    }
}
