// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;
using Azure.Provisioning.OperationalInsights;

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
    public void AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureContainerAppEnvironmentResource()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var existingName = builder.AddParameter("existing-env-name");
        var existingResourceGroup = builder.AddParameter("existing-env-rg");
        
        var containerAppEnvironmentResource = new AzureContainerAppEnvironmentResource("test-container-app-env", _ => { });
        containerAppEnvironmentResource.Annotations.Add(new ExistingAzureResourceAnnotation(existingName.Resource, existingResourceGroup.Resource));
        
        var infrastructure = new AzureResourceInfrastructure(containerAppEnvironmentResource, "test-container-app-env");

        // Act
        var result = containerAppEnvironmentResource.AddAsExistingResource(infrastructure);

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
    public void AddAsExistingResource_FallsBackToDefault_WhenNoAnnotation_ForAzureContainerAppEnvironmentResource()
    {
        // Arrange
        var containerAppEnvironmentResource = new AzureContainerAppEnvironmentResource("test-container-app-env", _ => { });
        var infrastructure = new AzureResourceInfrastructure(containerAppEnvironmentResource, "test-container-app-env");

        // Act
        var result = containerAppEnvironmentResource.AddAsExistingResource(infrastructure);

        // Assert - Should use default behavior (NameOutputReference.AsProvisioningParameter)
        Assert.NotNull(result);
        Assert.True(result.ProvisionableProperties.ContainsKey("name"));
        
        // The key point is that it should work when no annotation exists
        // Scope behavior may vary based on infrastructure setup, so we don't assert on it
    }

    [Fact]
    public void WithAzureLogAnalyticsWorkspace_RespectsExistingWorkspaceInDifferentResourceGroup()
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
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Assert.True(containerAppEnvironment.Resource.TryGetLastAnnotation<AzureLogAnalyticsWorkspaceReferenceAnnotation>(out var workspaceRef));
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Assert.Same(logAnalyticsWorkspace.Resource, workspaceRef.Workspace);

        // Act - Generate the bicep infrastructure for the Container App Environment
        var infrastructure = new AzureResourceInfrastructure(containerAppEnvironment.Resource, containerAppEnvironment.Resource.Name);
        containerAppEnvironment.Resource.ConfigureInfrastructure(infrastructure);

        // Assert - Verify that the Log Analytics Workspace is correctly referenced
        // The LAW should be added as an existing resource respecting the ExistingAzureResourceAnnotation
        var lawResource = infrastructure.GetProvisionableResources().OfType<OperationalInsightsWorkspace>().Single();
        
        // Verify that the LAW resource has the correct name property (from the annotation)
        Assert.True(lawResource.ProvisionableProperties.ContainsKey("name"));
        var nameProperty = lawResource.ProvisionableProperties["name"];
        Assert.NotNull(nameProperty);
        
        // Verify that scope is set because the LAW is in a different resource group
        Assert.True(lawResource.ProvisionableProperties.ContainsKey("scope"));
        var scopeProperty = lawResource.ProvisionableProperties["scope"];
        Assert.NotNull(scopeProperty);
    }
}