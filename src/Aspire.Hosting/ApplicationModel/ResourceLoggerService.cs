// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
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
    /// The internal logger is used when adding logs from resource's stream logs.
    /// It allows the parsed date from text to be used as the log line date.
    /// </summary>
    internal Action<DateTime, string, bool> GetInternalLogger(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        return GetResourceLoggerState(resourceName).AddLog;
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

    // Internal for testing.
    internal ResourceLoggerState GetResourceLoggerState(string resourceName) =>
        _loggers.GetOrAdd(resourceName, (name, context) =>
        {
            var state = new ResourceLoggerState();
            context._loggerAdded?.Invoke((name, state));
            return state;
        },
        this);

    internal sealed record InternalLogLine(DateTime DateTimeUtc, string Message, bool IsError);

    /// <summary>
    /// A logger for the resource to write to.
    /// </summary>
    internal sealed class ResourceLoggerState
    {
        private readonly ResourceLogger _logger;
        private readonly CancellationTokenSource _logStreamCts = new();

        private Task? _backlogReplayCompleteTask;
        private long _lastLogReceivedTimestamp;
        private readonly CircularBuffer<InternalLogLine> _backlog = new(10000);

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
            // Line number always restarts from 1 when watching logs.
            // Note that this will need to be improved if the log source (DCP) is changed to return a maximum number of lines.
            var lineNumber = 1;
            var channel = Channel.CreateUnbounded<InternalLogLine>();

            using var _ = _logStreamCts.Token.Register(() => channel.Writer.TryComplete());

            InternalLogLine[]? backlogSnapshot = null;
            void Log(InternalLogLine log)
            {
                lock (_backlog)
                {
                    // Don't write to the channel until the backlog snapshot is accessed.
                    // This prevents duplicate logs in result.
                    if (backlogSnapshot != null)
                    {
                        channel.Writer.TryWrite(log);
                    }
                }
            }
            OnNewLog += Log;

            // Add a small delay to ensure the backlog is replay and ordered correctly.
            await EnsureBacklogReplayAsync(cancellationToken).ConfigureAwait(false);

            lock (_backlog)
            {
                backlogSnapshot = GetBacklogSnapshot();
            }

            if (backlogSnapshot.Length > 0)
            {
                yield return CreateLogLines(ref lineNumber, backlogSnapshot);
            }

            try
            {
                await foreach (var entry in channel.GetBatchesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    yield return CreateLogLines(ref lineNumber, entry);
                }
            }
            finally
            {
                OnNewLog -= Log;

                channel.Writer.TryComplete();
            }

            static LogLine[] CreateLogLines(ref int lineNumber, IReadOnlyList<InternalLogLine> entry)
            {
                var logs = new LogLine[entry.Count];
                for (var i = 0; i < entry.Count; i++)
                {
                    logs[i] = new LogLine(lineNumber, entry[i].Message, entry[i].IsError);
                    lineNumber++;
                }

                return logs;
            }
        }

        private Task EnsureBacklogReplayAsync(CancellationToken cancellationToken)
        {
            lock (_backlog)
            {
                _backlogReplayCompleteTask ??= StartBacklogReplayAsync(cancellationToken);
                return _backlogReplayCompleteTask;
            }

            async Task StartBacklogReplayAsync(CancellationToken cancellationToken)
            {
                var delay = TimeSpan.FromMilliseconds(100);

                // There could be an initial burst of logs as they're replayed. Give them the opporunity to be loaded
                // into the backlog in the correct order and returned before streaming logs as they arrive.
                for (var i = 0; i < 3; i++)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    lock (_backlog)
                    {
                        if (_lastLogReceivedTimestamp != 0 && Stopwatch.GetElapsedTime(_lastLogReceivedTimestamp) > delay)
                        {
                            break;
                        }
                    }
                }
            }
        }

        // This provides the fan out to multiple subscribers.
        private Action<InternalLogLine>? _onNewLog;
        private event Action<InternalLogLine> OnNewLog
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
                _backlogReplayCompleteTask = null;
            }
        }

        internal InternalLogLine[] GetBacklogSnapshot()
        {
            lock (_backlog)
            {
                return [.. _backlog];
            }
        }

        public void AddLog(DateTime dateTimeUtc, string logMessage, bool isErrorMessage)
        {
            InternalLogLine logLine;
            lock (_backlog)
            {
                logLine = new InternalLogLine(dateTimeUtc, logMessage, isErrorMessage);

                var added = false;
                for (var i = _backlog.Count - 1; i >= 0; i--)
                {
                    if (dateTimeUtc >= _backlog[i].DateTimeUtc)
                    {
                        _backlog.Insert(i + 1, logLine);
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    _backlog.Insert(0, logLine);
                }

                _lastLogReceivedTimestamp = Stopwatch.GetTimestamp();
            }

            _onNewLog?.Invoke(logLine);
        }

        private sealed class ResourceLogger(ResourceLoggerState loggerState) : ILogger
        {
            IDisposable? ILogger.BeginScope<TState>(TState state) => null;

            bool ILogger.IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (loggerState._logStreamCts.IsCancellationRequested)
                {
                    // Noop if logging after completing the stream
                    return;
                }

                var logMessage = formatter(state, exception) + (exception is null ? "" : $"\n{exception}");
                var isErrorMessage = logLevel >= LogLevel.Error;

                loggerState.AddLog(DateTime.UtcNow, logMessage, isErrorMessage);
            }
        }
    }

    internal static bool TryParseContentLineDate(string content, out DateTime value)
    {
        const int MinDateLength = 20; // Date + time without fractional seconds.
        const int MaxDateLength = 30; // Date + time with fractional seconds.

        if (content.Length >= MinDateLength)
        {
            var firstSpaceIndex = content.IndexOf(' ', StringComparison.Ordinal);
            if (firstSpaceIndex > 0)
            {
                if (DateTimeOffset.TryParse(content.AsSpan(0, firstSpaceIndex), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                {
                    value = dateTime.UtcDateTime;
                    return true;
                }
            }
            else if (content.Length <= MaxDateLength)
            {
                if (DateTimeOffset.TryParse(content, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                {
                    value = dateTime.UtcDateTime;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}

/// <summary>
/// Represents a log subscriber for a resource.
/// </summary>
/// <param name="Name">The the resource name.</param>
/// <param name="AnySubscribers">Determines if there are any subscribers.</param>
public readonly record struct LogSubscriber(string Name, bool AnySubscribers);
