// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace Aspire.Dashboard.ConsoleLogs;

public class AnsiParser
{
    private const char EscapeChar = '\x1B';
    private const int ResetCode = 0;
    private const int IncreasedIntensityCode = 1;
    private const int NormalIntensityCode = 22;
    private const int DefaultForegroundCode = 39;
    private const int DefaultBackgroundCode = 49;

    public static ConversionResult ConvertToHtml(string? text, ParserState? priorResidualState = null)
    {
        var textStartIndex = -1;
        var textLength = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return new(text, default);
        }

        var span = text.AsSpan();

        ParserState currentState = default;
        var newState = priorResidualState ?? default;

        var outputBuilder = new StringBuilder(text.Length * 2);

        for (var i = 0; i < span.Length; i++)
        {
            if (IsControlSequence(span, i, out var parameter))
            {
                // If we have a control sequence, but have found some text already,
                // we need to write out the new styles (if applicable) and that text
                // before we continue
                if (textStartIndex != -1)
                {
                    if (newState != currentState)
                    {
                        outputBuilder.Append(ProcessStateChange(currentState, newState));
                        currentState = newState;
                    }
                    outputBuilder.Append(text[textStartIndex..(textStartIndex + textLength)]);
                    textStartIndex = -1;
                    textLength = 0;
                }

                ProcessParameter(ref newState, parameter);

                i += (parameter >= 10) ? 4 : 3;
                continue;
            }

            // If it wasn't a control sequence, then it must be text, so figure
            // out how much text before the next control sequence (if any)
            if (textStartIndex == -1)
            {
                textStartIndex = i;
            }
            var nextEscapeIndex = -1;
            if (i < text.Length - 1)
            {
                nextEscapeIndex = text.IndexOf(EscapeChar, i + 1);
            }

            // If there's no more control sequences, capture the text length and we're done
            if (nextEscapeIndex < 0)
            {
                textLength = text.Length - textStartIndex;
                break;
            }

            // If there is another control sequence, capture the text length and process it
            textLength = nextEscapeIndex - textStartIndex;
            i = nextEscapeIndex - 1;
        }

        // If we reached the end and have built up a new style, write it out
        if (newState != currentState)
        {
            outputBuilder.Append(ProcessStateChange(currentState, newState));
            currentState = newState;
        }

        // If there's any text left, right that out too
        if (textStartIndex != -1)
        {
            outputBuilder.Append(text[textStartIndex..(textStartIndex + textLength)]);
        }

        // Ensure we always close off the current span. The next log line will
        // pick up the residual state if necessary
        outputBuilder.Append(ProcessStateChange(currentState, default));

