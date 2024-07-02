// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceLoggerServiceTests
{
    [Fact]
    public async Task AddingResourceLoggerAnnotationAllowsLogging()
    {
        var testResource = new TestResource("myResource");
        var service = new ResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);
        var logsLoop = WatchForLogs(service, 2, testResource);

        // Wait for subscriber to be added
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        // Log
        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Wait for logs to be read
        var allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("Hello, world!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        Assert.Equal("Hello, error!", allLogs[1].Content);
        Assert.True(allLogs[1].IsErrorMessage);

        // New sub should get the previous logs
        subsLoop = WatchForSubscribers(service);
        logsLoop = WatchForLogs(service, 2, testResource);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(2, allLogs.Count);
        Assert.Equal("Hello, world!", allLogs[0].Content);
        Assert.Equal("Hello, error!", allLogs[1].Content);
    }

    [Fact]
    public async Task StreamingLogsCancelledAfterComplete()
    {
        var testResource = new TestResource("myResource");
        var service = new ResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);
        var logsLoop = WatchForLogs(service, 2, testResource);

        // Wait for subscriber to be added
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Complete the log stream & log afterwards
        service.Complete(testResource);
        logger.LogInformation("The third log");

        // Wait for logs to be read
        var allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("Hello, world!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        Assert.Equal("Hello, error!", allLogs[1].Content);
        Assert.True(allLogs[1].IsErrorMessage);

        Assert.DoesNotContain("The third log", allLogs.Select(x => x.Content));

        // New sub should not get new logs as the stream is completed
        logsLoop = WatchForLogs(service, 100, testResource);
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(2, allLogs.Count);
    }

    [Fact]
    public async Task SecondSubscriberGetsBacklog()
    {
        var testResource = new TestResource("myResource");
        var service = new ResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);
        var logsLoop = WatchForLogs(service, 2, testResource);

        // Wait for subscriber to be added
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        // Log
        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Wait for logs to be read
        var allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("Hello, world!", allLogs[0].Content);
        Assert.False(allLogs[0].IsErrorMessage);

        Assert.Equal("Hello, error!", allLogs[1].Content);
        Assert.True(allLogs[1].IsErrorMessage);

        // New sub should get the previous logs (backlog)
        subsLoop = WatchForSubscribers(service);
        logsLoop = WatchForLogs(service, 2, testResource);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(2, allLogs.Count);
        Assert.Equal("Hello, world!", allLogs[0].Content);
        Assert.Equal("Hello, error!", allLogs[1].Content);

        // Clear the backlog and ensure new subs only get new logs
        service.ClearBacklog(testResource.Name);

        subsLoop = WatchForSubscribers(service);
        logsLoop = WatchForLogs(service, 1, testResource);
        await subsLoop.WaitAsync(TimeSpan.FromSeconds(15));
        logger.LogInformation("The third log");
        allLogs = await logsLoop.WaitAsync(TimeSpan.FromSeconds(15));

        // The backlog should be cleared so only new logs are received
        Assert.Equal(1, allLogs.Count);
        Assert.Equal("The third log", allLogs[0].Content);
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

    private static Task<IReadOnlyList<LogLine>> WatchForLogs(ResourceLoggerService service, int targetLogCount, IResource resource)
    {
        return Task.Run(async () =>
        {
            var logs = new List<LogLine>();
            await foreach (var log in service.WatchAsync(resource))
            {
                logs.AddRange(log);
                if (logs.Count >= targetLogCount)
                {
                    break;
                }
            }
            return (IReadOnlyList<LogLine>)logs;
        });
    }
}
