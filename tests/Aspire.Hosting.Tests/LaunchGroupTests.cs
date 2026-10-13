// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

[Trait("Partition", "2")]
public class LaunchGroupTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("catalog")]
    [InlineData("CATALOG")]
    [InlineData("basket")]
    public async Task SelectionPreservesFullRunsAndUngroupedResources(string? selection)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroup"] = selection;
        builder.AddLaunchGroups("catalog", "basket");
        var catalog = builder.AddContainer("catalog", "image").WithLaunchGroup("catalog");
        var basket = builder.AddExecutable("basket", "unused", ".").WithLaunchGroup("basket");
        var shared = builder.AddExecutable("shared", "unused", ".").WithLaunchGroups("basket", "catalog");
        var ungrouped = builder.AddExecutable("ungrouped", "unused", ".");
        await using var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken);

        Assert.Equal(selection == "basket", catalog.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
        Assert.Equal(selection is "catalog" or "CATALOG", basket.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
        Assert.False(shared.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
        Assert.False(ungrouped.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Fact]
    public async Task MembershipIsAdditiveAndImplicitlyDeclaresNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroup"] = "second";
        var resource = builder.AddExecutable("worker", "unused", ".")
            .WithLaunchGroup("first")
            .WithLaunchGroups("FIRST", "second");
        await using var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second"], resource.Resource.Annotations.OfType<LaunchGroupAnnotation>().Select(annotation => annotation.LaunchGroupName));
        Assert.False(resource.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Fact]
    public async Task IncludesTransitiveWaitReferenceAndParentDependencies()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroup"] = "frontend";
        var database = builder.AddContainer("database", "image").WithLaunchGroup("backend");
        var child = builder.AddConnectionString("connection", ReferenceExpression.Create($"{database.GetEndpoint("tcp")}"));
        database.WithEndpoint(name: "tcp", scheme: "tcp", targetPort: 1234);
        var migration = builder.AddExecutable("migration", "unused", ".")
            .WithLaunchGroup("backend").WaitFor(database);
        var api = builder.AddExecutable("api", "unused", ".")
            .WithLaunchGroup("backend").WaitForCompletion(migration).WithReference(child);
        var frontend = builder.AddExecutable("frontend", "unused", ".")
            .WithLaunchGroup("frontend").WithReferenceRelationship(api);
        var unrelated = builder.AddContainer("unrelated", "image").WithLaunchGroup("backend");
        await using var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken);

        foreach (var resource in new IResource[] { database.Resource, migration.Resource, api.Resource, frontend.Resource })
        {
            Assert.False(resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
        }
        Assert.True(unrelated.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Fact]
    public async Task UngroupedChildrenInheritMembershipAndKeepTheirOwnLifecycle()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroup"] = "catalog";
        builder.AddLaunchGroups("catalog", "basket");
        var parent = builder.AddContainer("cache", "image").WithLaunchGroup("basket");
        var child = builder.AddExecutable("admin", "unused", ".").WithParentRelationship(parent);
        var overrideChild = builder.AddExecutable("other-admin", "unused", ".")
            .WithParentRelationship(parent).WithLaunchGroup("catalog");
        await using var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken);

        Assert.True(child.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
        Assert.False(overrideChild.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
        Assert.False(parent.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Fact]
    public async Task ExplicitStartIsPreservedAndDoesNotSelectItsDependencies()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroup"] = "catalog";
        builder.AddLaunchGroups("catalog", "basket");
        var dependency = builder.AddContainer("cache", "image").WithLaunchGroup("basket");
        var explicitResource = builder.AddExecutable("explicit", "unused", ".").WithExplicitStart().WaitFor(dependency);
        await using var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken);

        Assert.True(explicitResource.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
        Assert.True(dependency.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Fact]
    public async Task ConfigurationAndCodeDeclarationsAreCombined()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroups:0"] = "catalog";
        builder.Configuration["LaunchGroup"] = "BASKET";
        builder.AddLaunchGroups("basket").AddLaunchGroups("BASKET");
        var resource = builder.AddExecutable("catalog", "unused", ".").WithLaunchGroup("catalog");
        await using var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken);

        Assert.True(resource.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("catalog,basket")]
    [InlineData("catalog;basket")]
    [InlineData(" catalog ")]
    public async Task InvalidSelectionsFailBeforeStarting(string selection)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroup"] = selection;
        builder.AddLaunchGroups("catalog", "basket");
        var resource = builder.AddExecutable("catalog", "unused", ".").WithLaunchGroup("catalog");
        await using var app = builder.Build();

        await Assert.ThrowsAsync<DistributedApplicationException>(
            () => app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken));

        Assert.False(resource.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Fact]
    public async Task UndefinedMembershipFailsEvenWithoutSelection()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddLaunchGroups("catalog");
        builder.AddExecutable("basket", "unused", ".").WithLaunchGroup("basket");
        await using var app = builder.Build();

        await Assert.ThrowsAsync<DistributedApplicationException>(
            () => app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" catalog")]
    [InlineData("catalog,basket")]
    public void InvalidNamesAreRejected(string name)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddExecutable("worker", "unused", ".");

        Assert.Throws<ArgumentException>(() => builder.AddLaunchGroups(name));
        Assert.Throws<ArgumentException>(() => resource.WithLaunchGroup(name));
        Assert.Empty(resource.Resource.Annotations.OfType<LaunchGroupAnnotation>());
    }

    [Fact]
    public async Task PublishingIgnoresLaunchGroupSelection()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.Configuration["LaunchGroup"] = "invalid";
        builder.AddLaunchGroups("catalog");
        var resource = builder.AddContainer("catalog", "image").WithLaunchGroup("catalog");
        await using var app = builder.Build();

        app.Services.GetRequiredService<LaunchGroupService>().Configure(app.Services.GetRequiredService<DistributedApplicationModel>());

        Assert.False(resource.Resource.HasAnnotationOfType<ExplicitStartupAnnotation>());
    }

    [Fact]
    public void ReferenceCyclesHaveAFiniteClosure()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var first = builder.AddExecutable("first", "unused", ".");
        var second = builder.AddExecutable("second", "unused", ".");
        first.WithReferenceRelationship(second);
        second.WithReferenceRelationship(first);

        Assert.Equal([second.Resource, first.Resource], LaunchGroupService.GetClosure([first.Resource]));
    }

    [Fact]
    public async Task PartialRunsDeferPersistentExplicitResourceConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["LaunchGroup"] = "catalog";
        builder.AddLaunchGroups("catalog", "basket");
#pragma warning disable ASPIREPERSISTENCE001
        var resource = builder.AddExecutable("worker", "unused", ".")
            .WithPersistentLifetime().WithLaunchGroup("basket");
#pragma warning restore ASPIREPERSISTENCE001
        await using var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(TestContext.Current.CancellationToken);

        Assert.True(DcpModelUtilities.ShouldDeferCreateForExplicitStart(resource.Resource, start: false));
        Assert.False(DcpModelUtilities.ShouldDeferCreateForExplicitStart(resource.Resource, start: true));
    }
}
