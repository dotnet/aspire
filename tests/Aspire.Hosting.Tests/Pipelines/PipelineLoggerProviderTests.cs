// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Aspire.Hosting.Tests.Pipelines;

public class PipelineLoggerProviderTests
{
    [Fact]
    public void CurrentLogger_WhenNotSet_ReturnsNullLogger()
    {
        // Arrange & Act
        var currentLogger = PipelineLoggerProvider.CurrentLogger;

        // Assert
        Assert.Same(NullLogger.Instance, currentLogger);
    }

    [Fact]
    public void CurrentLogger_WhenSetToValidLogger_ReturnsSetLogger()
    {
        // Arrange
        var testLogger = new FakeLogger();

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = testLogger;
            var retrievedLogger = PipelineLoggerProvider.CurrentLogger;

            // Assert
            Assert.Same(testLogger, retrievedLogger);
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    [Fact]
    public void CurrentLogger_WhenSetToNull_ReturnsNullLogger()
    {
        // Arrange
        var testLogger = new FakeLogger();
        PipelineLoggerProvider.CurrentLogger = testLogger;

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
            var retrievedLogger = PipelineLoggerProvider.CurrentLogger;

            // Assert
            Assert.Same(NullLogger.Instance, retrievedLogger);
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    [Fact]
    public void CreateLogger_ReturnsValidLogger()
    {
        // Arrange
        var provider = new PipelineLoggerProvider();

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public async Task CurrentLogger_IsAsyncLocal_IsolatedBetweenTasks()
    {
        // Arrange
        var fakeLogger1 = new FakeLogger();
        var fakeLogger2 = new FakeLogger();
        var provider = new PipelineLoggerProvider();

        // Act
        var task1 = Task.Run(async () =>
        {
            PipelineLoggerProvider.CurrentLogger = fakeLogger1;
            var logger = provider.CreateLogger("Task1");

            logger.LogInformation("Task 1 message");
        });

        var task2 = Task.Run(async () =>
        {
            PipelineLoggerProvider.CurrentLogger = fakeLogger2;
            var logger = provider.CreateLogger("Task2");

            logger.LogInformation("Task 2 message");
        });

        await Task.WhenAll(task1, task2);

        // Assert
        var logs1 = fakeLogger1.Collector.GetSnapshot();
        var logEntry1 = Assert.Single(logs1);
        Assert.Equal(LogLevel.Information, logEntry1.Level);
        Assert.Equal("Task 1 message", logEntry1.Message);

        var logs2 = fakeLogger2.Collector.GetSnapshot();
        var logEntry2 = Assert.Single(logs2);
        Assert.Equal(LogLevel.Information, logEntry2.Level);
        Assert.Equal("Task 2 message", logEntry2.Message);
    }
}