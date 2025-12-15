// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE003 // Type is for evaluation purposes only and is subject to change or removal in future updates.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureAppServiceEnvironmentExtensionsTests
{
    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureAppServiceEnvironmentResource()
    {
        // Arrange
        var appServiceEnvironmentResource = new AzureAppServiceEnvironmentResource("test-app-service-env", _ => { });
        var infrastructure = new AzureResourceInfrastructure(appServiceEnvironmentResource, "test-app-service-env");

        // Act - Call AddAsExistingResource twice
        var firstResult = appServiceEnvironmentResource.AddAsExistingResource(infrastructure);
        var secondResult = appServiceEnvironmentResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public async Task AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureAppServiceEnvironmentResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var existingName = builder.AddParameter("existing-appenv-name");
        var existingResourceGroup = builder.AddParameter("existing-appenv-rg");

        var appServiceEnvironment = builder.AddAzureAppServiceEnvironment("test-app-service-env")
            .AsExisting(existingName, existingResourceGroup);

        var module = builder.AddAzureInfrastructure("mymodule", infra =>
        {
            _ = appServiceEnvironment.Resource.AddAsExistingResource(infra);
        });

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(module.Resource, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
             .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public void ContainerRegistry_ReturnsDefaultContainerRegistry()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var appServiceEnvironment = builder.AddAzureAppServiceEnvironment("env");

        // The environment should have a default container registry set up
        var registry = appServiceEnvironment.Resource.ContainerRegistry;
        Assert.NotNull(registry);
        Assert.IsType<AzureContainerRegistryResource>(registry);
    }

    [Fact]
    public void ContainerRegistry_PrefersExplicitContainerRegistry()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var acr = builder.AddAzureContainerRegistry("myacr");
        var appServiceEnvironment = builder.AddAzureAppServiceEnvironment("env")
            .WithAzureContainerRegistry(acr);

        // Should return the explicitly set registry
        var registry = appServiceEnvironment.Resource.ContainerRegistry;
        Assert.Same(acr.Resource, registry);
    }

    [Fact]
    public void ContainerRegistry_ReturnsNullWhenNoRegistryConfigured()
    {
        // Create an environment resource without the builder to avoid automatic registry setup
        var environment = new AzureAppServiceEnvironmentResource("env", _ => { });

        Assert.Null(environment.ContainerRegistry);
    }

    [Fact]
    public void ContainerRegistry_ThrowsWhenNonAzureRegistryConfigured()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var dockerRegistry = builder.AddContainerRegistry("docker-hub", "docker.io", "myuser");
        var appServiceEnvironment = builder.AddAzureAppServiceEnvironment("env")
            .WithContainerRegistry(dockerRegistry);

        // Should throw because a non-Azure registry is configured
        var exception = Assert.Throws<InvalidOperationException>(() => appServiceEnvironment.Resource.ContainerRegistry);
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

        // Create App Service Environment without explicit registry
        var environment = builder.AddAzureAppServiceEnvironment("app-service-env");

        // Act & Assert - Building the app (which triggers infrastructure configuration) should throw
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(environment.Resource);
        });

        Assert.Contains("Azure App Service environment 'app-service-env' has multiple Azure Container Registries available", ex.Message);
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

        // Act - Creating App Service Environment with explicit registry should succeed
        var environment = builder.AddAzureAppServiceEnvironment("app-service-env")
            .WithContainerRegistry(acr1);

        // Assert - Should not throw when building
        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(environment.Resource);
        Assert.NotNull(manifest);
        Assert.NotNull(bicep);
    }
}
