// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class DeploymentTargetParentAnnotationTests
{
    [Fact]
    public async Task PublishAsAzureContainerApp_AddsDeploymentTargetParentAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        IResourceBuilder<ProjectResource>? apiProjectBuilder = null;
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // This callback should have access to the parent resource
                // via the DeploymentTargetParentAnnotation on the infrastructure.AspireResource
                Assert.IsType<AzureProvisioningResource>(infrastructure.AspireResource);
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<DeploymentTargetParentAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                Assert.Equal("api", annotation.ParentResource.Name);
                Assert.Same(apiProjectBuilder!.Resource, annotation.ParentResource);
            });
        
        apiProjectBuilder = apiProject;

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var project = Assert.Single(model.GetProjectResources());

        // Verify the deployment target was created
        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var provisioningResource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(provisioningResource);

        // Verify the annotation exists on the AzureProvisioningResource
        var annotation = provisioningResource.Annotations
            .OfType<DeploymentTargetParentAnnotation>()
            .SingleOrDefault();
        
        Assert.NotNull(annotation);
        Assert.Equal("api", annotation.ParentResource.Name);
        Assert.Same(apiProject.Resource, annotation.ParentResource);
    }

    [Fact]
    public async Task PublishAsAzureAppServiceWebsite_AddsDeploymentTargetParentAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        IResourceBuilder<ProjectResource>? apiProjectBuilder = null;
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .PublishAsAzureAppServiceWebsite((infrastructure, website) =>
            {
                // This callback should have access to the parent resource
                // via the DeploymentTargetParentAnnotation on the infrastructure.AspireResource
                Assert.IsType<AzureProvisioningResource>(infrastructure.AspireResource);
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<DeploymentTargetParentAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                Assert.Equal("api", annotation.ParentResource.Name);
                Assert.Same(apiProjectBuilder!.Resource, annotation.ParentResource);
            });
        
        apiProjectBuilder = apiProject;

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var project = Assert.Single(model.GetProjectResources());

        // Verify the deployment target was created
        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var provisioningResource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(provisioningResource);

        // Verify the annotation exists on the AzureProvisioningResource
        var annotation = provisioningResource.Annotations
            .OfType<DeploymentTargetParentAnnotation>()
            .SingleOrDefault();
        
        Assert.NotNull(annotation);
        Assert.Equal("api", annotation.ParentResource.Name);
        Assert.Same(apiProject.Resource, annotation.ParentResource);
    }

    [Fact]
    public async Task ContainerResource_WithPublishAsContainerApp_AddsDeploymentTargetParentAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        IResourceBuilder<ContainerResource>? containerBuilder = null;
        var container = builder.AddContainer("api", "myimage")
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // Verify we can access the original container resource
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<DeploymentTargetParentAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                Assert.Equal("api", annotation.ParentResource.Name);
                Assert.Same(containerBuilder!.Resource, annotation.ParentResource);
            });
        
        containerBuilder = container;

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResource = Assert.Single(model.GetContainerResources());

        // Verify the deployment target was created with the annotation
        containerResource.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var provisioningResource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(provisioningResource);

        var annotation = provisioningResource.Annotations
            .OfType<DeploymentTargetParentAnnotation>()
            .SingleOrDefault();
        
        Assert.NotNull(annotation);
        Assert.Equal("api", annotation.ParentResource.Name);
        Assert.Same(container.Resource, annotation.ParentResource);
    }

    [Fact]
    public async Task AnnotationAllowsAccessToParentResourceAnnotations()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        // Add a custom annotation to the project
        var customAnnotation = new CustomTestAnnotation("test-value");
        
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithAnnotation(customAnnotation)
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // Access the parent resource through the annotation
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<DeploymentTargetParentAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                
                // Verify we can access annotations on the parent resource
                var originalCustomAnnotation = annotation.ParentResource.Annotations
                    .OfType<CustomTestAnnotation>()
                    .SingleOrDefault();
                    
                Assert.NotNull(originalCustomAnnotation);
                Assert.Equal("test-value", originalCustomAnnotation.Value);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);
    }

    [Fact]
    public void DeploymentTargetParentAnnotation_StoresResourceCorrectly()
    {
        var mockResource = new MockResource("test");
        var annotation = new DeploymentTargetParentAnnotation(mockResource);
        
        Assert.Same(mockResource, annotation.ParentResource);
        Assert.Equal("test", annotation.ParentResource.Name);
    }

    [Fact]
    public void DeploymentTargetParentAnnotation_ImplementsIResourceAnnotation()
    {
        var mockResource = new MockResource("test");
        var annotation = new DeploymentTargetParentAnnotation(mockResource);
        
        Assert.IsAssignableFrom<IResourceAnnotation>(annotation);
    }

    [Fact]
    public void DeploymentTargetParentAnnotation_IsSealed()
    {
        var type = typeof(DeploymentTargetParentAnnotation);
        Assert.True(type.IsSealed, "DeploymentTargetParentAnnotation should be sealed for performance");
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