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

public class AzureContainerRegistryTests
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

        var (manifest, bicep) = await GetManifestWithBicep(acr.Resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
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

        await Verify(rolesManifest.ToString(), "json")
              .AppendContentAsFile(rolesBicep, "bicep");
              
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
