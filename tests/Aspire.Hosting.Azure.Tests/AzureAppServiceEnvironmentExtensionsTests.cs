// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}