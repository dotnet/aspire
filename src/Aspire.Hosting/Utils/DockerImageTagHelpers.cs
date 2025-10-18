// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Provides helper methods for sanitizing Docker image tags.
/// </summary>
public static class DockerImageTagHelpers
{
    private const int MaxDockerTagLength = 128;
    private const string DefaultTag = "aspire-deploy";

    /// <summary>
    /// Sanitizes a string to be a valid Docker image tag.
    /// Docker image tags must match the pattern [a-zA-Z0-9_.-]+ and cannot start with a period or hyphen.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>A sanitized Docker image tag, or "aspire-deploy" if the input is null, empty, or results in an empty string after sanitization.</returns>
    public static string SanitizeTag(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DefaultTag;
        }

        // Convert to lowercase and replace invalid characters with hyphens
        var sanitized = new StringBuilder(capacity: Math.Min(input.Length, MaxDockerTagLength));

        foreach (var c in input.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
            {
                sanitized.Append(c);
            }
            else
            {
                // Replace invalid characters with hyphens
                sanitized.Append('-');
            }
        }

        // Ensure it doesn't start with a period or hyphen
        var result = sanitized.ToString().TrimStart('.', '-');

        // Truncate to max length if necessary
        if (result.Length > MaxDockerTagLength)
        {
            result = result.Substring(0, MaxDockerTagLength);
        }

        // Ensure the result is not empty after sanitization
        return string.IsNullOrEmpty(result) ? DefaultTag : result;
    }
}
