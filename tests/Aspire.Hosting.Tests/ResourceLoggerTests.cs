// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceLoggerTests
{
    [Fact]
    public void AddingResourceLoggerAnnotationAllowsLogging()
    {
        var builder = DistributedApplication.CreateBuilder();

        var testResource = builder.AddResource(new TestResource("myResource"))
            .WithResourceLogger();

        var annotation = testResource.Resource.Annotations.OfType<ResourceLoggerAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var enumerator = annotation.WatchAsync().GetAsyncEnumerator();

        annotation.Logger.LogInformation("Hello, world!");
        annotation.Logger.LogError("Hello, error!");
        annotation.Complete();

        var allLogs = annotation.WatchAsync().ToBlockingEnumerable().SelectMany(x => x).ToList();

        Assert.Equal("Hello, world!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        Assert.Equal("Hello, error!", allLogs[1].Content);
        Assert.True(allLogs[1].IsErrorMessage);

        var backlog = annotation.WatchAsync().ToBlockingEnumerable().SelectMany(x => x).ToList();
        
        Assert.Equal("Hello, world!", backlog[0].Content);
        Assert.Equal("Hello, error!", backlog[1].Content);
    }

    [Fact]
    public async Task StreamingLogsCancelledAfterComplete()
    {
        var builder = DistributedApplication.CreateBuilder();

        var testResource = builder.AddResource(new TestResource("myResource"))
            .WithResourceLogger();

        var annotation = testResource.Resource.Annotations.OfType<ResourceLoggerAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        annotation.Logger.LogInformation("Hello, world!");
        annotation.Logger.LogError("Hello, error!");
        annotation.Complete();
        annotation.Logger.LogInformation("Hello, again!");

        var allLogs = annotation.WatchAsync().ToBlockingEnumerable().SelectMany(x => x).ToList();

        Assert.Collection(allLogs,
            log => Assert.Equal("Hello, world!", log.Content),
            log => Assert.Equal("Hello, error!", log.Content));

        Assert.DoesNotContain("Hello, again!", allLogs.Select(x => x.Content));

        await using var backlogEnumerator = annotation.WatchAsync().GetAsyncEnumerator();
        Assert.True(await backlogEnumerator.MoveNextAsync());
        Assert.Equal("Hello, world!", backlogEnumerator.Current[0].Content);
        Assert.Equal("Hello, error!", backlogEnumerator.Current[1].Content);

        // We're done
        Assert.False(await backlogEnumerator.MoveNextAsync());
    }

    private sealed class TestResource(string name) : Resource(name)
    {

    }
}
