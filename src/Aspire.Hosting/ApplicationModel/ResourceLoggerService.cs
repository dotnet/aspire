// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
    /// <returns>An <see cref="ILogger"/> which represents the resource.</returns>
    public ILogger GetLogger(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return GetResourceLoggerState(resource.Name).Logger;
    }

    /// <summary>
    /// Gets the logger for the resource to write to.
    /// </summary>
    /// <param name="resourceName">The name of the resource from the Aspire application model.</param>
    /// <returns>An <see cref="ILogger"/> which represents the named resource.</returns>
    public ILogger GetLogger(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        return GetResourceLoggerState(resourceName).Logger;
    }

    /// <summary>
    /// Watch for changes to the log stream for a resource.
    /// </summary>
    /// <param name="resource">The resource to watch for logs.</param>
    /// <returns>An async enumerable that returns the logs as they are written.</returns>
    public IAsyncEnumerable<IReadOnlyList<LogLine>> WatchAsync(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return WatchAsync(resource.Name);
    }

    /// <summary>
    /// Watch for changes to the log stream for a resource.
    /// </summary>
    /// <param name="resourceName">The resource name</param>
    /// <returns>An async enumerable that returns the logs as they are written.</returns>
    public IAsyncEnumerable<IReadOnlyList<LogLine>> WatchAsync(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        return GetResourceLoggerState(resourceName).WatchAsync();
    }

    /// <summary>
    /// Watch for subscribers to the log stream for a resource.
    /// </summary>
    /// <returns>
    /// An async enumerable that returns when the first subscriber is added to a log,
    /// or when the last subscriber is removed.
    /// </returns>
    public async IAsyncEnumerable<LogSubscriber> WatchAnySubscribersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        LoggerAdded += OnLoggerAdded;

        try
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return entry;
            }
        }
        finally
        {
            LoggerAdded -= OnLoggerAdded;
        }
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

    /// <summary>
    /// Clears the log stream's backlog for the resource.
    /// </summary>
    public void ClearBacklog(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        if (_loggers.TryGetValue(resourceName, out var logger))
        {
            logger.ClearBacklog();
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

    /// <summary>
    /// A logger for the resource to write to.
    /// </summary>
    private sealed class ResourceLoggerState
    {
        private readonly ResourceLogger _logger;
        private readonly CancellationTokenSource _logStreamCts = new();

        private readonly CircularBuffer<LogLine> _backlog = new(10000);

        /// <summary>
        /// Creates a new <see cref="ResourceLoggerState"/>.
        /// </summary>
        public ResourceLoggerState()
        {
            _logger = new ResourceLogger(this);
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
                    if (_onNewLog is not null) // we have subscribers
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
        public async IAsyncEnumerable<IReadOnlyList<LogLine>> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<LogLine>();

            using var _ = _logStreamCts.Token.Register(() => channel.Writer.TryComplete());

            void Log(LogLine log)
            {
                channel.Writer.TryWrite(log);
            }

            OnNewLog += Log;

            // ensure the backlog snapshot is taken after subscribing to OnNewLog
            // to ensure the backlog snapshot contains the correct logs. The backlog
            // can get cleared when there are no subscribers, so we ensure we are subscribing first.

            // REVIEW: Performance makes me very sad, but we can optimize this later.
            var backlogSnapshot = GetBacklogSnapshot();
            if (backlogSnapshot.Length > 0)
            {
                yield return backlogSnapshot;
            }

            try
            {
                await foreach (var entry in channel.GetBatchesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
            finally
            {
                OnNewLog -= Log;

                channel.Writer.TryComplete();
            }
        }

        // This provides the fan out to multiple subscribers.
        private Action<LogLine>? _onNewLog;
        private event Action<LogLine> OnNewLog
        {
            add
            {
                bool raiseSubscribersChanged;
                lock (this)
                {
                    raiseSubscribersChanged = _onNewLog is null; // is this the first subscriber?

                    _onNewLog += value;
                }

                if (raiseSubscribersChanged)
                {
                    _onSubscribersChanged?.Invoke(true);
                }
            }
            remove
            {
                bool raiseSubscribersChanged;
                lock (this)
                {
                    _onNewLog -= value;

                    raiseSubscribersChanged = _onNewLog is null; // is this the last subscriber?
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

        /// <summary>
        /// Close the log stream for the resource. Future subscribers will not receive any updates and will complete immediately.
        /// </summary>
        public void Complete()
        {
            // REVIEW: Do we clean up the backlog?
            _logStreamCts.Cancel();
        }

        public void ClearBacklog()
        {
            lock (_backlog)
            {
                _backlog.Clear();
            }
        }

        private LogLine[] GetBacklogSnapshot()
        {
            lock (_backlog)
            {
                return [.. _backlog];
            }
        }

        private sealed class ResourceLogger(ResourceLoggerState loggerState) : ILogger
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

                LogLine logLine;
                lock (loggerState._backlog)
                {
                    _lineNumber++;
                    logLine = new LogLine(_lineNumber, log, isErrorMessage);

                    loggerState._backlog.Add(logLine);
                }

                loggerState._onNewLog?.Invoke(logLine);
            }
        }
    }
}

/// <summary>
/// Represents a log subscriber for a resource.
/// </summary>
/// <param name="Name">The the resource name.</param>
/// <param name="AnySubscribers">Determines if there are any subscribers.</param>
public readonly record struct LogSubscriber(string Name, bool AnySubscribers);
