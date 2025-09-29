// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class AzureProvisioningResourceTests
{
    [Fact]
    public async Task PublishAsAzureContainerApp_CreatesAzureContainerAppResource()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        IResourceBuilder<ProjectResource>? apiProjectBuilder = null;
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // This callback should have access to the original resource
                // via the AzureContainerAppResource.TargetResource property
                Assert.IsType<AzureContainerAppResource>(infrastructure.AspireResource);
                var containerAppResource = (AzureContainerAppResource)infrastructure.AspireResource;
                
                Assert.NotNull(containerAppResource.TargetResource);
                Assert.Equal("api", containerAppResource.TargetResource.Name);
                Assert.Same(apiProjectBuilder!.Resource, containerAppResource.TargetResource);
            });
        
        apiProjectBuilder = apiProject;

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var project = Assert.Single(model.GetProjectResources());

        // Verify the deployment target was created
        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var provisioningResource = target?.DeploymentTarget as AzureContainerAppResource;
        Assert.NotNull(provisioningResource);

        // Verify the target resource is accessible
        Assert.NotNull(provisioningResource.TargetResource);
        Assert.Equal("api", provisioningResource.TargetResource.Name);
        Assert.Same(apiProject.Resource, provisioningResource.TargetResource);
    }

    [Fact]
    public async Task PublishAsAzureAppServiceWebsite_CreatesAzureWebSiteResource()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        IResourceBuilder<ProjectResource>? apiProjectBuilder = null;
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .PublishAsAzureAppServiceWebsite((infrastructure, website) =>
            {
                // This callback should have access to the original resource
                // via the AzureWebSiteResource.TargetResource property
                Assert.IsType<AzureWebSiteResource>(infrastructure.AspireResource);
                var webSiteResource = (AzureWebSiteResource)infrastructure.AspireResource;
                
                Assert.NotNull(webSiteResource.TargetResource);
                Assert.Equal("api", webSiteResource.TargetResource.Name);
                Assert.Same(apiProjectBuilder!.Resource, webSiteResource.TargetResource);
            });
        
        apiProjectBuilder = apiProject;

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var project = Assert.Single(model.GetProjectResources());

        // Verify the deployment target was created
        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var provisioningResource = target?.DeploymentTarget as AzureWebSiteResource;
        Assert.NotNull(provisioningResource);

        // Verify the target resource is accessible
        Assert.NotNull(provisioningResource.TargetResource);
        Assert.Equal("api", provisioningResource.TargetResource.Name);
        Assert.Same(apiProject.Resource, provisioningResource.TargetResource);
    }

    [Fact]
    public async Task ContainerResource_WithPublishAsContainerApp_CreatesAzureContainerAppResource()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        IResourceBuilder<ContainerResource>? containerBuilder = null;
        var container = builder.AddContainer("api", "myimage")
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // Verify we can access the original container resource
                Assert.IsType<AzureContainerAppResource>(infrastructure.AspireResource);
                var containerAppResource = (AzureContainerAppResource)infrastructure.AspireResource;
                
                Assert.NotNull(containerAppResource.TargetResource);
                Assert.Equal("api", containerAppResource.TargetResource.Name);
                Assert.Same(containerBuilder!.Resource, containerAppResource.TargetResource);
            });
        
        containerBuilder = container;

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResource = Assert.Single(model.GetContainerResources());

        // Verify the deployment target was created with the correct type
        containerResource.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var provisioningResource = target?.DeploymentTarget as AzureContainerAppResource;
        Assert.NotNull(provisioningResource);

        Assert.NotNull(provisioningResource.TargetResource);
        Assert.Equal("api", provisioningResource.TargetResource.Name);
        Assert.Same(container.Resource, provisioningResource.TargetResource);
    }

    [Fact]
    public async Task TargetResourceAllowsAccessToOriginalResourceAnnotations()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        // Add a custom annotation to the project
        var customAnnotation = new CustomTestAnnotation("test-value");
        
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithAnnotation(customAnnotation)
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // Access the original resource through the TargetResource property
                Assert.IsType<AzureContainerAppResource>(infrastructure.AspireResource);
                var containerAppResource = (AzureContainerAppResource)infrastructure.AspireResource;
                
                Assert.NotNull(containerAppResource.TargetResource);
                
                // Verify we can access annotations on the original resource
                var originalCustomAnnotation = containerAppResource.TargetResource.Annotations
                    .OfType<CustomTestAnnotation>()
                    .SingleOrDefault();
                    
                Assert.NotNull(originalCustomAnnotation);
                Assert.Equal("test-value", originalCustomAnnotation.Value);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);
    }

    [Fact]
    public void AzureContainerAppResource_StoresTargetResourceCorrectly()
    {
        var mockResource = new MockResource("test");
        var containerAppResource = new AzureContainerAppResource("test-app", _ => { }, mockResource);
        
        Assert.Same(mockResource, containerAppResource.TargetResource);
        Assert.Equal("test", containerAppResource.TargetResource.Name);
    }

    [Fact]
    public void AzureWebSiteResource_StoresTargetResourceCorrectly()
    {
        var mockResource = new MockResource("test");
        var webSiteResource = new AzureWebSiteResource("test-site", _ => { }, mockResource);
        
        Assert.Same(mockResource, webSiteResource.TargetResource);
        Assert.Equal("test", webSiteResource.TargetResource.Name);
    }

    [Fact]
    public void AzureContainerAppResource_InheritsFromAzureProvisioningResource()
    {
        var mockResource = new MockResource("test");
        var containerAppResource = new AzureContainerAppResource("test-app", _ => { }, mockResource);
        
        Assert.IsAssignableFrom<AzureProvisioningResource>(containerAppResource);
    }

    [Fact]
    public void AzureWebSiteResource_InheritsFromAzureProvisioningResource()
    {
        var mockResource = new MockResource("test");
        var webSiteResource = new AzureWebSiteResource("test-site", _ => { }, mockResource);
        
        Assert.IsAssignableFrom<AzureProvisioningResource>(webSiteResource);
    }

    private static async Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        var hooks = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        foreach (var hook in hooks)
        {
            await hook.BeforeStartAsync(appModel, cancellationToken).ConfigureAwait(false);
        }
    }

    // Test helper classes
    private sealed class CustomTestAnnotation(string value) : IResourceAnnotation
    {
        public string Value { get; } = value;
    }

    private sealed class MockResource(string name) : Resource(name), IResource
    {
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}