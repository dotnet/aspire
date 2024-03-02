// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceNotificationTests
{
    [Fact]
    public void InitialStateCanBeSpecified()
    {
        var builder = DistributedApplication.CreateBuilder();

        var custom = builder.AddResource(new CustomResource("myResource"))
            .WithEndpoint(name: "ep", scheme: "http", hostPort: 8080)
            .WithEnvironment("x", "1000")
            .WithInitialState(new()
            {
                ResourceType = "MyResource",
                Properties = [("A", "B")],
            });

        var annotation = custom.Resource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.Equal("MyResource", state.ResourceType);
        Assert.Empty(state.EnvironmentVariables);
        Assert.Collection(state.Properties, c =>
        {
            Assert.Equal("A", c.Key);
            Assert.Equal("B", c.Value);
        });
    }

    [Fact]
    public async Task ResourceUpdatesAreQueued()
    {
        var resource = new CustomResource("myResource");

        var notificationService = new ResourceNotificationService();

        async Task<List<CustomResourceSnapshot>> GetValuesAsync()
        {
            var values = new List<CustomResourceSnapshot>();

            await foreach (var item in notificationService.WatchAsync(resource))
            {
                values.Add(item);
            }

            return values;
        }

        var enumerableTask = GetValuesAsync();

        await notificationService.PublishUpdateAsync(resource, state => state with { Properties = state.Properties.Add(("A", "value")) });

        await notificationService.PublishUpdateAsync(resource, state => state with { Properties = state.Properties.Add(("B", "value")) });

        notificationService.Complete(resource);

        var values = await enumerableTask;

        // Watch returns an initial snapshot
        Assert.Empty(values[0].Properties);
        Assert.Equal("value", values[1].Properties.Single(p => p.Key == "A").Value);
        Assert.Equal("value", values[2].Properties.Single(p => p.Key == "B").Value);
    }

    [Fact]
    public async Task WatchReturnsAnInitialState()
    {
        var resource = new CustomResource("myResource");

        var notificationService = new ResourceNotificationService();

        async Task<List<CustomResourceSnapshot>> GetValuesAsync()
        {
            var values = new List<CustomResourceSnapshot>();

            await foreach (var item in notificationService.WatchAsync(resource))
            {
                values.Add(item);
            }

            return values;
        }

        var enumerableTask = GetValuesAsync();

        notificationService.Complete(resource);

        var values = await enumerableTask;

        // Watch returns an initial snapshot
        var snapshot = Assert.Single(values);

        Assert.Equal("CustomResource", snapshot.ResourceType);
        Assert.Empty(snapshot.EnvironmentVariables);
        Assert.Empty(snapshot.Properties);
    }

    [Fact]
    public async Task WatchReturnsAnInitialStateIfCustomized()
    {
        var resource = new CustomResource("myResource");
        resource.Annotations.Add(new ResourceSnapshotAnnotation(new CustomResourceSnapshot
        {
            ResourceType = "CustomResource1",
            Properties = [("A", "B")],
        }));

        var notificationService = new ResourceNotificationService();

        async Task<List<CustomResourceSnapshot>> GetValuesAsync()
        {
            var values = new List<CustomResourceSnapshot>();

            await foreach (var item in notificationService.WatchAsync(resource))
            {
                values.Add(item);
            }

            return values;
        }

        var enumerableTask = GetValuesAsync();

        notificationService.Complete(resource);

        var values = await enumerableTask;

        // Watch returns an initial snapshot
        var snapshot = Assert.Single(values);

        Assert.Equal("CustomResource1", snapshot.ResourceType);
        Assert.Empty(snapshot.EnvironmentVariables);
        Assert.Collection(snapshot.Properties, c =>
        {
            Assert.Equal("A", c.Key);
            Assert.Equal("B", c.Value);
        });
    }

    private sealed class CustomResource(string name) : Resource(name),
        IResourceWithEnvironment,
        IResourceWithConnectionString
    {
        public string? GetConnectionString() => "CustomConnectionString";
    }
}
