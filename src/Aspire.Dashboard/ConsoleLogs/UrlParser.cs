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
        if (TryRemoveXmlTagsFromMatch(text, match.Index, out var noXmlTags, out var removedCharacterCount))
        {
            var noXmlTagsMatch = s_urlRegEx.Match(noXmlTags);
            nextCharIndex = match.Index + noXmlTagsMatch.Length + removedCharacterCount;
            return noXmlTagsMatch.Value;
        }
        else
        {
            nextCharIndex = match.Index + match.Length;
            return match.Value;
        }
    }

    private static bool TryRemoveXmlTagsFromMatch(string text, int index, [NotNullWhen(true)] out string? modifiedText, out int removedCharacterCount)
    {
        removedCharacterCount = 0;

        var nextTagStart = text.IndexOfAny(['<', ' '], index);
        if (nextTagStart < 0 || text[nextTagStart] == ' ')
        {
            modifiedText = null;
            return false;
        }

        var sb = new StringBuilder();

        while (nextTagStart > 0)
        {
            sb.Append(text[index..nextTagStart]);

            var tagEnd = text.IndexOf('>', nextTagStart);
            if (tagEnd < 0)
            {
                break;
            }

            index = tagEnd + 1;
            removedCharacterCount += index - nextTagStart;

            nextTagStart = text.IndexOf('<', index);
        }

        sb.Append(text[index..]);

        modifiedText = sb.ToString();
        return true;
    }
}
