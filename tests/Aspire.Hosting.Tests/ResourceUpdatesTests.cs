// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceUpdatesTests
{
    [Fact]
    public void CreatePopulatesStateFromResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var custom = builder.AddResource(new CustomResource("myResource"))
            .WithEndpoint(name: "ep", scheme: "http", hostPort: 8080)
            .WithEnvironment("x", "1000")
            .WithResourceUpdates();

        var annotation = custom.Resource.Annotations.OfType<ResourceUpdatesAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.GetInitialSnapshot();

        Assert.Equal("Custom", state.ResourceType);

        Assert.Collection(state.EnviromentVariables, a =>
        {
            Assert.Equal("x", a.Name);
            Assert.Equal("1000", a.Value);
        });

        Assert.Collection(state.Properties, c =>
        {
            Assert.Equal("ConnectionString", c.Key);
            Assert.Equal("CustomConnectionString", c.Value);
        });

        Assert.Collection(state.Urls, u =>
        {
            Assert.Equal("http://localhost:8080", u);
        });
    }

    [Fact]
    public void InitialStateCanBeSpecified()
    {
        var builder = DistributedApplication.CreateBuilder();

        var custom = builder.AddResource(new CustomResource("myResource"))
            .WithEndpoint(name: "ep", scheme: "http", hostPort: 8080)
            .WithEnvironment("x", "1000")
            .WithResourceUpdates(() => new()
            {
                ResourceType = "MyResource",
                Properties = [("A", "B")],
            });

        var annotation = custom.Resource.Annotations.OfType<ResourceUpdatesAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.GetInitialSnapshot();

        Assert.Equal("MyResource", state.ResourceType);
        Assert.Empty(state.EnviromentVariables);
        Assert.Collection(state.Properties, c =>
        {
            Assert.Equal("A", c.Key);
            Assert.Equal("B", c.Value);
        });
    }

    [Fact]
    public async Task ResourceUpdatesAreQueued()
    {
        var builder = DistributedApplication.CreateBuilder();

        var custom = builder.AddResource(new CustomResource("myResource"))
            .WithEndpoint(name: "ep", scheme: "http", hostPort: 8080)
            .WithEnvironment("x", "1000")
            .WithResourceUpdates();

        var annotation = custom.Resource.Annotations.OfType<ResourceUpdatesAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var enumerableTask = Task.Run(async () =>
        {
            var values = new List<CustomResourceSnapshot>();

            await foreach (var item in annotation.WatchAsync())
            {
                values.Add(item);
            }

            return values;
        });

        var state = annotation.GetInitialSnapshot();

        state = state with { Properties = state.Properties.Add(("A", "value")) };

        await annotation.UpdateStateAsync(state);

        state = state with { Properties = state.Properties.Add(("B", "value")) };

        await annotation.UpdateStateAsync(state);

        annotation.Complete();

        var values = await enumerableTask;

        Assert.Equal("value", values[0].Properties.Single(p => p.Key == "A").Value);
        Assert.Equal("value", values[1].Properties.Single(p => p.Key == "B").Value);
    }

    private sealed class CustomResource(string name) : Resource(name),
        IResourceWithEnvironment,
        IResourceWithConnectionString
    {
        public string? GetConnectionString() => "CustomConnectionString";
    }
}