        return new(outputBuilder.ToString(), currentState);
    }

    private static void ProcessParameter(ref ParserState newState, int parameter)
    {
        if (TryGetForegroundColor(parameter, out var color))
        {
            newState.ForegroundColor = color;
        }
        else if (TryGetBackgroundColor(parameter, out color))
        {
            newState.BackgroundColor = color;
        }
        else if (parameter == DefaultBackgroundCode)
        {
            newState.BackgroundColor = null;
        }
        else if (parameter == DefaultForegroundCode)
        {
            newState.ForegroundColor = null;
        }
        else if (parameter == NormalIntensityCode)
        {
            newState.Bright = false;
        }
        else if (parameter == ResetCode)
        {
            newState = default;
        }
        else if (parameter == IncreasedIntensityCode)
        {
            newState.Bright = true;
        }
    }

    private static bool IsControlSequence(ReadOnlySpan<char> span, int position, out int parameter)
    {
        // If we're at \x1B[ and the span is at least long enough for a single digit parameter
        if (span[position] == EscapeChar && span.Length >= position + 4 && span[position + 1] == '[')
        {
            // If we've got a single digit parameter and the closing m
            if (span[position + 3] == 'm' && IsDigit(span[position + 2]))
            {
                parameter = span[position + 2] - '0';
                return true;
            }
            // If we've got a two digit parameter and the closing m
            else if (span.Length >= position + 5 && span[position + 4] == 'm' && IsDigit(span[position + 2]) && IsDigit(span[position + 3]))
            {
                parameter = (span[position + 2] - '0') * 10 + (span[position + 3] - '0');
                return true;
            }
        }

        parameter = -1;
        return false;
    }

    private static string ProcessStateChange(ParserState currentState, ParserState newState)
    {
        var closeSpanIfNeeded = "";
        if (currentState != default)
        {
            closeSpanIfNeeded += "</span>";
        }

        if (newState.ForegroundColor.HasValue && newState.BackgroundColor.HasValue)
        {
            return closeSpanIfNeeded + $"<span class=\"{GetForegroundColorClass(newState)} {GetBackgroundColorClass(newState)}\">";
        }
        else if (newState.ForegroundColor.HasValue)
        {
            return closeSpanIfNeeded + $"<span class=\"{GetForegroundColorClass(newState)}\">";
        }
        else if (newState.BackgroundColor.HasValue)
        {
            return closeSpanIfNeeded + $"<span class=\"{GetBackgroundColorClass(newState)}\">";
        }
        else
        {
            return closeSpanIfNeeded;
        }
    }

    private static string? GetForegroundColorClass(ParserState state)
    {
        return state.ForegroundColor switch
        {
            ConsoleColor.Black   => state.Bright ? "ansi-fg-brightblack"   : "ansi-fg-black",
            ConsoleColor.Blue    => state.Bright ? "ansi-fg-brightblue"    : "ansi-fg-blue",
            ConsoleColor.Cyan    => state.Bright ? "ansi-fg-brightcyan"    : "ansi-fg-cyan",
            ConsoleColor.Green   => state.Bright ? "ansi-fg-brightgreen"   : "ansi-fg-green",
            ConsoleColor.Magenta => state.Bright ? "ansi-fg-brightmagenta" : "ansi-fg-magenta",
            ConsoleColor.Red     => state.Bright ? "ansi-fg-brightred"     : "ansi-fg-red",
            ConsoleColor.White   => state.Bright ? "ansi-fg-brightwhite"   : "ansi-fg-white",
            ConsoleColor.Yellow  => state.Bright ? "ansi-fg-brightyellow"  : "ansi-fg-yellow",
            _ => ""
        };
    }

    private static string? GetBackgroundColorClass(ParserState state)
    {
        return state.BackgroundColor switch
        {
            ConsoleColor.Black   => "ansi-bg-black",
            ConsoleColor.Blue    => "ansi-bg-blue",
            ConsoleColor.Cyan    => "ansi-bg-cyan",
            ConsoleColor.Green   => "ansi-bg-green",
            ConsoleColor.Magenta => "ansi-bg-magenta",
            ConsoleColor.Red     => "ansi-bg-red",
            ConsoleColor.White   => "ansi-bg-white",
            ConsoleColor.Yellow  => "ansi-bg-yellow",
            _ => ""
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(char c) => (uint)(c - '0') <= ('9' - '0');

    private static bool TryGetForegroundColor(int number, out ConsoleColor? color)
    {
        color = number switch
        {
            30 => ConsoleColor.Black,
            31 => ConsoleColor.Red,
            32 => ConsoleColor.Green,
            33 => ConsoleColor.Yellow,
            34 => ConsoleColor.Blue,
            35 => ConsoleColor.Magenta,
            36 => ConsoleColor.Cyan,
            37 => ConsoleColor.White,
            _ => null
        };
        return color != null || number == 39;
    }

    private static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
    {
        color = number switch
        {
            40 => ConsoleColor.Black,
            41 => ConsoleColor.Red,
            42 => ConsoleColor.Green,
            43 => ConsoleColor.Yellow,
            44 => ConsoleColor.Blue,
            45 => ConsoleColor.Magenta,
            46 => ConsoleColor.Cyan,
            47 => ConsoleColor.White,
            _ => null
        };
        return color != null || number == 49;
    }

    public record struct ParserState
    {
        public ConsoleColor? ForegroundColor { get; set; }
        public ConsoleColor? BackgroundColor { get; set; }
        public bool Bright { get; set; }
    }

    public readonly record struct ConversionResult(string? ConvertedText, ParserState ResidualState);
}
