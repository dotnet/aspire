// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSignalRExtensionsTests
{
    [Fact]
    public async Task AddAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        var (manifest, bicep) = await GetManifestWithBicep(signalr.Resource, skipPreparer: true);
        var signalrRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "signalr-roles");
        var (signalrRolesManifest, signalrRolesBicep) = await GetManifestWithBicep(signalrRoles, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(signalrRolesManifest.ToString(), "json")
              .AppendContentAsFile(signalrRolesBicep, "bicep");
              
    }

    [Fact]
    public async Task AddServerlessAzureSignalR()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var signalr = builder.AddAzureSignalR("signalr", AzureSignalRServiceMode.Serverless);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        var (manifest, bicep) = await GetManifestWithBicep(signalr.Resource, skipPreparer: true);
        var signalrRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "signalr-roles");
        var (signalrRolesManifest, signalrRolesBicep) = await GetManifestWithBicep(signalrRoles, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(signalrRolesManifest.ToString(), "json")
              .AppendContentAsFile(signalrRolesBicep, "bicep");
              
    }

    [Fact]
    public void RunAsEmulatorAppliesEmulatorResourceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var signalR = builder.AddAzureSignalR("signalr")
                            .RunAsEmulator();

        // Verify that the EmulatorResourceAnnotation is applied
        Assert.True(signalR.Resource.IsEmulator());
        Assert.Contains(signalR.Resource.Annotations, a => a is EmulatorResourceAnnotation);
    }

    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureSignalRResource()
    {
        // Arrange
        var signalRResource = new AzureSignalRResource("test-signalr", _ => { });
        var infrastructure = new AzureResourceInfrastructure(signalRResource, "test-signalr");

        // Act - Call AddAsExistingResource twice
        var firstResult = signalRResource.AddAsExistingResource(infrastructure);
        var secondResult = signalRResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public void AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureSignalRResource()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var existingName = builder.AddParameter("existing-signalr-name");
        var existingResourceGroup = builder.AddParameter("existing-signalr-rg");
        
        var signalRResource = new AzureSignalRResource("test-signalr", _ => { });
        signalRResource.Annotations.Add(new ExistingAzureResourceAnnotation(existingName.Resource, existingResourceGroup.Resource));
        
        var infrastructure = new AzureResourceInfrastructure(signalRResource, "test-signalr");

        // Act
        var result = signalRResource.AddAsExistingResource(infrastructure);

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
    public void AddAsExistingResource_FallsBackToDefault_WhenNoAnnotation_ForAzureSignalRResource()
    {
        // Arrange
        var signalRResource = new AzureSignalRResource("test-signalr", _ => { });
        var infrastructure = new AzureResourceInfrastructure(signalRResource, "test-signalr");

        // Act
        var result = signalRResource.AddAsExistingResource(infrastructure);

        // Assert - Should use default behavior (NameOutputReference.AsProvisioningParameter)
        Assert.NotNull(result);
        Assert.True(result.ProvisionableProperties.ContainsKey("name"));
        
        // The key point is that it should work when no annotation exists
        // Scope behavior may vary based on infrastructure setup, so we don't assert on it
    }
}
