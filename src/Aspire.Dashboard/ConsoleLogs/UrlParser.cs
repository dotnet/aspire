// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Aspire.Dashboard.ConsoleLogs;
public static partial class UrlParser
{
    private static readonly Regex s_urlRegEx = GenerateUrlRegEx();

    public static bool TryParse(string? text, [NotNullWhen(true)] out string? modifiedText)
    {
        if (text is not null)
        {
            var urlMatch = s_urlRegEx.Match(text);

            var builder = new StringBuilder(text.Length * 2);

            var nextCharIndex = 0;
            while (urlMatch.Success)
            {
                if (urlMatch.Index > 0)
                {
                    builder.Append(text[(nextCharIndex)..urlMatch.Index]);
                }

                var url = ReadUrl(text, urlMatch, out nextCharIndex);
                builder.Append(CultureInfo.InvariantCulture, $"<a target=\"_blank\" href=\"{url}\">{url}</a>");
                urlMatch = urlMatch.NextMatch();
            }

            if (builder.Length > 0)
            {
                if (nextCharIndex < text.Length)
                {
                    builder.Append(text[(nextCharIndex)..]);
                }

                modifiedText = builder.ToString();
                return true;
            }
        }

        modifiedText = null;
        return false;
    }

    // Regular expression that detects http/https URLs in a log entry
    // Based on the RegEx used in Windows Terminal for the same purpose, but limited
    // to only http/https URLs
    //
    // Explanation:
    // /b                             - Match must start at a word boundary (after whitespace or at the start of the text)
    // https?://                      - http:// or https://
    // [-A-Za-z0-9+&@#/%?=~_|$!:,.;]* - Any character in the list, matched zero or more times.
    // [A-Za-z0-9+&@#/%=~_|$]         - Any character in the list, matched exactly once
    [GeneratedRegex("\\bhttps?://[-A-Za-z0-9+&@#/%?=~_|$!:,.;]*[A-Za-z0-9+&@#/%=~_|$]")]
    private static partial Regex GenerateUrlRegEx();

    private static string ReadUrl(string text, Match match, out int nextCharIndex)
    {
        if (TryRemoveAnsiSequencesFromMatch(text, match.Index, out var noAnsi, out var removedCharacterCount))
        {
            var noAnsiMatch = s_urlRegEx.Match(noAnsi);
            nextCharIndex = noAnsiMatch.Index + noAnsiMatch.Length + removedCharacterCount;
            return noAnsiMatch.Value;
        }
        else
        {
            nextCharIndex = match.Index + match.Length;
            return match.Value;
        }
    }

    private static bool TryRemoveAnsiSequencesFromMatch(string text, int index, [NotNullWhen(true)] out string? trimmedWord, out int removedCharacterCount)
    {
        removedCharacterCount = 0;

        var nextSequenceStart = text.IndexOf('\x1B', index);
        if (nextSequenceStart < 0)
        {
            trimmedWord = null;
            return false;
        }

        var sb = new StringBuilder();

        while (nextSequenceStart > 0)
        {
            sb.Append(text[index..nextSequenceStart]);

            var sequenceLength = GetAnsiSequenceLengthAt(text, nextSequenceStart);
            if (sequenceLength == 0)
            {
                break;
            }

            index = nextSequenceStart + sequenceLength;
            removedCharacterCount += sequenceLength;

            nextSequenceStart = text.IndexOf('\x1B', index);
        }

        sb.Append(text[index..]);
        trimmedWord = sb.ToString();
        return true;
    }

    private static int GetAnsiSequenceLengthAt(string text, int index)
    {
        var start = index;

        if (index + 4 > text.Length || text[index] != '\u001B' || text[index + 1] != '[' || !char.IsDigit(text[index + 2]))
        {
            return 0;
        }

        index += 2; // The escape and the '[' character
        while (index < text.Length && char.IsDigit(text[index]))
        {
            index++;
        }
        index++; // m-postfix

        return index - start;
    }
}
