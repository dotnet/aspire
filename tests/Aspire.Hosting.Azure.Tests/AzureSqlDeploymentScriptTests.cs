// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003

using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSqlDeploymentScriptTests
{
    [Fact]
    public async Task SqlWithPrivateEndpoint_AutoCreatesBothSubnetAndStorage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var peSubnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");

        var sqlServer = builder.AddAzureSqlServer("sql");
        var db = sqlServer.AddDatabase("db");

        peSubnet.AddPrivateEndpoint(sqlServer);

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        await VerifyAllAzureBicep(builder);
    }

    [Fact]
    public async Task SqlWithPrivateEndpoint_ExplicitSubnet_AutoCreatesStorage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var peSubnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var aciSubnet = vnet.AddSubnet("acisubnet", "10.0.2.0/29");

        var sqlServer = builder.AddAzureSqlServer("sql");
        var db = sqlServer.AddDatabase("db");

        peSubnet.AddPrivateEndpoint(sqlServer);
        sqlServer.WithAdminDeploymentScriptSubnet(aciSubnet);

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        await VerifyAllAzureBicep(builder);
    }

    [Fact]
    public async Task SqlWithPrivateEndpoint_ExplicitStorage_AutoCreatesSubnet()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var peSubnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");

        var sqlServer = builder.AddAzureSqlServer("sql");
        var db = sqlServer.AddDatabase("db");

        peSubnet.AddPrivateEndpoint(sqlServer);

        var storage = builder.AddAzureStorage("depscriptstorage");
        sqlServer.WithAdminDeploymentScriptStorage(storage);

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        await VerifyAllAzureBicep(builder);
    }

    [Fact]
    public async Task SqlWithPrivateEndpoint_BothExplicitSubnetAndStorage()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var peSubnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var aciSubnet = vnet.AddSubnet("acisubnet", "10.0.2.0/29");

        var sqlServer = builder.AddAzureSqlServer("sql");
        var db = sqlServer.AddDatabase("db");

        peSubnet.AddPrivateEndpoint(sqlServer);
        sqlServer.WithAdminDeploymentScriptSubnet(aciSubnet);

        var storage = builder.AddAzureStorage("depscriptstorage");
        sqlServer.WithAdminDeploymentScriptStorage(storage);

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        await VerifyAllAzureBicep(builder);
    }

    [Fact]
    public async Task SqlWithPrivateEndpoint_StorageBeforePrivateEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var peSubnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");

        var sqlServer = builder.AddAzureSqlServer("sql");
        var db = sqlServer.AddDatabase("db");

        // Call WithAdminDeploymentScriptStorage BEFORE AddPrivateEndpoint
        var storage = builder.AddAzureStorage("depscriptstorage");
        sqlServer.WithAdminDeploymentScriptStorage(storage);

        peSubnet.AddPrivateEndpoint(sqlServer);

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        await VerifyAllAzureBicep(builder);
    }

    [Fact]
    public async Task SqlWithPrivateEndpoint_SubnetBeforePrivateEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var peSubnet = vnet.AddSubnet("pesubnet", "10.0.1.0/24");
        var aciSubnet = vnet.AddSubnet("acisubnet", "10.0.2.0/29");

        var sqlServer = builder.AddAzureSqlServer("sql");
        var db = sqlServer.AddDatabase("db");

        // Call WithAdminDeploymentScriptSubnet BEFORE AddPrivateEndpoint
        sqlServer.WithAdminDeploymentScriptSubnet(aciSubnet);

        peSubnet.AddPrivateEndpoint(sqlServer);

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        await VerifyAllAzureBicep(builder);
    }

    private static async Task VerifyAllAzureBicep(IDistributedApplicationBuilder builder)
    {
        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        // Collect bicep for all Azure provisioning resources, ordered by name for deterministic output
        var azureResources = model.Resources
            .OfType<AzureProvisioningResource>()
            .OrderBy(r => r.Name)
            .ToList();

        var sb = new StringBuilder();

        foreach (var resource in azureResources)
        {
            var bicep = await GetBicep(resource);

            sb.AppendLine($"// Resource: {resource.Name}");
            sb.AppendLine(bicep);
            sb.AppendLine();
        }

        await Verify(sb.ToString(), extension: "bicep")
            .ScrubLinesWithReplace(s => s.Replace("\\r\\n", "\\n"));
    }

    private static async Task<string> GetBicep(IResource resource)
    {
        var (_, bicep) = await AzureManifestUtils.GetManifestWithBicep(resource, skipPreparer: true);
        return bicep;
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}

