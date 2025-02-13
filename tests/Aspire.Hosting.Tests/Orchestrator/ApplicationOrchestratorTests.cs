// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Orchestrator;

public class ApplicationOrchestratorTests
{
    [Fact]
    public async Task ParentPropertySetOnChildResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var parentResource = builder.AddContainer("database", "image");
        var childResource = builder.AddResource(new CustomChildResource("child", parentResource.Resource));

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var events = new DcpExecutorEvents();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var appOrchestrator = CreateOrchestrator(distributedAppModel, notificationService: resourceNotificationService, dcpEvents: events);
        await appOrchestrator.RunApplicationAsync();

        string? parentResourceId = null;
        string? childParentResourceId = null;
        var watchResourceTask = Task.Run(async () =>
        {
            await foreach (var item in resourceNotificationService.WatchAsync())
            {
                if (item.Resource == parentResource.Resource)
                {
                    parentResourceId = item.ResourceId;
                }
                else if (item.Resource == childResource.Resource)
                {
                    childParentResourceId = item.Snapshot.Properties.SingleOrDefault(p => p.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                }

                if (parentResourceId != null && childParentResourceId != null)
                {
                    return;
                }
            }
        });

        await events.PublishAsync(new OnResourceStartingContext(CancellationToken.None, KnownResourceTypes.Container, parentResource.Resource, parentResource.Resource.Name));

        await watchResourceTask.DefaultTimeout();

        Assert.Equal(parentResourceId, childParentResourceId);
    }

