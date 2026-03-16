// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Cli.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

internal sealed class OutputCollector
{
    private readonly CircularBuffer<(string Stream, string Line)> _lines = new(10000); // 10k lines.
    private readonly object _lock = new object();
    private readonly FileLoggerProvider? _fileLogger;
    private readonly string _category;
    private readonly Action<string, string>? _liveOutputCallback;
    private readonly Channel<(string Stream, string Line)>? _liveOutputChannel;
    private readonly Task? _liveOutputTask;
    private int _liveOutputCompleted;

    /// <summary>
    /// Creates an OutputCollector that only buffers output in memory.
    /// </summary>
    public OutputCollector() : this(null, "AppHost")
    {
    }

    /// <summary>
    /// Creates an OutputCollector that buffers output and optionally logs to disk.
    /// </summary>
    /// <param name="fileLogger">Optional file logger for writing output to disk.</param>
    /// <param name="category">Category for log entries (e.g., "Build", "AppHost").</param>
    /// <param name="liveOutputCallback">Optional callback invoked immediately when a line is appended.</param>
    public OutputCollector(FileLoggerProvider? fileLogger, string category = "AppHost", Action<string, string>? liveOutputCallback = null)
    {
        _fileLogger = fileLogger;
        _category = category;
        _liveOutputCallback = liveOutputCallback;

        if (liveOutputCallback is not null)
        {
            _liveOutputChannel = Channel.CreateUnbounded<(string Stream, string Line)>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
            _liveOutputTask = ProcessLiveOutputAsync(_liveOutputChannel.Reader, liveOutputCallback);
        }
    }

    public bool HasLiveOutputCallback => _liveOutputCallback is not null;

    public void AppendOutput(string line)
    {
        AppendLine("stdout", line);
    }

    public void AppendError(string line)
    {
        AppendLine("stderr", line);
    }

    public IEnumerable<(string Stream, string Line)> GetLines()
    {
        lock (_lock)
        {
            return _lines.ToArray();
        }
    }

    public async Task FlushLiveOutputAsync(CancellationToken cancellationToken = default)
    {
        if (_liveOutputChannel is null || _liveOutputTask is null)
        {
            return;
        }

        if (Interlocked.Exchange(ref _liveOutputCompleted, 1) == 0)
        {
            _liveOutputChannel.Writer.TryComplete();
        }

        await _liveOutputTask.WaitAsync(cancellationToken);
    }

    private void AppendLine(string stream, string line)
    {
        lock (_lock)
        {
            _lines.Add((stream, line));
            _fileLogger?.WriteLog(DateTimeOffset.UtcNow, stream == "stderr" ? LogLevel.Error : LogLevel.Information, _category, line);
        }

        if (_liveOutputChannel is not null && !_liveOutputChannel.Writer.TryWrite((stream, line)))
        {
            throw new InvalidOperationException("Live output callback cannot accept additional lines.");
        }
    }

    private static async Task ProcessLiveOutputAsync(ChannelReader<(string Stream, string Line)> reader, Action<string, string> liveOutputCallback)
    {
        await foreach (var (stream, line) in reader.ReadAllAsync())
        {
            liveOutputCallback(stream, line);
        }
    }
}
