// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.Channels;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service that provides loggers for resources to write to.
/// </summary>
public class ResourceLoggerService
{
    private readonly ConcurrentDictionary<string, ResourceLoggerState> _loggers = new();

    private Action<(string, ResourceLoggerState)>? _loggerAdded;
    private event Action<(string, ResourceLoggerState)> LoggerAdded
    {
        add
        {
            _loggerAdded += value;

            foreach (var logger in _loggers)
            {
                value((logger.Key, logger.Value));
            }
        }
        remove
        {
            _loggerAdded -= value;
        }
    }

    /// <summary>
    /// Gets the logger for the resource to write to.
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <returns>An <see cref="ILogger"/>.</returns>
    public ILogger GetLogger(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return GetResourceLoggerState(resource.Name).Logger;
    }

    /// <summary>
    /// Gets the logger for the resource to write to.
    /// </summary>
    /// <param name="resourceName">The name of the resource from the Aspire application model.</param>
    /// <returns>An <see cref="ILogger"/> which repesents the named resource.</returns>
    /// <returns>An <see cref="ILogger"/>.</returns>
    public ILogger GetLogger(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        return GetResourceLoggerState(resourceName).Logger;
    }

    /// <summary>
    /// Gets a logger for the resource to write logs to. This logger will not include any buffering.
    /// </summary>
    /// <param name="resourceName">The resource name. </param>
    /// <returns>An <see cref="ILogger"/>.</returns>
    public ILogger GetStreamingLogger(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        return GetResourceLoggerState(resourceName).StreamingLogger;
    }

    /// <summary>
    /// Watch for changes to the log stream for a resource.
    /// </summary>
    /// <param name="resourceName">The resource name</param>
    /// <returns></returns>
    public IAsyncEnumerable<IReadOnlyList<LogLine>> WatchAsync(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        return GetResourceLoggerState(resourceName).WatchAsync();
    }

    /// <summary>
    /// Watch for subscribers to the log stream for a resource.
    /// </summary>
    /// <returns></returns>
    public IAsyncEnumerable<LogSubscriber> WatchAnySubscribersAsync() =>
        new AnySubscribersAsyncEnumerable(this);

    /// <summary>
    /// Watch for changes to the log stream for a resource.
    /// </summary>
    /// <param name="resource">The resource to watch for logs.</param>
    /// <returns></returns>
    public IAsyncEnumerable<IReadOnlyList<LogLine>> WatchAsync(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return WatchAsync(resource.Name);
    }

    /// <summary>
    /// Completes the log stream for the resource.
    /// </summary>
    /// <param name="resource">The <see cref="IResource"/>.</param>
    public void Complete(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (_loggers.TryGetValue(resource.Name, out var logger))
        {
            logger.Complete();
        }
    }

