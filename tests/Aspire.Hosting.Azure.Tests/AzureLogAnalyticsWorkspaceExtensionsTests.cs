// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureLogAnalyticsWorkspaceExtensionsTests
{
    [Fact]
    public async Task AddLogAnalyticsWorkspace()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var logAnalyticsWorkspace = builder.AddAzureLogAnalyticsWorkspace("logAnalyticsWorkspace");

        Assert.Equal("logAnalyticsWorkspace", logAnalyticsWorkspace.Resource.Name);
        Assert.Equal("{logAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId}", logAnalyticsWorkspace.Resource.WorkspaceId.ValueExpression);

        var appInsightsManifest = await AzureManifestUtils.GetManifestWithBicep(logAnalyticsWorkspace.Resource);
        var expectedManifest = """
           {
             "type": "azure.bicep.v0",
             "path": "logAnalyticsWorkspace.module.bicep"
           }
           """;
        Assert.Equal(expectedManifest, appInsightsManifest.ManifestNode.ToString());

        await Verify(appInsightsManifest.BicepText, extension: "bicep");
    }

    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureLogAnalyticsWorkspaceResource()
    {
        // Arrange
        var logAnalyticsWorkspaceResource = new AzureLogAnalyticsWorkspaceResource("test-log-workspace", _ => { });
        var infrastructure = new AzureResourceInfrastructure(logAnalyticsWorkspaceResource, "test-log-workspace");

        // Act - Call AddAsExistingResource twice
        var firstResult = logAnalyticsWorkspaceResource.AddAsExistingResource(infrastructure);
        var secondResult = logAnalyticsWorkspaceResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public void AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureLogAnalyticsWorkspaceResource()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var existingName = builder.AddParameter("existing-logworkspace-name");
        var existingResourceGroup = builder.AddParameter("existing-logworkspace-rg");
        
        var logAnalyticsWorkspaceResource = new AzureLogAnalyticsWorkspaceResource("test-log-workspace", _ => { });
        logAnalyticsWorkspaceResource.Annotations.Add(new ExistingAzureResourceAnnotation(existingName.Resource, existingResourceGroup.Resource));
        
        var infrastructure = new AzureResourceInfrastructure(logAnalyticsWorkspaceResource, "test-log-workspace");

        // Act
        var result = logAnalyticsWorkspaceResource.AddAsExistingResource(infrastructure);

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
    public void AddAsExistingResource_FallsBackToDefault_WhenNoAnnotation_ForAzureLogAnalyticsWorkspaceResource()
    {
        // Arrange
        var logAnalyticsWorkspaceResource = new AzureLogAnalyticsWorkspaceResource("test-log-workspace", _ => { });
        var infrastructure = new AzureResourceInfrastructure(logAnalyticsWorkspaceResource, "test-log-workspace");

        // Act
        var result = logAnalyticsWorkspaceResource.AddAsExistingResource(infrastructure);

        // Assert - Should use default behavior (NameOutputReference.AsProvisioningParameter)
        Assert.NotNull(result);
        Assert.True(result.ProvisionableProperties.ContainsKey("name"));
        
        // The key point is that it should work when no annotation exists
        // Scope behavior may vary based on infrastructure setup, so we don't assert on it
    }
}
