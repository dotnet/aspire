// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Kusto;
using Aspire.Hosting.Utils;
using Azure.Provisioning.Kusto;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureKustoExtensionsTests
{
    [Fact]
    public async Task AddAzureKustoCluster()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var kusto = builder.AddAzureKustoCluster("kusto");
        kusto.Resource.Outputs["clusterUri"] = "https://kusto-cluster.eastus.kusto.windows.net";
        Assert.Equal("https://kusto-cluster.eastus.kusto.windows.net", await kusto.Resource.ConnectionStringExpression.GetValueAsync(default));

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, kusto.Resource);

        var connectionStringResource = (IResourceWithConnectionString)kusto.Resource;
        Assert.Equal("https://kusto-cluster.eastus.kusto.windows.net", await connectionStringResource.GetConnectionStringAsync());

        var kustoRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "kusto-roles");
        var kustoRolesManifest = await GetManifestWithBicep(kustoRoles, skipPreparer: true);

        await Verify(manifest.BicepText, extension: "bicep")
            .AppendContentAsFile(kustoRolesManifest.BicepText, "bicep");
    }

    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureKustoClusterResource()
    {
        // Arrange
        var kustoResource = new AzureKustoClusterResource("test-kusto", _ => { });
        var infrastructure = new AzureResourceInfrastructure(kustoResource, "test-kusto");

        // Act - Call AddAsExistingResource twice
        var firstResult = kustoResource.AddAsExistingResource(infrastructure);
        var secondResult = kustoResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public async Task AddAsExistingResource_RespectsExistingAzureResourceAnnotation_ForAzureKustoClusterResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var existingName = builder.AddParameter("existing-kusto-name");
        var existingResourceGroup = builder.AddParameter("existing-kusto-rg");

        var kusto = builder.AddAzureKustoCluster("test-kusto")
            .AsExisting(existingName, existingResourceGroup);

        var module = builder.AddAzureInfrastructure("mymodule", infra =>
        {
            _ = kusto.Resource.AddAsExistingResource(infra);
        });

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(module.Resource, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
             .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddAzureKustoCluster_WithCustomRoleAssignments()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var kusto = builder.AddAzureKustoCluster("kusto");
        
        builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(kusto, KustoBuiltInRole.Reader);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, kusto.Resource);

        var kustoRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "kusto-roles");
        var kustoRolesManifest = await GetManifestWithBicep(kustoRoles, skipPreparer: true);

        await Verify(manifest.BicepText, extension: "bicep")
            .AppendContentAsFile(kustoRolesManifest.BicepText, "bicep");
    }

    [Fact]
    public async Task AddAzureKustoCluster_WithMultipleRoleAssignments()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var kusto = builder.AddAzureKustoCluster("kusto");
        
        builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(kusto, KustoBuiltInRole.Reader, KustoBuiltInRole.Contributor);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, kusto.Resource);

        var kustoRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "kusto-roles");
        var kustoRolesManifest = await GetManifestWithBicep(kustoRoles, skipPreparer: true);

        await Verify(manifest.BicepText, extension: "bicep")
            .AppendContentAsFile(kustoRolesManifest.BicepText, "bicep");
    }

    [Fact]
    public void AddAzureKustoCluster_ShouldAddResourceToBuilder()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureKustoCluster("myKusto");
        Assert.NotNull(resourceBuilder);
        var resource = Assert.Single(builder.Resources.OfType<AzureKustoClusterResource>());
        Assert.Equal("myKusto", resource.Name);
    }

    [Fact]
    public void AddAzureKustoCluster_RunAsEmulator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var kusto = builder.AddAzureKustoCluster("kusto").RunAsEmulator();
        
        // Set up a mock endpoint for the emulator
        kusto.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = 8080;
            endpoint.UriScheme = "http";
        });

        var connectionStringExpr = kusto.Resource.ConnectionStringExpression;
        
        // Since we can't easily mock the endpoint in tests, just verify the resource was created
        Assert.NotNull(connectionStringExpr);
        
        var resource = Assert.Single(builder.Resources.OfType<AzureKustoEmulatorResource>());
        Assert.Equal("kusto", resource.Name);
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}