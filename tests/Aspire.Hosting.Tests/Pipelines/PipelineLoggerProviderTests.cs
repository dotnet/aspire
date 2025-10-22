// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        var testLogger = new TestLogger();

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
        var testLogger = new TestLogger();
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
    public void CurrentLogger_WhenSetToNullLogger_ReturnsNullLogger()
    {
        // Arrange & Act
        PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        var retrievedLogger = PipelineLoggerProvider.CurrentLogger;

        // Assert
        Assert.Same(NullLogger.Instance, retrievedLogger);
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
    public void CreatedLogger_WhenNoCurrentLogger_ForwardsToNullLogger()
    {
        // Arrange
        var provider = new PipelineLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        // Act & Assert
        Assert.False(logger.IsEnabled(LogLevel.Information));
        
        // Should not throw when logging with no current logger
        logger.LogInformation("Test message");
    }

    [Fact]
    public void CreatedLogger_WhenCurrentLoggerSet_ForwardsToCurrentLogger()
    {
        // Arrange
        var testLogger = new TestLogger();
        var provider = new PipelineLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = testLogger;
            
            logger.LogInformation("Test message");

            // Assert
            var logEntry = Assert.Single(testLogger.LogEntries);
            Assert.Equal(LogLevel.Information, logEntry.LogLevel);
            Assert.Equal("Test message", logEntry.Message);
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    [Fact]
    public void CreatedLogger_IsEnabled_ForwardsToCurrentLogger()
    {
        // Arrange
        var testLogger = new TestLogger { EnabledLogLevel = LogLevel.Warning };
        var provider = new PipelineLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = testLogger;

            // Assert
            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Error));
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    [Fact]
    public void CreatedLogger_BeginScope_ForwardsToCurrentLogger()
    {
        // Arrange
        var testLogger = new TestLogger();
        var provider = new PipelineLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = testLogger;
            
            using var scope = logger.BeginScope("Test scope");

            // Assert
            Assert.Single(testLogger.Scopes);
            Assert.Equal("Test scope", testLogger.Scopes[0]);
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    [Fact]
    public void CreatedLogger_WhenCurrentLoggerChanges_UsesNewLogger()
    {
        // Arrange
        var testLogger1 = new TestLogger();
        var testLogger2 = new TestLogger();
        var provider = new PipelineLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = testLogger1;
            logger.LogInformation("Message 1");

            PipelineLoggerProvider.CurrentLogger = testLogger2;
            logger.LogInformation("Message 2");

            // Assert
            var logEntry1 = Assert.Single(testLogger1.LogEntries);
            Assert.Equal("Message 1", logEntry1.Message);

            var logEntry2 = Assert.Single(testLogger2.LogEntries);
            Assert.Equal("Message 2", logEntry2.Message);
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    [Fact]
    public async Task CurrentLogger_IsAsyncLocal_IsolatedBetweenTasks()
    {
        // Arrange
        var testLogger1 = new TestLogger();
        var testLogger2 = new TestLogger();
        var provider = new PipelineLoggerProvider();

        var task1Complete = new TaskCompletionSource<bool>();
        var task2Complete = new TaskCompletionSource<bool>();
        var bothTasksStarted = new TaskCompletionSource<bool>();
        var startedCount = 0;

        // Act
        var task1 = Task.Run(async () =>
        {
            PipelineLoggerProvider.CurrentLogger = testLogger1;
            var logger = provider.CreateLogger("Task1");

            if (Interlocked.Increment(ref startedCount) == 2)
            {
                bothTasksStarted.SetResult(true);
            }

            await bothTasksStarted.Task;
            await Task.Delay(10); // Allow other task to potentially interfere

            logger.LogInformation("Task 1 message");
            task1Complete.SetResult(true);
        });

        var task2 = Task.Run(async () =>
        {
            PipelineLoggerProvider.CurrentLogger = testLogger2;
            var logger = provider.CreateLogger("Task2");

            if (Interlocked.Increment(ref startedCount) == 2)
            {
                bothTasksStarted.SetResult(true);
            }

            await bothTasksStarted.Task;
            await Task.Delay(10); // Allow other task to potentially interfere

            logger.LogInformation("Task 2 message");
            task2Complete.SetResult(true);
        });

        await Task.WhenAll(task1, task2);
        await Task.WhenAll(task1Complete.Task, task2Complete.Task);

        // Assert
        var logEntry1 = Assert.Single(testLogger1.LogEntries);
        Assert.Equal("Task 1 message", logEntry1.Message);

        var logEntry2 = Assert.Single(testLogger2.LogEntries);
        Assert.Equal("Task 2 message", logEntry2.Message);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var provider = new PipelineLoggerProvider();

        // Act & Assert
        provider.Dispose(); // Should not throw
    }

    [Fact]
    public void CreatedLogger_LogWithException_ForwardsToCurrentLogger()
    {
        // Arrange
        var testLogger = new TestLogger();
        var provider = new PipelineLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");
        var exception = new InvalidOperationException("Test exception");

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = testLogger;
            logger.LogError(exception, "Error occurred");

            // Assert
            var logEntry = Assert.Single(testLogger.LogEntries);
            Assert.Equal(LogLevel.Error, logEntry.LogLevel);
            Assert.Equal("Error occurred", logEntry.Message);
            Assert.Same(exception, logEntry.Exception);
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    [Fact]
    public void CreatedLogger_LogWithEventId_ForwardsToCurrentLogger()
    {
        // Arrange
        var testLogger = new TestLogger();
        var provider = new PipelineLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");
        var eventId = new EventId(42, "TestEvent");

        try
        {
            // Act
            PipelineLoggerProvider.CurrentLogger = testLogger;
            logger.Log(LogLevel.Information, eventId, "Test message", null, (state, ex) => state);

            // Assert
            var logEntry = Assert.Single(testLogger.LogEntries);
            Assert.Equal(LogLevel.Information, logEntry.LogLevel);
            Assert.Equal(eventId, logEntry.EventId);
            Assert.Equal("Test message", logEntry.Message);
        }
        finally
        {
            // Cleanup
            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
        }
    }

    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> LogEntries { get; } = [];
        public List<object> Scopes { get; } = [];
        public LogLevel EnabledLogLevel { get; set; } = LogLevel.Trace;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            Scopes.Add(state);
            return new TestScope(() => Scopes.Remove(state));
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= EnabledLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception
            });
        }

        public sealed class LogEntry
        {
            public LogLevel LogLevel { get; init; }
            public EventId EventId { get; init; }
            public string Message { get; init; } = string.Empty;
            public Exception? Exception { get; init; }
        }

        private sealed class TestScope(Action onDispose) : IDisposable
        {
            public void Dispose() => onDispose();
        }
    }
}