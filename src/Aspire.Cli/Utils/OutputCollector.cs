// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Diagnostics;

namespace Aspire.Cli.Utils;

internal sealed class OutputCollector
{
    private readonly CircularBuffer<(string Stream, string Line)> _lines = new(10000); // 10k lines.
    private readonly object _lock = new object();
    private readonly FileLoggerProvider? _fileLogger;
    private readonly string _category;

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
    public OutputCollector(FileLoggerProvider? fileLogger, string category = "AppHost")
    {
        _fileLogger = fileLogger;
        _category = category;
    }

    public void AppendOutput(string line)
    {
        lock (_lock)
        {
            _lines.Add(("stdout", line));
            _fileLogger?.WriteLog(FormatLogLine("stdout", line));
        }
    }

    public void AppendError(string line)
    {
        lock (_lock)
        {
            _lines.Add(("stderr", line));
            _fileLogger?.WriteLog(FormatLogLine("stderr", line));
        }
    }

    public IEnumerable<(string Stream, string Line)> GetLines()
    {
        lock (_lock)
        {
            return _lines.ToArray();
        }
    }

    private string FormatLogLine(string stream, string line)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        var level = stream == "stderr" ? "FAIL" : "INFO";
        return $"[{timestamp}] [{level}] [{_category}] {line}";
    }
}