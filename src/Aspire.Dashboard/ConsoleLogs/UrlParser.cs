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

    public static bool TryParse(string? html, [NotNullWhen(true)] out string? modifiedHtml)
    {
        modifiedHtml = null;

        if (html is null)
        {
            return false;
        }

        var (text, toHtmlIndex) = BuildContext(html);

        var urlMatch = s_urlRegEx.Match(text);

        var builder = new StringBuilder(text.Length * 2);

        var nextCharIndex = 0;
        while (urlMatch.Success)
        {
            if (urlMatch.Index > 0)
            {
                builder.Append(text[(nextCharIndex)..urlMatch.Index]);
            }

            var urlStart = toHtmlIndex[urlMatch.Index];
            nextCharIndex = toHtmlIndex[urlMatch.Index + urlMatch.Length - 1] + 1;
            var url = html[urlStart..nextCharIndex];

            builder.Append(CultureInfo.InvariantCulture, $"<a target=\"_blank\" href=\"{urlMatch.Value}\">{url}</a>");
            urlMatch = urlMatch.NextMatch();
        }

        if (builder.Length == 0)
        {
            return false;
        }

        if (nextCharIndex < html.Length)
        {
            builder.Append(html[(nextCharIndex)..]);
        }

        modifiedHtml = builder.ToString();
        return true;
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

    private static (string, int[]) BuildContext(string html)
    {
        var textBuilder = new StringBuilder();
        var indexLookup = new int[html.Length];

        var textIndex = 0;
        var htmlIndex = 0;
        var isInsideTag = false;
        foreach (var character in html)
        {
            if (character == '<')
            {
                isInsideTag = true;
            }

            if (isInsideTag)
            {
                htmlIndex++;
            }
            else
            {
                indexLookup[textIndex++] = htmlIndex++;
                textBuilder.Append(character);
            }

            if (character == '>')
            {
                isInsideTag = false;
            }
        }

        var text = textBuilder.ToString();
        return (text, indexLookup);
    }
}
