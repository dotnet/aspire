// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// Utility for parsing .env files.
/// </summary>
internal static class EnvFileParser
{
    /// <summary>
    /// Parses a .env file and returns the entries.
    /// </summary>
    /// <param name="filePath">The path to the .env file.</param>
    /// <returns>A collection of env entries. If the file doesn't exist, returns an empty collection.</returns>
    public static IEnumerable<EnvEntry> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        return ParseLines(File.ReadAllLines(filePath));
    }

    /// <summary>
    /// Parses lines from a .env file content.
    /// </summary>
    /// <param name="lines">The lines to parse.</param>
    /// <returns>A collection of env entries.</returns>
    public static IEnumerable<EnvEntry> ParseLines(IEnumerable<string> lines)
    {
        var entries = new Dictionary<string, EnvEntry>();
        string? pendingComment = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(trimmedLine))
            {
                pendingComment = null;
                continue;
            }

            // Handle comments
            if (trimmedLine.StartsWith('#'))
            {
                // Extract comment content (remove # and leading whitespace)
                var commentContent = trimmedLine.Substring(1).TrimStart();
                pendingComment = string.IsNullOrEmpty(commentContent) ? null : commentContent;
                continue;
            }

            // Parse key=value pairs
            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex == -1)
            {
                // Malformed line - skip but reset pending comment
                pendingComment = null;
                continue;
            }

            var key = trimmedLine.Substring(0, equalIndex).Trim();
            if (string.IsNullOrEmpty(key))
            {
                // Invalid key - skip
                pendingComment = null;
                continue;
            }

            var valuepart = trimmedLine.Substring(equalIndex + 1);
            var value = ParseValue(valuepart);

            // Create entry (last one wins for duplicate keys)
            entries[key] = new EnvEntry(key, value, pendingComment);
            pendingComment = null;
        }

        return entries.Values;
    }

    /// <summary>
    /// Parses a value from the .env file, handling quotes and escaping.
    /// </summary>
    /// <param name="valuepart">The value part after the equals sign.</param>
    /// <returns>The parsed value.</returns>
    private static string? ParseValue(string valuepart)
    {
        if (string.IsNullOrEmpty(valuepart))
        {
            return string.Empty;
        }

        var trimmed = valuepart.Trim();
        
        // Handle quoted values
        if (trimmed.Length >= 2)
        {
            if ((trimmed.StartsWith('"') && trimmed.EndsWith('"')) ||
                (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
            {
                // Remove outer quotes
                var unquoted = trimmed.Substring(1, trimmed.Length - 2);
                // Basic escape sequence handling for double quotes
                if (trimmed.StartsWith('"'))
                {
                    return unquoted.Replace("\\n", "\n")
                                  .Replace("\\r", "\r")
                                  .Replace("\\t", "\t")
                                  .Replace("\\\"", "\"")
                                  .Replace("\\\\", "\\");
                }
                return unquoted;
            }
        }

        // Handle inline comments for unquoted values
        var commentIndex = trimmed.IndexOf('#');
        if (commentIndex >= 0)
        {
            trimmed = trimmed.Substring(0, commentIndex).TrimEnd();
        }

        return trimmed;
    }
}