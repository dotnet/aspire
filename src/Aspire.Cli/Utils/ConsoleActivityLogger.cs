// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Aspire.Cli.Utils;

/// <summary>
/// Lightweight, spec-aligned console logger for aligned colored task output without
/// rewriting the entire existing publishing pipeline. Integrates by mapping publish
/// step/task events to Start/Progress/Success/Warning/Failure calls.
/// </summary>
internal sealed class ConsoleActivityLogger
{
    private const int TaskWidth = 20; // Reserved for potential fixed width alignment (currently not truncating)
    private readonly bool _enableColor;
    private readonly object _lock = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Dictionary<string, string> _stepColors = new();
    private readonly string[] _availableColors = ["blue", "cyan", "yellow", "magenta", "purple", "orange3"];
    private int _colorIndex;

    private int _successCount;
    private int _warningCount;
    private int _failureCount;
    private volatile bool _spinning;
    private Task? _spinnerTask;
    private readonly char[] _spinnerChars = ['|', '/', '-', '\\'];
    private int _spinnerIndex;

    // No raw ANSI escape codes; rely on Spectre.Console markup tokens.

    private const string SuccessSymbol = "✓";
    private const string FailureSymbol = "✗";
    private const string WarningSymbol = "⚠";
    private const string InProgressSymbol = "→";
    private const string InfoSymbol = "i";

    public ConsoleActivityLogger(bool? forceColor = null)
    {
        _enableColor = forceColor ?? DetectColorSupport();
    }

    public enum ActivityState
    {
        InProgress,
        Success,
        Warning,
        Failure,
        Info
    }

    public void StartTask(string taskKey, string? startingMessage = null)
    {
        WriteLine(taskKey, InProgressSymbol, startingMessage ?? "Starting...", ActivityState.InProgress);
    }

    public void StartSpinner()
    {
        if (_spinning)
        {
            return;
        }
        _spinning = true;
        _spinnerTask = Task.Run(async () =>
        {
            // Spinner sits at bottom; we write spinner char then backspace.
            while (_spinning)
            {
                AnsiConsole.Write(CultureInfo.InvariantCulture, _spinnerChars[_spinnerIndex % _spinnerChars.Length]);
                AnsiConsole.Write(CultureInfo.InvariantCulture, "\b");
                _spinnerIndex++;
                await Task.Delay(120).ConfigureAwait(false);
            }
            // Clear spinner character
            AnsiConsole.Write(CultureInfo.InvariantCulture, ' ');
            AnsiConsole.Write(CultureInfo.InvariantCulture, "\b");
        });
    }

    public async Task StopSpinnerAsync()
    {
        _spinning = false;
        if (_spinnerTask is not null)
        {
            await _spinnerTask.ConfigureAwait(false);
            _spinnerTask = null;
        }
    }

    public void Progress(string taskKey, string message)
    {
    WriteLine(taskKey, InProgressSymbol, message, ActivityState.InProgress);
    }

    public void Success(string taskKey, string message, double? seconds = null)
    {
        lock (_lock)
        {
            _successCount++;
        }
        WriteCompletion(taskKey, SuccessSymbol, message, ActivityState.Success, seconds);
    }

    public void Warning(string taskKey, string message, double? seconds = null)
    {
        lock (_lock)
        {
            _warningCount++;
        }
        WriteCompletion(taskKey, WarningSymbol, message, ActivityState.Warning, seconds);
    }

    public void Failure(string taskKey, string message, double? seconds = null)
    {
        lock (_lock)
        {
            _failureCount++;
        }
        WriteCompletion(taskKey, FailureSymbol, message, ActivityState.Failure, seconds);
    }

    public void Info(string taskKey, string message)
    {
        WriteLine(taskKey, InfoSymbol, message, ActivityState.Info);
    }

    public void Continuation(string message)
    {
        lock (_lock)
        {
            var indent = new string(' ', 8 /* time */ + 3 /* gap */ + TaskWidth + 2 /* gap */ + 2 /* symbol + space */);
            foreach (var line in SplitLinesPreserve(message))
            {
                Console.Write(indent);
                Console.WriteLine(line);
            }
        }
    }

