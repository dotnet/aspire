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

public class AzureUserAssignedIdentityTests
{
    [Fact]
    public async Task AddAzureUserAssignedIdentity_GeneratesExpectedResourcesAndBicep()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureUserAssignedIdentity("myidentity");

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act
        var resource = Assert.Single(model.Resources.OfType<AzureUserAssignedIdentityResource>());

        var (_, bicep) = await GetManifestWithBicep(resource);

        await Verifier.Verify(bicep, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots")
            .AutoVerify();
    }

    [Fact]
    public async Task AddAzureUserAssignedIdentity_PublishAsExisting_Works()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureUserAssignedIdentity("myidentity")
               .PublishAsExisting("existingidentity", "my-rg");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(model.Resources.OfType<AzureUserAssignedIdentityResource>());

        var (_, bicep) = await GetManifestWithBicep(resource);

        await Verifier.Verify(bicep, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots")
            .AutoVerify();
    }

    [Fact]
    public async Task AddAzureUserAssignedIdentity_WithRoleAssignments_Works()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("cae");

        var registry = builder.AddAzureContainerRegistry("myregistry");
        builder.AddAzureUserAssignedIdentity("myidentity")
            .WithRoleAssignments(registry, [ContainerRegistryBuiltInRole.AcrPush]);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.Collection(model.Resources.OrderBy(r => r.Name),
            r => Assert.IsType<AzureContainerAppEnvironmentResource>(r),
            r => Assert.IsType<AzureUserAssignedIdentityResource>(r),
            r =>
            {
                Assert.IsType<AzureProvisioningResource>(r);
                Assert.Equal("myidentity-roles-myregistry", r.Name);
            },
            r => Assert.IsType<AzureContainerRegistryResource>(r));

        var identityResource = Assert.Single(model.Resources.OfType<AzureUserAssignedIdentityResource>());
        var (_, identityBicep) = await GetManifestWithBicep(identityResource, skipPreparer: true);

        var registryResource = Assert.Single(model.Resources.OfType<AzureContainerRegistryResource>());
        var (_, registryBicep) = await GetManifestWithBicep(registryResource, skipPreparer: true);

        var identityRoleAssignments = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "myidentity-roles-myregistry");
        var (_, identityRoleAssignmentsBicep) = await GetManifestWithBicep(identityRoleAssignments, skipPreparer: true);

        Target[] targets = [
            new Target("bicep", identityBicep),
            new Target("bicep", registryBicep),
            new Target("bicep", identityRoleAssignmentsBicep)
        ];
        await Verifier.Verify(targets)
            .UseHelixAwareDirectory("Snapshots")
            .AutoVerify();
    }
}
