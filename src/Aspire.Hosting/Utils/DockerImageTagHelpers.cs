// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Provides helper methods for sanitizing Docker image tags.
/// </summary>
/// <remarks>
/// Docker image tags must conform to specific format requirements:
/// <list type="bullet">
/// <item>Only contain lowercase and uppercase ASCII letters, digits, underscores, periods, and hyphens</item>
/// <item>Cannot start with a period or hyphen</item>
/// <item>Maximum 128 characters</item>
/// </list>
/// </remarks>
public static class DockerImageTagHelpers
{
    private const int MaxDockerTagLength = 128;
    private const string DefaultTag = "aspire-deploy";

    /// <summary>
    /// Sanitizes a string to be a valid Docker image tag.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>
    /// A sanitized Docker image tag that conforms to Docker naming requirements.
    /// Returns "aspire-deploy" if the input is <c>null</c>, empty, or results in an empty string after sanitization.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method performs the following transformations:
    /// </para>
    /// <list type="bullet">
    /// <item>Converts all characters to lowercase</item>
    /// <item>Replaces invalid characters (anything other than a-z, 0-9, underscore, period, or hyphen) with hyphens</item>
    /// <item>Removes leading periods and hyphens</item>
    /// <item>Truncates to 128 characters if necessary</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Sanitizing various input strings:
    /// <code>
    /// var tag1 = DockerImageTagHelpers.SanitizeTag("MyEnvironment");
    /// // Returns: "myenvironment"
    ///
    /// var tag2 = DockerImageTagHelpers.SanitizeTag("My-Env@2024");
    /// // Returns: "my-env-2024"
    ///
    /// var tag3 = DockerImageTagHelpers.SanitizeTag(".invalid");
    /// // Returns: "invalid"
    ///
    /// var tag4 = DockerImageTagHelpers.SanitizeTag("@#$%");
    /// // Returns: "aspire-deploy"
    /// </code>
    /// </example>
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