    public void WriteSummary(string? dashboardUrl = null)
    {
        lock (_lock)
        {
            var totalSeconds = _stopwatch.Elapsed.TotalSeconds;
            var line = new string('-', 60);
            AnsiConsole.MarkupLine(line);

            var parts = new List<string>();
            if (_enableColor)
            {
                parts.Add($"[green]{SuccessSymbol} {_successCount} succeeded[/]");
                if (_warningCount > 0)
                {
                    parts.Add($"[yellow]{WarningSymbol} {_warningCount} warning{(_warningCount == 1 ? string.Empty : "s")}[/]");
                }
                if (_failureCount > 0)
                {
                    parts.Add($"[red]{FailureSymbol} {_failureCount} failed[/]");
                }
            }
            else
            {
                parts.Add($"{SuccessSymbol} {_successCount} succeeded");
                if (_warningCount > 0)
                {
                    parts.Add($"{WarningSymbol} {_warningCount} warning{(_warningCount == 1 ? string.Empty : "s")}");
                }
                if (_failureCount > 0)
                {
                    parts.Add($"{FailureSymbol} {_failureCount} failed");
                }
            }

            parts.Add($"Total time: {totalSeconds.ToString("0.0", CultureInfo.InvariantCulture)}s");
            AnsiConsole.MarkupLine(string.Join(" • ", parts));
            if (!string.IsNullOrEmpty(dashboardUrl))
            {
                // Render dashboard URL as clickable link
                var url = dashboardUrl;
                AnsiConsole.MarkupLine($"Dashboard: [link={url}]{url}[/]");
            }
            AnsiConsole.MarkupLine(line);
            AnsiConsole.WriteLine(); // Ensure final newline after deployment summary
        }
    }

    private void WriteCompletion(string taskKey, string symbol, string message, ActivityState state, double? seconds)
    {
        var text = seconds.HasValue ? $"{message} ({seconds.Value.ToString("0.0", CultureInfo.InvariantCulture)}s)" : message;
        WriteLine(taskKey, symbol, text, state);
    }

    private void WriteLine(string taskKey, string symbol, string message, ActivityState state)
    {
        lock (_lock)
        {
            var time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            var stepColor = GetOrAssignStepColor(taskKey);
            var coloredSymbol = _enableColor ? ColorizeSymbol(symbol, state) : symbol;

            foreach (var line in SplitLinesPreserve(message))
            {
                // Format: dim timestamp, colored step tag, symbol, escaped message
                var escapedLine = HighlightAndEscape(line);
                var escapedTask = taskKey.EscapeMarkup();
                var markup = new StringBuilder();
                markup.Append("[dim]").Append(time).Append("[/] ");
                markup.Append('[').Append(stepColor).Append(']').Append('(').Append(escapedTask).Append(')').Append("[/] ");
                if (_enableColor)
                {
                    markup.Append(coloredSymbol).Append(' ').Append(escapedLine);
                }
                else
                {
                    markup.Append(symbol).Append(' ').Append(escapedLine);
                }
                AnsiConsole.MarkupLine(markup.ToString());
            }
        }
    }

    private string GetOrAssignStepColor(string taskKey)
    {
        if (!_stepColors.TryGetValue(taskKey, out var color))
        {
            color = _availableColors[_colorIndex % _availableColors.Length];
            _stepColors[taskKey] = color;
            _colorIndex++;
        }
        return color;
    }

    private static IEnumerable<string> SplitLinesPreserve(string message)
    {
        if (message.IndexOf('\n') < 0)
        {
            yield return message;
            yield break;
        }
        var lines = message.Replace("\r\n", "\n").Split('\n');
        foreach (var l in lines)
        {
            yield return l;
        }
    }

    private static string ColorizeSymbol(string symbol, ActivityState state) => state switch
    {
        ActivityState.Success => $"[green]{symbol}[/]",
        ActivityState.Warning => $"[yellow]{symbol}[/]",
        ActivityState.Failure => $"[red]{symbol}[/]",
        ActivityState.InProgress => $"[cyan]{symbol}[/]",
        ActivityState.Info => $"[dim]{symbol}[/]",
        _ => symbol
    };

    private static readonly Regex s_urlRegex = new(
        pattern: @"(?:(?:https?|ftp)://)[^\s]+",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // Escapes non-URL portions for Spectre markup while preserving injected [link] markup unescaped.
    private static string HighlightAndEscape(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var matches = s_urlRegex.Matches(input);
        if (matches.Count == 0)
        {
            return input.EscapeMarkup();
        }

        var sb = new StringBuilder(input.Length + 32);
        var lastIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                var segment = input.Substring(lastIndex, match.Index - lastIndex);
                sb.Append(segment.EscapeMarkup());
            }
            var url = match.Value; // Do not EscapeMarkup inside [link] to keep it functional.
            sb.Append("[link=").Append(url).Append(']').Append(url).Append("[/]");
            lastIndex = match.Index + match.Length;
        }
        if (lastIndex < input.Length)
        {
            sb.Append(input.Substring(lastIndex).EscapeMarkup());
        }
        return sb.ToString();
    }

    // (kept previously for fixed-width layout; currently unused after switching to markup layout)
    // private static string Truncate(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";

    private static bool DetectColorSupport()
    {
        try
        {
            if (Console.IsOutputRedirected)
            {
                return false;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true; // Modern Windows terminals support ANSI
            }
            return true; // Assume ANSI on Unix
        }
        catch
        {
            return false;
        }
    }
}
