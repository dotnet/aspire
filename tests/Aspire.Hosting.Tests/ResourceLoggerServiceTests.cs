// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceLoggerServiceTests
{
    [Fact]
    public async Task AddingResourceLoggerAnnotationAllowsLogging()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);

        var logsEnumerator1 = service.WatchAsync(testResource).GetAsyncEnumerator();
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 2);

        // Wait for subscriber to be added
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        // Log
        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Wait for logs to be read
        var allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);
        Assert.True(allLogs[1].IsErrorMessage);

        // New sub should get the previous logs
        subsLoop = WatchForSubscribers(service);
        var logsEnumerator2 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator2, 2);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(150));
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(2, allLogs.Count);
        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);

        await logsEnumerator1.DisposeAsync();
        await logsEnumerator2.DisposeAsync();
    }

    [Fact]
    public async Task StreamingLogsCancelledAfterComplete()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(service, 2, testResource);

        // Wait for subscriber to be added
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Complete the log stream & log afterwards
        service.Complete(testResource);
        logger.LogInformation("The third log");

        // Wait for logs to be read
        var allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Collection(allLogs,
            l => Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, world!", l.Content),
            l => Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, error!", l.Content));

        // The backlog should be cleared once there are no subscribers.
        Assert.Empty(service.GetResourceLoggerState(testResource.Name).GetBacklogSnapshot());

        // New sub should replay logs again.
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(service, 100, testResource);
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Collection(allLogs,
            l => Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, world!", l.Content),
            l => Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, error!", l.Content));
    }

    [Fact]
    public async Task SecondSubscriberGetsBacklog()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);
        var logsEnumerator1 = service.WatchAsync(testResource).GetAsyncEnumerator();
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 2);

        // Wait for subscriber to be added
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        // Log
        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Wait for logs to be read
        var allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);
        Assert.True(allLogs[1].IsErrorMessage);

        // New sub should get the previous logs (backlog)
        subsLoop = WatchForSubscribers(service);
        var logsEnumerator2 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator2, 2);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(2, allLogs.Count);
        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.Equal("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);

        // Clear the backlog and ensure new subs only get new logs
        service.ClearBacklog(testResource.Name);

        subsLoop = WatchForSubscribers(service);
        var logsEnumerator3 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator3, 1);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));
        logger.LogInformation("The third log");
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        // The backlog should be cleared so only new logs are received
        Assert.Single(allLogs);
        Assert.Equal("2000-12-29T20:59:59.0000000Z The third log", allLogs[0].Content);
    }

    [Fact]
    public async Task InMemoryLogsPreservedBetweenWatches()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        // Log before watching
        logger.LogInformation("Before watching!");

        var subsLoop = WatchForSubscribers(service);
        var logsEnumerator1 = service.WatchAsync(testResource).GetAsyncEnumerator();
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 1);

        // Wait for subscriber to be added
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        // Read before watching log
        var allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("2000-12-29T20:59:59.0000000Z Before watching!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        // Log while watching
        logger.LogInformation("While watching!");

        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 1);
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("2000-12-29T20:59:59.0000000Z While watching!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        // New sub should get the previous logs (backlog)
        subsLoop = WatchForSubscribers(service);
        var logsEnumerator2 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator2, 2);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(2, allLogs.Count);
        Assert.Equal("2000-12-29T20:59:59.0000000Z Before watching!", allLogs[0].Content);
        Assert.Equal("2000-12-29T20:59:59.0000000Z While watching!", allLogs[1].Content);

        await logsEnumerator1.DisposeAsync();
        await logsEnumerator2.DisposeAsync();

        logger.LogInformation("After watching!");

        // The backlog should be cleared once there are no subscribers.
        Assert.Empty(service.GetResourceLoggerState(testResource.Name).GetBacklogSnapshot());

        subsLoop = WatchForSubscribers(service);
        var logsEnumerator3 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator3, 4);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));
        logger.LogInformation("While watching again!");
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(4, allLogs.Count);
        Assert.Equal("2000-12-29T20:59:59.0000000Z Before watching!", allLogs[0].Content);
        Assert.Equal("2000-12-29T20:59:59.0000000Z While watching!", allLogs[1].Content);
        Assert.Equal("2000-12-29T20:59:59.0000000Z After watching!", allLogs[2].Content);
        Assert.Equal("2000-12-29T20:59:59.0000000Z While watching again!", allLogs[3].Content);
    }

    private sealed class TestResource(string name) : Resource(name)
    {

    }

    private static Task WatchForSubscribers(ResourceLoggerService service)
    {
        return Task.Run(async () =>
        {
            await foreach (var sub in service.WatchAnySubscribersAsync())
            {
                if (sub.AnySubscribers)
                {
                    break;
                }
            }
        });
    }
}
