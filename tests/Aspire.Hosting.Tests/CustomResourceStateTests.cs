// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class CustomResourceStateTests
{
    [Fact]
    public void CreatePopulatesStateFromResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var custom = builder.AddResource(new CustomResource("myResource"))
            .WithEndpoint(name: "ep", scheme: "http", hostPort: 8080)
            .WithEnvironment("x", "1000")
            .WithCustomResourceState();

        var annotation = custom.Resource.Annotations.OfType<CustomResourceAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.GetInitialState();

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
            .WithCustomResourceState(() => new()
            {
                ResourceType = "MyResource",
                Properties = [("A", "B")],
            });

        var annotation = custom.Resource.Annotations.OfType<CustomResourceAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.GetInitialState();

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
            .WithCustomResourceState();

        var annotation = custom.Resource.Annotations.OfType<CustomResourceAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.GetInitialState();

        state = state with { Properties = state.Properties.Add(("A", "value")) };

        await annotation.UpdateStateAsync(state);

        state = state with { Properties = state.Properties.Add(("B", "value")) };

        await annotation.UpdateStateAsync(state);

        var enumerator = annotation.WatchAsync().GetAsyncEnumerator();

        await enumerator.MoveNextAsync();

        Assert.Equal("value", enumerator.Current.Properties.Single(p => p.Key == "A").Value);

        await enumerator.MoveNextAsync();

        Assert.Equal("value", enumerator.Current.Properties.Single(p => p.Key == "B").Value);
    }

    private sealed class CustomResource(string name) : Resource(name),
        IResourceWithEnvironment,
        IResourceWithConnectionString
    {
        public string? GetConnectionString() => "CustomConnectionString";
    }
}
