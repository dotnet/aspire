// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Provides loggers that forward calls to a contextual logger associated with the current pipeline step.
/// </summary>
/// <remarks>
/// This logger provider uses AsyncLocal storage to maintain the current logger context across async operations.
/// This enables pipeline steps to log through a contextual logger that can be set per execution.
/// </remarks>
internal sealed class PipelineLoggerProvider : ILoggerProvider
{
    private static readonly AsyncLocal<StepLoggerHolder?> s_currentLogger = new();

    /// <summary>
    /// Gets or sets the current logger for the executing pipeline step.
    /// </summary>
    public static ILogger CurrentLogger
    {
        get => s_currentLogger.Value?.Logger ?? NullLogger.Instance;
        set
        {
            // Clear the current logger from AsyncLocal context
            s_currentLogger.Value = null;

            if (value is not null && value != NullLogger.Instance)
            {
                // Use an object indirection to hold the logger in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when cleared.
                s_currentLogger.Value = new StepLoggerHolder { Logger = value };
            }
        }
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) =>
        new PipelineLogger(() => CurrentLogger);

    /// <inheritdoc/>
    public void Dispose()
    {
        // No resources to dispose
    }

    /// <summary>
    /// Holds the logger instance in AsyncLocal storage.
    /// </summary>
    private sealed class StepLoggerHolder
    {
        public ILogger? Logger;
    }

    /// <summary>
    /// A logger implementation that forwards all calls to the current contextual logger.
    /// </summary>
    /// <remarks>
    /// This logger acts as a proxy and dynamically resolves the current logger on each operation,
    /// allowing the target logger to change between calls.
    /// When logging exceptions, stack traces are only included when the log level is Debug or Trace.
    /// </remarks>
    private sealed class PipelineLogger(Func<ILogger> currentLoggerAccessor) : ILogger
    {
        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            currentLoggerAccessor().BeginScope(state);

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) =>
            currentLoggerAccessor().IsEnabled(logLevel);

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var logger = currentLoggerAccessor();
            
            // If there's an exception and the log level is not Debug or Trace, exclude the exception from the log
            // to avoid cluttering production logs with stack traces
            if (exception is not null && logLevel > LogLevel.Debug)
            {
                logger.Log(logLevel, eventId, state, null, formatter);
            }
            else
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}