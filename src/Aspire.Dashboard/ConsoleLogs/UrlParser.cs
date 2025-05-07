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
    private static readonly Regex s_ansiEscape = GenerateAnsiEscapeRegEx();
    private static readonly Regex s_ansiEscapeStringLiteral = GenerateAnsiEscapeStringLiteralRegEx();
    private static readonly Regex s_spanTag = GenerateSpanTagRegEx();

    public static bool TryParse(string? text, Func<string, string>? nonMatchFragmentCallback, [NotNullWhen(true)] out string? modifiedText)
    {
        if (text is null)
        {
            modifiedText = null;
            return false;
        }

        // First, normalize string literal ANSI sequences to actual escape sequences
        string normalizedText = text;
        normalizedText = s_ansiEscapeStringLiteral.Replace(normalizedText, match => 
            $"\x1B{match.Value.Substring(4)}"); // Convert \\x1B to \x1B

        // Now build a clean string without any ANSI escapes
        var cleanBuilder = new StringBuilder(normalizedText.Length);
        var indexMap = new List<int>();
        var reverseMap = new Dictionary<int, int>();

        for (int i = 0, cleanIndex = 0; i < normalizedText.Length;)
        {
            // Skip ANSI escape sequences
            var ansiMatch = s_ansiEscape.Match(normalizedText, i);
            if (ansiMatch.Success && ansiMatch.Index == i)
            {
                i += ansiMatch.Length;
                continue;
            }

            // Skip span tags
            var spanMatch = s_spanTag.Match(normalizedText, i);
            if (spanMatch.Success && spanMatch.Index == i)
            {
                i += spanMatch.Length;
                continue;
            }

            // Keep this character
            cleanBuilder.Append(normalizedText[i]);
            indexMap.Add(i);
            reverseMap[cleanIndex] = i;
            i++;
            cleanIndex++;
        }

        var cleaned = cleanBuilder.ToString();

        // Ensure the cleaned string is not null or empty
        if (string.IsNullOrEmpty(cleaned))
        {
            modifiedText = null;
            return false;
        }

        // Run URL regex against the cleaned text
        var match = s_urlRegEx.Match(cleaned);
        if (!match.Success)
        {
            modifiedText = null;
            return false;
        }

        StringBuilder output = new StringBuilder();
        int lastPos = 0;

        while (match.Success)
        {
            // Append any non-URL text before this match
            if (match.Index > lastPos)
            {
                var fragment = cleaned.Substring(lastPos, match.Index - lastPos);
                if (nonMatchFragmentCallback != null)
                {
                    fragment = nonMatchFragmentCallback(fragment);
                }
                output.Append(fragment);
            }

            // Extract the clean URL (without any ANSI escapes)
            var cleanUrl = cleaned.Substring(match.Index, match.Length);

            // Get the original text with ANSI escapes by finding the corresponding positions in the original text
            int startOriginal = reverseMap[match.Index];
            int endOriginal = reverseMap[match.Index + match.Length - 1] + 1;
            var originalText = normalizedText.Substring(startOriginal, endOriginal - startOriginal);

            // Generate the HTML link with clean URL in href but original text (with ANSI) in the content
            output.Append(CultureInfo.InvariantCulture,
                $"<a target=\"_blank\" href=\"{cleanUrl}\" rel=\"noopener noreferrer nofollow\">"
                + $"{text}</a>");

            lastPos = match.Index + match.Length;
            match = match.NextMatch();
        }

        // Add any remaining text after the last URL
        if (lastPos < cleaned.Length)
        {
            var tail = cleaned.Substring(lastPos);
            if (nonMatchFragmentCallback != null)
            {
                tail = nonMatchFragmentCallback(tail);
            }
            output.Append(tail);
        }

        modifiedText = output.ToString();
        return true;
    }

    // Regular expression that detects http/https URLs in a log entry
    // Based on the RegEx used by GitHub to detect links in content.
    [GeneratedRegex(
        @"((?<!\+)https?:\/\/(?:www\.)?(?:(?:[-\p{L}\d.]+?[.@][a-zA-Z\d]{2,})|localhost|(?:\d{1,3}(?:\.\d{1,3}){3}))(?:[-\w\p{L}.:%+~#*$!?&/=@]*(?:,(?!\s))*?)*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    public static partial Regex GenerateUrlRegEx();

    // Matches ANSI‐CSI escape sequences (e.g. "\x1B[32m")
    [GeneratedRegex(
        @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    public static partial Regex GenerateAnsiEscapeRegEx();

    // Matches string literal representations of ANSI escape sequences (e.g. "\\x1B[32m")
    [GeneratedRegex(
        @"\\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    public static partial Regex GenerateAnsiEscapeStringLiteralRegEx();

    // Matches any <span>…</span> tag so we can strip coloring markup
    [GeneratedRegex(
        @"</?span[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    public static partial Regex GenerateSpanTagRegEx();

    
}
