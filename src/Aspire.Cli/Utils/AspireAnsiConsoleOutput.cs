// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Aspire.Cli.Utils;

/// <summary>
/// Custom console output implementation that addresses Spectre.Console width detection issues in CI environments.
/// </summary>
/// <remarks>
/// <para>
/// In CI environments (Jenkins, GitHub Actions, etc.) and when output is redirected, Spectre.Console
/// defaults to 80 columns which causes awkward line wrapping in logs. This implementation:
/// </para>
/// <list type="bullet">
/// <item>Detects when running in non-terminal environments and automatically uses 160 columns instead of 80</item>
/// <item>Respects the ASPIRE_CONSOLE_WIDTH environment variable for explicit width overrides (capped at 500)</item>
/// <item>Handles IOException gracefully when console buffer information is unavailable</item>
/// </list>
/// <para>
/// This addresses https://github.com/spectreconsole/spectre.console/issues/216 and provides a better
/// experience for CI logs without requiring manual width configuration.
/// </para>
/// </remarks>
internal sealed class AspireAnsiConsoleOutput : IAnsiConsoleOutput
{
    private readonly TextWriter _writer;
    private readonly IConfiguration _configuration;
    private int? _width;

    /// <inheritdoc/>
    public TextWriter Writer => _writer;

    /// <inheritdoc/>
    public bool IsTerminal
    {
        get
        {
            if (IsStandardOut(_writer))
            {
                return !Console.IsOutputRedirected;
            }

            if (IsStandardError(_writer))
            {
                return !Console.IsErrorRedirected;
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public int Width
    {
        get => _width ?? GetConfiguredWidth();
        set => _width = value;
    }

    /// <inheritdoc/>
    public int Height => GetSafeHeight();

    public AspireAnsiConsoleOutput(TextWriter writer, IConfiguration configuration)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    private int GetConfiguredWidth()
    {
        // Check if explicit width override is set via ASPIRE_CONSOLE_WIDTH
        var consoleWidthOverride = _configuration["ASPIRE_CONSOLE_WIDTH"];
        if (!string.IsNullOrEmpty(consoleWidthOverride) && 
            int.TryParse(consoleWidthOverride, out var width) && 
            width > 0)
        {
            // Cap at reasonable maximum to prevent performance issues
            return Math.Min(width, 500);
        }

        var detectedWidth = GetSafeWidth();

        // In scenarios where CLI is not running on a terminal, Spectre.Console defaults to 80 columns
        // which is too narrow and causes unexpected line breaks in CI logs. Double the value to 160.
        if (!IsTerminal && detectedWidth == 80)
        {
            return 160;
        }

        return detectedWidth;
    }

    /// <inheritdoc/>
    public void SetEncoding(Encoding encoding)
    {
        if (IsStandardOut(_writer) || IsStandardError(_writer))
        {
            Console.OutputEncoding = encoding;
        }
    }

    private static bool IsStandardOut(TextWriter writer)
    {
        try
        {
            return writer == Console.Out;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsStandardError(TextWriter writer)
    {
        try
        {
            return writer == Console.Error;
        }
        catch
        {
            return false;
        }
    }

    private static int GetSafeWidth()
    {
        try
        {
            var width = Console.BufferWidth;
            if (width == 0)
            {
                // Return default terminal width when buffer width is 0
                return 80;
            }

            return width;
        }
        catch (IOException)
        {
            // When console is redirected (CI environments), return the default width
            // The caller will detect this and double it to 160 for better readability
            return 80;
        }
    }

    private static int GetSafeHeight()
    {
        try
        {
            var height = Console.WindowHeight;
            if (height == 0)
            {
                return 24;
            }

            return height;
        }
        catch (IOException)
        {
            return 24;
        }
    }
}
