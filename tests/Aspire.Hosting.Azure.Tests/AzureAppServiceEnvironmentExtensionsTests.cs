// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
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
    public void AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureAppServiceEnvironmentResource()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var existingName = builder.AddParameter("existing-appenv-name");
        var existingResourceGroup = builder.AddParameter("existing-appenv-rg");
        
        var appServiceEnvironmentResource = new AzureAppServiceEnvironmentResource("test-app-service-env", _ => { });
        appServiceEnvironmentResource.Annotations.Add(new ExistingAzureResourceAnnotation(existingName.Resource, existingResourceGroup.Resource));
        
        var infrastructure = new AzureResourceInfrastructure(appServiceEnvironmentResource, "test-app-service-env");

        // Act
        var result = appServiceEnvironmentResource.AddAsExistingResource(infrastructure);

        // Assert - The resource should use the name from the annotation, not the default
        Assert.NotNull(result);
        Assert.True(result.ProvisionableProperties.ContainsKey("name"));
        var nameProperty = result.ProvisionableProperties["name"];
        Assert.NotNull(nameProperty);
        
        // Verify that scope is set due to different resource group
        Assert.True(result.ProvisionableProperties.ContainsKey("scope"));
        var scopeProperty = result.ProvisionableProperties["scope"];
        Assert.NotNull(scopeProperty);
    }

    [Fact]
    public void AddAsExistingResource_FallsBackToDefault_WhenNoAnnotation_ForAzureAppServiceEnvironmentResource()
    {
        // Arrange
        var appServiceEnvironmentResource = new AzureAppServiceEnvironmentResource("test-app-service-env", _ => { });
        var infrastructure = new AzureResourceInfrastructure(appServiceEnvironmentResource, "test-app-service-env");

        // Act
        var result = appServiceEnvironmentResource.AddAsExistingResource(infrastructure);

        // Assert - Should use default behavior (NameOutputReference.AsProvisioningParameter)
        Assert.NotNull(result);
        Assert.True(result.ProvisionableProperties.ContainsKey("name"));
        
        // The key point is that it should work when no annotation exists
        // Scope behavior may vary based on infrastructure setup, so we don't assert on it
    }
}