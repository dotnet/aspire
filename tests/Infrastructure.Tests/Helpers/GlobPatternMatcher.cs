// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.Tests.Helpers;

/// <summary>
/// Converts glob patterns to regex and tests file paths against them.
/// Supports: ** (any path including /), * (any segment not including /), ? (single char)
/// </summary>
public static class GlobPatternMatcher
{
    private static readonly char[] s_specialRegexChars = ['.', '[', ']', '^', '$', '(', ')', '{', '}', '|', '+', '\\'];

    /// <summary>
    /// Converts a glob pattern to a regex pattern.
    /// </summary>
    /// <param name="pattern">The glob pattern to convert.</param>
    /// <returns>A regex pattern string with ^ and $ anchors.</returns>
    public static string ConvertGlobToRegex(string pattern)
    {
        var regex = new StringBuilder();
        var i = 0;
        var len = pattern.Length;

        while (i < len)
        {
            var c = pattern[i];
            var nextChar = i + 1 < len ? pattern[i + 1] : (char?)null;

            switch (c)
            {
                case '*':
                    if (nextChar == '*')
                    {
                        // ** matches any path (including /)
                        regex.Append(".*");
                        i++;
                    }
                    else
                    {
                        // * matches any segment (not including /)
                        regex.Append("[^/]*");
                    }
                    break;

                case '?':
                    // ? matches single char
                    regex.Append('.');
                    break;

                default:
                    if (s_specialRegexChars.Contains(c))
                    {
                        // Escape special regex chars
                        regex.Append('\\');
                    }
                    regex.Append(c);
                    break;
            }
            i++;
        }

        // Anchor the pattern
        return $"^{regex}$";
    }

    /// <summary>
    /// Tests if a file path matches a glob pattern.
    /// </summary>
    /// <param name="filePath">The file path to test.</param>
    /// <param name="pattern">The glob pattern to match against.</param>
    /// <returns>True if the path matches the pattern.</returns>
    public static bool IsMatch(string filePath, string pattern)
    {
        var regex = ConvertGlobToRegex(pattern);
        return Regex.IsMatch(filePath, regex);
    }

    /// <summary>
    /// Tests if a file path matches any of the given glob patterns.
    /// </summary>
    /// <param name="filePath">The file path to test.</param>
    /// <param name="patterns">The glob patterns to match against.</param>
    /// <returns>True if the path matches any pattern.</returns>
    public static bool MatchesAny(string filePath, IEnumerable<string> patterns)
    {
        return patterns.Any(pattern => IsMatch(filePath, pattern));
    }

    /// <summary>
    /// Converts a source pattern with {name} placeholder to a regex with a named capture group.
    /// </summary>
    /// <param name="pattern">The source pattern containing {name} placeholder.</param>
    /// <returns>A regex pattern string with a named capture group for "name".</returns>
    public static string ConvertSourcePatternToRegex(string pattern)
    {
        var regex = new StringBuilder();
        var i = 0;
        var len = pattern.Length;

        while (i < len)
        {
            // Check for {name} placeholder
            if (i + 5 < len && pattern.Substring(i, 6) == "{name}")
            {
                // Capture group for the name - match path segments (no slashes)
                regex.Append("(?<name>[^/]+)");
                i += 6;
                continue;
            }

            var c = pattern[i];
            var nextChar = i + 1 < len ? pattern[i + 1] : (char?)null;

            switch (c)
            {
                case '*':
                    if (nextChar == '*')
                    {
                        regex.Append(".*");
                        i++;
                    }
                    else
                    {
                        regex.Append("[^/]*");
                    }
                    break;

                case '?':
                    regex.Append('.');
                    break;

                default:
                    if (s_specialRegexChars.Contains(c))
                    {
                        regex.Append('\\');
                    }
                    regex.Append(c);
                    break;
            }
            i++;
        }

        return $"^{regex}$";
    }

    /// <summary>
    /// Tries to match a file path against a source pattern with {name} placeholder,
    /// extracting the captured name.
    /// </summary>
    /// <param name="filePath">The file path to test.</param>
    /// <param name="sourcePattern">The source pattern containing {name} placeholder.</param>
    /// <param name="capturedName">The captured name value if matched.</param>
    /// <returns>True if the path matches and a name was captured.</returns>
    public static bool TryMatchSourcePattern(string filePath, string sourcePattern, out string? capturedName)
    {
        capturedName = null;
        var regexPattern = ConvertSourcePatternToRegex(sourcePattern);
        var match = Regex.Match(filePath, regexPattern);

        if (match.Success && match.Groups["name"].Success)
        {
            capturedName = match.Groups["name"].Value;
            return true;
        }

        return false;
    }
}
