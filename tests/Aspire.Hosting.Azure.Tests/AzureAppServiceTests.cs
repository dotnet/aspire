#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureAppServiceTests
{

    [Fact]
    public async Task AddContainerAppEnvironmentAddsDeploymentTargetWithContainerAppToProjectResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureAppServiceEnvironment("env");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints()
            .PublishAsAzureAppServiceWebsite((infrastructure, site) =>
            {
                site.SiteConfig.IsWebSocketsEnabled = true;
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.IsType<IComputeResource>(Assert.Single(model.GetProjectResources()), exactMatch: false);

        var target = container.GetDeploymentTargetAnnotation();

        Assert.NotNull(target);
        Assert.Same(env.Resource, target.ComputeEnvironment);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddContainerAppEnvironmentAddsEnvironmentResource()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var environment = Assert.Single(model.Resources.OfType<AzureAppServiceEnvironmentResource>());

        var (manifest, bicep) = await GetManifestWithBicep(environment);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task KeyvaultReferenceHandling()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        var db = builder.AddAzureCosmosDB("mydb").WithAccessKeyAuthentication();
        db.AddCosmosDatabase("db");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetProjectResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task EndpointReferencesAreResolvedAcrossProjects()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        // Add 2 projects with endpoints
        var project1 = builder.AddProject<Project>("project1", launchProfileName: null)
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints();

        var project2 = builder.AddProject<Project>("project2", launchProfileName: null)
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints()
            .WithReference(project1);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        project2.Resource.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AzureAppServiceSupportBaitAndSwitchResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        builder.AddProject<Project>("api", launchProfileName: null)
            .PublishAsDockerFile()
            .WithHttpEndpoint(env: "PORT", targetPort: 80)
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddDockerfileWithAppServiceInfrastructureAddsDeploymentTargetWithAppServiceToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        var directory = Directory.CreateTempSubdirectory(".aspire-test");

        // Contents of the Dockerfile are not important for this test
        File.WriteAllText(Path.Combine(directory.FullName, "Dockerfile"), "");

        builder.AddDockerfile("api", directory.FullName)
               .WithHttpEndpoint(targetPort: 85, env: "PORT")
               .WithExternalHttpEndpoints();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task UnknownManifestExpressionProviderIsHandledWithAllocateParameter()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        var customProvider = new CustomManifestExpressionProvider();
        
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["CUSTOM_VALUE"] = customProvider;
            })
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var project = Assert.Single(model.GetProjectResources());

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public void AzureAppServiceEnvironmentImplementsIAzureComputeEnvironmentResource()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var env = builder.AddAzureAppServiceEnvironment("env");

        Assert.IsAssignableFrom<IAzureComputeEnvironmentResource>(env.Resource);
        Assert.IsAssignableFrom<IComputeEnvironmentResource>(env.Resource);
    }

    private static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource) =>
        AzureManifestUtils.GetManifestWithBicep(resource, skipPreparer: true);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "/foo/bar/project.csproj";
    }

    private sealed class CustomManifestExpressionProvider : IManifestExpressionProvider
    {
        public string ValueExpression => "{customValue}";
    }
}
