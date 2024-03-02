// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceLoggerServiceTests
{
    [Fact]
    public void AddingResourceLoggerAnnotationAllowsLogging()
    {
        var testResource = new TestResource("myResource");

        var service = new ResourceLoggerService();

        var logger = service.GetLogger(testResource);

        var enumerator = service.WatchAsync(testResource).GetAsyncEnumerator();

        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");
        service.Complete(testResource);

        var allLogs = service.WatchAsync(testResource).ToBlockingEnumerable().SelectMany(x => x).ToList();

        Assert.Equal("Hello, world!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        Assert.Equal("Hello, error!", allLogs[1].Content);
        Assert.True(allLogs[1].IsErrorMessage);

        var backlog = service.WatchAsync(testResource).ToBlockingEnumerable().SelectMany(x => x).ToList();

        Assert.Equal("Hello, world!", backlog[0].Content);
        Assert.Equal("Hello, error!", backlog[1].Content);
    }

    [Fact]
    public async Task StreamingLogsCancelledAfterComplete()
    {
        var service = new ResourceLoggerService();

        var testResource = new TestResource("myResource");

        var logger = service.GetLogger(testResource);

        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");
        service.Complete(testResource);
        logger.LogInformation("Hello, again!");

        var allLogs = service.WatchAsync(testResource).ToBlockingEnumerable().SelectMany(x => x).ToList();

        Assert.Collection(allLogs,
            log => Assert.Equal("Hello, world!", log.Content),
            log => Assert.Equal("Hello, error!", log.Content));

        Assert.DoesNotContain("Hello, again!", allLogs.Select(x => x.Content));

        await using var backlogEnumerator = service.WatchAsync(testResource).GetAsyncEnumerator();
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