    /// <summary>
    /// Completes the log stream for the resource.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public void Complete(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_loggers.TryGetValue(name, out var logger))
        {
            logger.Complete();
        }
    }

    private ResourceLoggerState GetResourceLoggerState(string resourceName) =>
        _loggers.GetOrAdd(resourceName, (name, context) =>
        {
            var state = new ResourceLoggerState();
            context._loggerAdded?.Invoke((name, state));
            return state;
        },
        this);

    private sealed class AnySubscribersAsyncEnumerable(ResourceLoggerService loggerService) : IAsyncEnumerable<LogSubscriber>
    {
        public async IAsyncEnumerator<LogSubscriber> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<LogSubscriber>();

            void OnLoggerAdded((string Name, ResourceLoggerState State) loggerItem)
            {
                var (name, state) = loggerItem;

                state.OnSubscribersChanged += (hasSubscribers) =>
                {
                    channel.Writer.TryWrite(new(name, hasSubscribers));
                };
            }

            loggerService.LoggerAdded += OnLoggerAdded;

            try
            {
                await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return entry;
                }
            }
            finally
            {
                loggerService.LoggerAdded -= OnLoggerAdded;
            }
        }
    }

    /// <summary>
    /// A logger for the resource to write to.
    /// </summary>
    private sealed class ResourceLoggerState
    {
        private readonly ResourceLogger _logger;
        private readonly ResourceLogger _streamingLogger;
        private readonly CancellationTokenSource _logStreamCts = new();

        private readonly CircularBuffer<LogLine> _backlog = new(10000);

        /// <summary>
        /// Creates a new <see cref="ResourceLoggerState"/>.
        /// </summary>
        public ResourceLoggerState()
        {
            _logger = new ResourceLogger(this, useBacklog: true);
            _streamingLogger = new ResourceLogger(this, useBacklog: false);
        }

        private Action<bool>? _onSubscribersChanged;

        public event Action<bool> OnSubscribersChanged
        {
            add
            {
                _onSubscribersChanged += value;

                var hasSubscribers = false;

                lock (this)
                {
                    if (_count > 0)
                    {
                        hasSubscribers = true;
                    }
                }

                if (hasSubscribers)
                {
                    value(hasSubscribers);
                }
            }
            remove
            {
                _onSubscribersChanged -= value;
            }
        }

        /// <summary>
        /// Watch for changes to the log stream for a resource.
        /// </summary>
        /// <returns>The log stream for the resource.</returns>
        public IAsyncEnumerable<IReadOnlyList<LogLine>> WatchAsync()
        {
            if (_backlog is null)
            {
                return new LogAsyncEnumerable(this, []);
            }

            lock (_backlog)
            {
                // REVIEW: Performance makes me very sad, but we can optimize this later.
                return new LogAsyncEnumerable(this, [.. _backlog]);
            }
        }

        private Action<LogLine>? _onNewLog;
        private int _count;

        // This provides the fan out to multiple subscribers.
        private event Action<LogLine> OnNewLog
        {
            add
            {
                _onNewLog += value;

                var raiseSubscribersChanged = false;

                lock (this)
                {
                    _count++;

                    if (_count == 1)
                    {
                        raiseSubscribersChanged = true;
                    }
                }

                if (raiseSubscribersChanged)
                {
                    _onSubscribersChanged?.Invoke(true);
                }
            }
            remove
            {
                _onNewLog -= value;

                var raiseSubscribersChanged = false;

                lock (this)
                {
                    _count--;

                    if (_count == 0)
                    {
                        raiseSubscribersChanged = true;
                    }
                }

                if (raiseSubscribersChanged)
                {
                    _onSubscribersChanged?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// The logger for the resource to write to. This will write updates to the live log stream for this resource.
        /// </summary>
        public ILogger Logger => _logger;

        public ILogger StreamingLogger => _streamingLogger;

        /// <summary>
        /// Close the log stream for the resource. Future subscribers will not receive any updates and will complete immediately.
        /// </summary>
        public void Complete()
        {
            // REVIEW: Do we clean up the backlog?
            _logStreamCts.Cancel();
        }

        private sealed class ResourceLogger(ResourceLoggerState loggerState, bool useBacklog) : ILogger
        {
            private int _lineNumber;

            IDisposable? ILogger.BeginScope<TState>(TState state) => null;

            bool ILogger.IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (loggerState._logStreamCts.IsCancellationRequested)
                {
                    // Noop if logging after completing the stream
                    return;
                }

                var log = formatter(state, exception) + (exception is null ? "" : $"\n{exception}");
                var isErrorMessage = logLevel >= LogLevel.Error;

                _lineNumber++;
                var logLine = new LogLine(_lineNumber, log, isErrorMessage);

                if (useBacklog)
                {
                    lock (loggerState._backlog)
                    {
                        loggerState._backlog.Add(logLine);
                    }
                }

                loggerState._onNewLog?.Invoke(logLine);
            }
        }

        private sealed class LogAsyncEnumerable(ResourceLoggerState state, LogLine[] backlogSnapshot) : IAsyncEnumerable<IReadOnlyList<LogLine>>
        {
            public async IAsyncEnumerator<IReadOnlyList<LogLine>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                if (backlogSnapshot.Length > 0)
                {
                    yield return backlogSnapshot;
                }

                var channel = Channel.CreateUnbounded<LogLine>();

                using var _ = state._logStreamCts.Token.Register(() => channel.Writer.TryComplete());

                void Log(LogLine log)
                {
                    channel.Writer.TryWrite(log);
                }

                state.OnNewLog += Log;

                try
                {
                    await foreach (var entry in channel.GetBatchesAsync(cancellationToken: cancellationToken))
                    {
                        yield return entry;
                    }
                }
                finally
                {
                    state.OnNewLog -= Log;

                    channel.Writer.TryComplete();
                }
            }
        }
    }
}

/// <summary>
/// Represents a log subscriber for a resource.
/// </summary>
/// <param name="Name">The the resource name.</param>
/// <param name="AnySubscribers">Determines if there are any subscribers.</param>
public record struct LogSubscriber(string Name, bool AnySubscribers);
