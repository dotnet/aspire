// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class TargetComputeResourceAnnotationTests
{
    [Fact]
    public async Task PublishAsAzureContainerApp_AddsTargetComputeResourceAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        IResourceBuilder<ProjectResource>? apiProjectBuilder = null;
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // This callback should have access to the original compute resource
                // via the TargetComputeResourceAnnotation on the infrastructure.AspireResource
                Assert.IsType<AzureProvisioningResource>(infrastructure.AspireResource);
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<TargetComputeResourceAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                Assert.Equal("api", annotation.ComputeResource.Name);
                Assert.Same(apiProjectBuilder!.Resource, annotation.ComputeResource);
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
            .OfType<TargetComputeResourceAnnotation>()
            .SingleOrDefault();
        
        Assert.NotNull(annotation);
        Assert.Equal("api", annotation.ComputeResource.Name);
        Assert.Same(apiProject.Resource, annotation.ComputeResource);
    }

    [Fact]
    public async Task PublishAsAzureAppServiceWebsite_AddsTargetComputeResourceAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureAppServiceEnvironment("env");

        IResourceBuilder<ProjectResource>? apiProjectBuilder = null;
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .PublishAsAzureAppServiceWebsite((infrastructure, website) =>
            {
                // This callback should have access to the original compute resource
                // via the TargetComputeResourceAnnotation on the infrastructure.AspireResource
                Assert.IsType<AzureProvisioningResource>(infrastructure.AspireResource);
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<TargetComputeResourceAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                Assert.Equal("api", annotation.ComputeResource.Name);
                Assert.Same(apiProjectBuilder!.Resource, annotation.ComputeResource);
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
            .OfType<TargetComputeResourceAnnotation>()
            .SingleOrDefault();
        
        Assert.NotNull(annotation);
        Assert.Equal("api", annotation.ComputeResource.Name);
        Assert.Same(apiProject.Resource, annotation.ComputeResource);
    }

    [Fact]
    public async Task ContainerResource_WithPublishAsContainerApp_AddsTargetComputeResourceAnnotation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        IResourceBuilder<ContainerResource>? containerBuilder = null;
        var container = builder.AddContainer("api", "myimage")
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // Verify we can access the original container resource
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<TargetComputeResourceAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                Assert.Equal("api", annotation.ComputeResource.Name);
                Assert.Same(containerBuilder!.Resource, annotation.ComputeResource);
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
            .OfType<TargetComputeResourceAnnotation>()
            .SingleOrDefault();
        
        Assert.NotNull(annotation);
        Assert.Equal("api", annotation.ComputeResource.Name);
        Assert.Same(container.Resource, annotation.ComputeResource);
    }

    [Fact]
    public async Task AnnotationAllowsAccessToComputeResourceAnnotations()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        // Add a custom annotation to the project
        var customAnnotation = new CustomTestAnnotation("test-value");
        
        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithAnnotation(customAnnotation)
            .PublishAsAzureContainerApp((infrastructure, containerApp) =>
            {
                // Access the original compute resource through the annotation
                var annotation = infrastructure.AspireResource.Annotations
                    .OfType<TargetComputeResourceAnnotation>()
                    .SingleOrDefault();
                
                Assert.NotNull(annotation);
                
                // Verify we can access annotations on the original compute resource
                var originalCustomAnnotation = annotation.ComputeResource.Annotations
                    .OfType<CustomTestAnnotation>()
                    .SingleOrDefault();
                    
                Assert.NotNull(originalCustomAnnotation);
                Assert.Equal("test-value", originalCustomAnnotation.Value);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);
    }

    [Fact]
    public void TargetComputeResourceAnnotation_StoresResourceCorrectly()
    {
        var mockResource = new MockResource("test");
        var annotation = new TargetComputeResourceAnnotation(mockResource);
        
        Assert.Same(mockResource, annotation.ComputeResource);
        Assert.Equal("test", annotation.ComputeResource.Name);
    }

    [Fact]
    public void TargetComputeResourceAnnotation_ImplementsIResourceAnnotation()
    {
        var mockResource = new MockResource("test");
        var annotation = new TargetComputeResourceAnnotation(mockResource);
        
        Assert.IsAssignableFrom<IResourceAnnotation>(annotation);
    }

    [Fact]
    public void TargetComputeResourceAnnotation_IsSealed()
    {
        var type = typeof(TargetComputeResourceAnnotation);
        Assert.True(type.IsSealed, "TargetComputeResourceAnnotation should be sealed for performance");
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