// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.Hosting;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureResourcePreparerTests
{
    [Fact]
    public void ThrowsExceptionsIfRoleAssignmentUnsupported()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var storage = builder.AddAzureStorage("storage");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataReader);

        var app = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(app.Start);
        Assert.Contains("role assignments", ex.Message);
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
            builder.AddAzureContainerAppsInfrastructure();
        }

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(blobs);

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.True(storage.Resource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultAssignments));

        if (!addContainerAppsInfra || operation == DistributedApplicationOperation.Run)
        {
            // when AzureContainerAppsInfrastructure is not added, we always apply the default role assignments to AppliedRoleAssignmentsAnnotation.
            // The same applies when in RunMode and we are provisioning Azure resources for F5 local development.

            Assert.True(storage.Resource.TryGetLastAnnotation<AppliedRoleAssignmentsAnnotation>(out var appliedAssignments));
            Assert.Equal(defaultAssignments.Roles, appliedAssignments.Roles);
        }
        else
        {
            // in PublishMode when AzureContainerAppsInfrastructure is added, we don't use AppliedRoleAssignmentsAnnotation.
            // Instead, the DefaultRoleAssignmentsAnnotation is copied to referencing resources' RoleAssignmentAnnotation.

            Assert.False(storage.Resource.HasAnnotationOfType<AppliedRoleAssignmentsAnnotation>());

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
        builder.AddAzureContainerAppsInfrastructure();

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobs("blobs");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDelegator, StorageBuiltInRole.StorageBlobDataReader)
            .WithReference(blobs);

        var api2 = builder.AddProject<Project>("api2", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataContributor)
            .WithReference(blobs);

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        if (operation == DistributedApplicationOperation.Run)
        {
            // in RunMode, we apply the role assignments to AppliedRoleAssignmentsAnnotation, so the provisioned resource
            // adds these role assignments for F5 local development.

            Assert.True(storage.Resource.TryGetLastAnnotation<AppliedRoleAssignmentsAnnotation>(out var appliedAssignments));

            Assert.Collection(appliedAssignments.Roles,
                role => Assert.Equal(StorageBuiltInRole.StorageBlobDelegator.ToString(), role.Id),
                role => Assert.Equal(StorageBuiltInRole.StorageBlobDataReader.ToString(), role.Id),
                role => Assert.Equal(StorageBuiltInRole.StorageBlobDataContributor.ToString(), role.Id));
        }
        else
        {
            // in PublishMode, we don't use AppliedRoleAssignmentsAnnotation.
            Assert.False(storage.Resource.HasAnnotationOfType<AppliedRoleAssignmentsAnnotation>());

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
        builder.AddAzureContainerAppsInfrastructure();

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
