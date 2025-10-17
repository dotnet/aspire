// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class OperatorRoleAssignmentTests
{
    [Fact]
    public async Task WithOperatorAddsOperatorPrincipalAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var adminGroupObjectId = builder.AddParameter("adminGroupObjectId");
        var env = builder.AddAzureContainerAppEnvironment("env")
            .WithOperator(adminGroupObjectId);

        Assert.True(env.Resource.TryGetLastAnnotation<OperatorPrincipalAnnotation>(out var annotation));
        Assert.NotNull(annotation);
        Assert.Same(adminGroupObjectId.Resource, annotation.PrincipalId);
    }

    [Fact]
    public async Task AzureStorageGetsOperatorRoleCallback()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var storage = builder.AddAzureStorage("storage");

        Assert.True(storage.Resource.TryGetAnnotationsOfType<OperatorRoleCallbackAnnotation>(out var annotations));
        var annotation = annotations.Single();
        Assert.NotNull(annotation);
        Assert.NotEmpty(annotation.Roles);

        // Should have StorageAccountContributor role for operators
        var hasStorageAccountContributorRole = annotation.Roles.Any(r =>
            r.Name == StorageBuiltInRole.GetBuiltInRoleName(StorageBuiltInRole.StorageAccountContributor));
        Assert.True(hasStorageAccountContributorRole);
    }

    [Fact]
    public async Task OperatorRoleAssignmentsAreCreatedForStorageWhenOperatorIsSpecified()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var adminGroupObjectId = builder.AddParameter("adminGroupObjectId");
        builder.AddAzureContainerAppEnvironment("env")
            .WithOperator(adminGroupObjectId);

        var storage = builder.AddAzureStorage("storage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Find the operator role assignment resource
        var operatorRoleResource = model.Resources
            .OfType<AzureProvisioningResource>()
            .FirstOrDefault(r => r.Name.Contains("storage-operator"));

        Assert.NotNull(operatorRoleResource);
    }

    [Fact]
    public async Task OperatorRoleAssignmentGeneratesCorrectBicep()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var adminGroupObjectId = builder.AddParameter("adminGroupObjectId");
        builder.AddAzureContainerAppEnvironment("env")
            .WithOperator(adminGroupObjectId);

        var storage = builder.AddAzureStorage("storage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Find the operator role assignment resource
        var operatorRoleResource = model.Resources
            .OfType<AzureProvisioningResource>()
            .FirstOrDefault(r => r.Name.Contains("storage-operator"));

        Assert.NotNull(operatorRoleResource);

        var (manifest, bicep) = await GetManifestWithBicep(operatorRoleResource);

        // Verify the bicep contains role assignment
        Assert.Contains("Microsoft.Authorization/roleAssignments", bicep);

        await Verify(manifest.ToString(), "json")
            .UseMethodName($"{nameof(OperatorRoleAssignmentGeneratesCorrectBicep)}")
            .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task MultipleOperatorsCreateMultipleRoleAssignments()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var admin1 = builder.AddParameter("admin1");
        var admin2 = builder.AddParameter("admin2");

        var env = builder.AddAzureContainerAppEnvironment("env")
            .WithOperator(admin1)
            .WithOperator(admin2);

        var storage = builder.AddAzureStorage("storage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Find operator role assignment resources
        var operatorRoleResources = model.Resources
            .OfType<AzureProvisioningResource>()
            .Where(r => r.Name.Contains("storage-operator"))
            .ToList();

        // Should have 2 operator role resources (one for each operator)
        Assert.Equal(2, operatorRoleResources.Count);
    }

    [Fact]
    public async Task OperatorRolesAreNotCreatedWhenNoOperatorIsSpecified()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var storage = builder.AddAzureStorage("storage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Find operator role assignment resources
        var operatorRoleResources = model.Resources
            .OfType<AzureProvisioningResource>()
            .Where(r => r.Name.Contains("storage-operator"))
            .ToList();

        // Should have 0 operator role resources
        Assert.Empty(operatorRoleResources);
    }
}