    [Fact]
    public async Task WithParentRelationshipSetsParentPropertyCorrectly()
    {
        var builder = DistributedApplication.CreateBuilder();

        var parent = builder.AddContainer("parent", "image");
        var child = builder.AddContainer("child", "image").WithParentRelationship(parent.Resource);
        var child2 = builder.AddContainer("child2", "image").WithParentRelationship(parent.Resource);

        var nestedChild = builder.AddContainer("nested-child", "image").WithParentRelationship(child.Resource);

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var events = new DcpExecutorEvents();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var appOrchestrator = CreateOrchestrator(distributedAppModel, notificationService: resourceNotificationService, dcpEvents: events);
        await appOrchestrator.RunApplicationAsync();

        string? parentResourceId = null;
        string? childResourceId = null;
        string? childParentResourceId = null;
        string? child2ParentResourceId = null;
        string? nestedChildParentResourceId = null;
        var watchResourceTask = Task.Run(async () =>
        {
            await foreach (var item in resourceNotificationService.WatchAsync())
            {
                if (item.Resource == parent.Resource)
                {
                    parentResourceId = item.ResourceId;
                }
                else if (item.Resource == child.Resource)
                {
                    childResourceId = item.ResourceId;
                    childParentResourceId = item.Snapshot.Properties.SingleOrDefault(p => p.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                }
                else if (item.Resource == nestedChild.Resource)
                {
                    nestedChildParentResourceId = item.Snapshot.Properties.SingleOrDefault(p => p.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                }
                else if (item.Resource == child2.Resource)
                {
                    child2ParentResourceId = item.Snapshot.Properties.SingleOrDefault(p => p.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                }

                if (parentResourceId != null && childParentResourceId != null && nestedChildParentResourceId != null && child2ParentResourceId != null)
                {
                    return;
                }
            }
        });

        await events.PublishAsync(new OnResourceStartingContext(CancellationToken.None, KnownResourceTypes.Container, parent.Resource, parent.Resource.Name));

        await watchResourceTask.DefaultTimeout();

        Assert.Equal(parentResourceId, childParentResourceId);
        Assert.Equal(parentResourceId, child2ParentResourceId);

        // Nested child should be parented on the direct parent
        Assert.Equal(childResourceId, nestedChildParentResourceId);
    }

    [Fact]
    public async Task LastWithParentRelationshipWins()
    {
        var builder = DistributedApplication.CreateBuilder();

        var firstParent = builder.AddContainer("firstParent", "image");
        var secondParent = builder.AddContainer("secondParent", "image");

        var child = builder.AddContainer("child", "image");

        child.WithParentRelationship(firstParent.Resource);
        child.WithParentRelationship(secondParent.Resource);

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var events = new DcpExecutorEvents();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var appOrchestrator = CreateOrchestrator(distributedAppModel, notificationService: resourceNotificationService, dcpEvents: events);
        await appOrchestrator.RunApplicationAsync();

        string? firstParentResourceId = null;
        string? secondParentResourceId = null;
        string? childParentResourceId = null;
        var watchResourceTask = Task.Run(async () =>
        {
            await foreach (var item in resourceNotificationService.WatchAsync())
            {
                if (item.Resource == firstParent.Resource)
                {
                    firstParentResourceId = item.ResourceId;
                }
                else if (item.Resource == secondParent.Resource)
                {
                    secondParentResourceId = item.ResourceId;
                }
                else if (item.Resource == child.Resource)
                {
                    childParentResourceId = item.Snapshot.Properties.SingleOrDefault(p => p.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                }

                if (firstParentResourceId != null && secondParentResourceId != null && childParentResourceId != null)
                {
                    return;
                }
            }
        });

        await events.PublishAsync(new OnResourceStartingContext(CancellationToken.None, KnownResourceTypes.Container, firstParent.Resource, firstParent.Resource.Name));
        await events.PublishAsync(new OnResourceStartingContext(CancellationToken.None, KnownResourceTypes.Container, secondParent.Resource, secondParent.Resource.Name));

        await watchResourceTask.DefaultTimeout();

        // child should be parented to the last parent set
        Assert.Equal(secondParentResourceId, childParentResourceId);
    }

    [Fact]
    public async Task WithParentRelationshipWorksWithProjects()
    {
        var builder = DistributedApplication.CreateBuilder();

        var projectA = builder.AddProject<ProjectA>("projecta");
        var projectB = builder.AddProject<ProjectB>("projectb").WithParentRelationship(projectA.Resource);

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var events = new DcpExecutorEvents();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var appOrchestrator = CreateOrchestrator(distributedAppModel, notificationService: resourceNotificationService, dcpEvents: events);
        await appOrchestrator.RunApplicationAsync();

        string? projectAResourceId = null;
        string? projectBParentResourceId = null;
        var watchResourceTask = Task.Run(async () =>
        {
            await foreach (var item in resourceNotificationService.WatchAsync())
            {
                if (item.Resource == projectA.Resource)
                {
                    projectAResourceId = item.ResourceId;
                }
                else if (item.Resource == projectB.Resource)
                {
                    projectBParentResourceId = item.Snapshot.Properties.SingleOrDefault(p => p.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                }

                if (projectAResourceId != null && projectBParentResourceId != null)
                {
                    return;
                }
            }
        });

        await events.PublishAsync(new OnResourceStartingContext(CancellationToken.None, KnownResourceTypes.Container, projectA.Resource, projectA.Resource.Name));
        await events.PublishAsync(new OnResourceStartingContext(CancellationToken.None, KnownResourceTypes.Container, projectB.Resource, projectB.Resource.Name));

        await watchResourceTask.DefaultTimeout();

        Assert.Equal(projectAResourceId, projectBParentResourceId);
    }

    [Fact]
    public void DetectsCircularDependency()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container1 = builder.AddContainer("container1", "image");
        var container2 = builder.AddContainer("container2", "image2");
        var container3 = builder.AddContainer("container3", "image3");

        container1.WithParentRelationship(container2.Resource);
        container2.WithParentRelationship(container3.Resource);
        container3.WithParentRelationship(container1.Resource);

        using var app = builder.Build();

        var e = Assert.Throws<InvalidOperationException>(() => app.Services.GetService<ApplicationOrchestrator>());
        Assert.Contains("Circular dependency detected", e.Message);
    }

    private static ApplicationOrchestrator CreateOrchestrator(
        DistributedApplicationModel distributedAppModel,
        ResourceNotificationService notificationService,
        DcpExecutorEvents? dcpEvents = null,
        DistributedApplicationEventing? applicationEventing = null)
    {
        return new ApplicationOrchestrator(
            distributedAppModel,
            new TestDcpExecutor(),
            dcpEvents ?? new DcpExecutorEvents(),
            Array.Empty<IDistributedApplicationLifecycleHook>(),
            notificationService,
            applicationEventing ?? new DistributedApplicationEventing(),
            new ServiceCollection().BuildServiceProvider()
            );
    }

    private sealed class TestDcpExecutor : IDcpExecutor
    {
        public IResourceReference GetResource(string resourceName) => throw new NotImplementedException();

        public Task RunApplicationAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StartResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class CustomChildResource(string name, IResource parent) : Resource(name), IResourceWithParent
    {
        public IResource Parent => parent;
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class ProjectB : IProjectMetadata
    {
        public string ProjectPath => "projectB";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}
