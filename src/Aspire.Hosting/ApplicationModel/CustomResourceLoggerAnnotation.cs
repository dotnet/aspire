// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A annotation that exposes a Logger for the resource to write to.
/// </summary>
public sealed class CustomResourceLoggerAnnotation : IResourceAnnotation
{
    private readonly ResourceLogger _logger;

    // History of logs, capped at 10000 entries.
    private readonly CircularBuffer<(string Content, bool IsErrorMessage)> _backlog = new(10000);

    /// <summary>
    /// Creates a new <see cref="CustomResourceLoggerAnnotation"/>.
    /// </summary>
    public CustomResourceLoggerAnnotation()
    {
        _logger = new ResourceLogger(this);
    }

    /// <summary>
    /// Watch for changes to the log stream for a resource.
    /// </summary>
    /// <returns> The log stream for the resource. </returns>
    public IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>> WatchAsync() => new LogAsyncEnumerable(this);

    // This provides the fan out to multiple subscribers.
    private Action<(string, bool)>? OnNewLog { get; set; }

    /// <summary>
    /// The logger for the resource to write to. This will write updates to the live log stream for this resource.
    /// </summary>
    public ILogger Logger => _logger;

    private sealed class ResourceLogger(CustomResourceLoggerAnnotation annotation) : ILogger
    {
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var log = formatter(state, exception) + (exception is null ? "" : $"\n{exception}");
            var isErrorMessage = logLevel >= LogLevel.Error;

            var payload = (log, isErrorMessage);

            lock (annotation._backlog)
            {
                annotation._backlog.Add(payload);
            }

            annotation.OnNewLog?.Invoke(payload);
        }
    }
    private sealed class LogAsyncEnumerable(CustomResourceLoggerAnnotation annotation) : IAsyncEnumerable<IReadOnlyList<(string, bool)>>
    {
        public async IAsyncEnumerator<IReadOnlyList<(string, bool)>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            // Yield the backlog first.

            lock (annotation._backlog)
            {
                if (annotation._backlog.Count > 0)
                {
                    // REVIEW: Performance makes me very sad, but we can optimize this later.
                    yield return annotation._backlog.ToList();
                }
            }

            var channel = Channel.CreateUnbounded<(string, bool)>();

            void Log((string Content, bool IsErrorMessage) log)
            {
                channel.Writer.TryWrite(log);
            }

            annotation.OnNewLog += Log;

            try
            {
                await foreach (var entry in channel.GetBatches(cancellationToken))
                {
                    yield return entry;
                }
            }
            finally
            {
                annotation.OnNewLog -= Log;

                channel.Writer.TryComplete();
            }
        }
    }
}
