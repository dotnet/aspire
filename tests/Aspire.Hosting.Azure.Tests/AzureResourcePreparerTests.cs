// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureResourcePreparerTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(DistributedApplicationOperation.Publish)]
    [InlineData(DistributedApplicationOperation.Run)]
    public async Task ThrowsExceptionsIfRoleAssignmentUnsupported(DistributedApplicationOperation operation)
    {
        using var builder = TestDistributedApplicationBuilder.Create(operation);

        var storage = builder.AddAzureStorage("storage");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataReader);

        var app = builder.Build();

        if (operation == DistributedApplicationOperation.Publish)
        {
            var ex = Assert.Throws<InvalidOperationException>(app.Start);
            Assert.Contains("role assignments", ex.Message);
        }
        else
        {
            await app.StartAsync();
            // no exception is thrown in Run mode
        }
    }

    [Theory]
    [InlineData(true, DistributedApplicationOperation.Run)]
    [InlineData(false, DistributedApplicationOperation.Run)]
    [InlineData(true, DistributedApplicationOperation.Publish)]
    [InlineData(false, DistributedApplicationOperation.Publish)]
    public async Task AppliesDefaultRoleAssignmentsInRunModeIfReferenced(bool addContainerAppsInfra, DistributedApplicationOperation operation)
    {
        using var builder = TestDistributedApplicationBuilder.Create(operation);
        if (addContainerAppsInfra)
        {
            builder.AddAzureContainerAppEnvironment("env");
        }

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(blobs);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.True(storage.Resource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultAssignments));

        if (!addContainerAppsInfra || operation == DistributedApplicationOperation.Run)
        {
            // when AzureContainerAppsInfrastructure is not added, we always apply the default role assignments to a new 'storage-roles' resource.
            // The same applies when in RunMode and we are provisioning Azure resources for F5 local development.
            var storageRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "storage-roles");

            var storageRolesManifest = await GetManifestWithBicep(storageRoles, skipPreparer: true);
            var expectedBicep = """
                @description('The location for the resource(s) to be deployed.')
                param location string = resourceGroup().location

                param storage_outputs_name string

                param principalType string

                param principalId string

                resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
                  name: storage_outputs_name
                }

                resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
                  properties: {
                    principalId: principalId
                    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                    principalType: principalType
                  }
                  scope: storage
                }

                resource storage_StorageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
                  properties: {
                    principalId: principalId
                    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
                    principalType: principalType
                  }
                  scope: storage
                }

                resource storage_StorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
                  properties: {
                    principalId: principalId
                    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
                    principalType: principalType
                  }
                  scope: storage
                }
                """;
            output.WriteLine(storageRolesManifest.BicepText);
            Assert.Equal(expectedBicep, storageRolesManifest.BicepText);
        }
        else
        {
            // in PublishMode when AzureContainerAppsInfrastructure is added, the DefaultRoleAssignmentsAnnotation
            // is copied to referencing resources' RoleAssignmentAnnotation.

            Assert.True(api.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var apiRoleAssignments));
            Assert.Equal(storage.Resource, apiRoleAssignments.Target);
            Assert.Equal(defaultAssignments.Roles, apiRoleAssignments.Roles);
        }
    }

    [Theory]
    [InlineData(DistributedApplicationOperation.Run)]
    [InlineData(DistributedApplicationOperation.Publish)]
    public async Task AppliesRoleAssignmentsInRunMode(DistributedApplicationOperation operation)
    {
        using var builder = TestDistributedApplicationBuilder.Create(operation);
        builder.AddAzureContainerAppEnvironment("env");

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDelegator, StorageBuiltInRole.StorageBlobDataReader)
            .WithReference(blobs);

        var api2 = builder.AddProject<Project>("api2", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataContributor)
            .WithReference(blobs);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        if (operation == DistributedApplicationOperation.Run)
        {
            // in RunMode, we apply the role assignments to a new 'storage-roles' resource, so the provisioned resource
            // adds these role assignments for F5 local development.
            var storageRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name is "storage-roles");

            var storageRolesManifest = await GetManifestWithBicep(storageRoles, skipPreparer: true);
            var expectedBicep = """
                @description('The location for the resource(s) to be deployed.')
                param location string = resourceGroup().location

                param storage_outputs_name string

                param principalType string

                param principalId string

                resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
                  name: storage_outputs_name
                }

                resource storage_StorageBlobDelegator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'db58b8e5-c6ad-4a2a-8342-4190687cbf4a'))
                  properties: {
                    principalId: principalId
                    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'db58b8e5-c6ad-4a2a-8342-4190687cbf4a')
                    principalType: principalType
                  }
                  scope: storage
                }

                resource storage_StorageBlobDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'))
                  properties: {
                    principalId: principalId
                    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
                    principalType: principalType
                  }
                  scope: storage
                }

                resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
                  properties: {
                    principalId: principalId
                    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
                    principalType: principalType
                  }
                  scope: storage
                }
                """;
            output.WriteLine(storageRolesManifest.BicepText);
            Assert.Equal(expectedBicep, storageRolesManifest.BicepText);
        }
        else
        {
            // in PublishMode, the role assignments are copied to the referencing resources' RoleAssignmentAnnotation.
            Assert.True(api.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var apiRoleAssignments));
            Assert.Equal(storage.Resource, apiRoleAssignments.Target);
            Assert.Collection(apiRoleAssignments.Roles,
                role => Assert.Equal(StorageBuiltInRole.StorageBlobDelegator.ToString(), role.Id),
                role => Assert.Equal(StorageBuiltInRole.StorageBlobDataReader.ToString(), role.Id));

            Assert.True(api2.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var api2RoleAssignments));
            Assert.Equal(storage.Resource, api2RoleAssignments.Target);
            Assert.Single(api2RoleAssignments.Roles,
                role => role.Id == StorageBuiltInRole.StorageBlobDataContributor.ToString());
        }
    }

    [Fact]
    public async Task FindsAzureReferencesFromArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        // the project doesn't WithReference or WithRoleAssignments, so it should get the default role assignments
        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithArgs(context =>
            {
                context.Args.Add("--azure-blobs");
                context.Args.Add(blobs.Resource.ConnectionStringExpression);
            });

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.True(storage.Resource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultAssignments));

        Assert.True(api.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var apiRoleAssignments));
        Assert.Equal(storage.Resource, apiRoleAssignments.Target);
        Assert.Equal(defaultAssignments.Roles, apiRoleAssignments.Roles);
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
