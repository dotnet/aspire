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

    public static bool TryParse(string? text, Func<string, string>? nonMatchFragmentCallback, [NotNullWhen(true)] out string? modifiedText)
    {
        if (text is not null)
        {
            var urlMatch = s_urlRegEx.Match(text);

            StringBuilder? builder = null;

            var nextCharIndex = 0;
            while (urlMatch.Success)
            {
                builder ??= new StringBuilder(text.Length * 2);

                if (urlMatch.Index > 0)
                {
                    AppendNonMatchFragment(builder, nonMatchFragmentCallback, text[(nextCharIndex)..urlMatch.Index]);
                }

                var urlStart = urlMatch.Index;
                nextCharIndex = urlMatch.Index + urlMatch.Length;
                var url = text[urlStart..nextCharIndex];

                builder.Append(CultureInfo.InvariantCulture, $"<a target=\"_blank\" href=\"{url}\">{url}</a>");
                urlMatch = urlMatch.NextMatch();
            }

            if (builder?.Length > 0)
            {
                if (nextCharIndex < text.Length)
                {
                    AppendNonMatchFragment(builder, nonMatchFragmentCallback, text[(nextCharIndex)..]);
                }

                modifiedText = builder.ToString();
                return true;
            }
        }

        modifiedText = null;
        return false;

        static void AppendNonMatchFragment(StringBuilder stringBuilder, Func<string, string>? nonMatchFragmentCallback, string text)
        {
            if (nonMatchFragmentCallback != null)
            {
                text = nonMatchFragmentCallback(text);
            }

            stringBuilder.Append(text);
        }
    }

    // Regular expression that detects http/https URLs in a log entry
    // Based on the RegEx used in Windows Terminal for the same purpose, but limited
    // to only http/https URLs
    //
    // Explanation:
    // (?<=m|\\b)                     - Match must start at a word boundary (after whitespace or at the start of the text) or "m".
    //                                  "m" is a trailing character of ANSI escape sequences.
    //                                  Required because URLs are matched before processing ANSI escape sequences.
    // https?://                      - http:// or https://
    // [-A-Za-z0-9+&@#/%?=~_|$!:,.;]* - Any character in the list, matched zero or more times.
    // [A-Za-z0-9+&@#/%=~_|$]         - Any character in the list, matched exactly once
    [GeneratedRegex("(?<=m|\\b)https?://[-A-Za-z0-9+&@#/%?=~_|$!:,.;]*[A-Za-z0-9+&@#/%=~_|$]")]
    public static partial Regex GenerateUrlRegEx();
}
