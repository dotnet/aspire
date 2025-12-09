// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Spectre.Console;

namespace Aspire.Cli.Utils;

/// <summary>
/// Custom console output that provides better width detection for CI environments.
/// Addresses https://github.com/spectreconsole/spectre.console/issues/216 where
/// console width defaults to 80 in non-terminal environments.
/// </summary>
internal sealed class AspireAnsiConsoleOutput : IAnsiConsoleOutput
{
    private readonly TextWriter _writer;
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
        get => _width ?? GetSafeWidth();
        set => _width = value;
    }

    /// <inheritdoc/>
    public int Height => GetSafeHeight();

    public AspireAnsiConsoleOutput(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
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
                // In CI environments, default to a reasonable width
                return 160;
            }

            return width;
        }
        catch (IOException)
        {
            // When console is redirected (CI environments), use a reasonable default
            return 160;
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
