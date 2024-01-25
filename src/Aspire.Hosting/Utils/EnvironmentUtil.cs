// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Utils;

internal static class EnvironmentUtil
{
    /// <summary>
    /// Parses a environment variable's semicolon-delimited value into an array of <see cref="Uri"/> objects.
    /// </summary>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="defaultValue">A default value, for when the environment variable is unspecified or white space. May be <see langword="null"/>.</param>
    /// <returns>The parsed values, or the default value if specified and parsing failed. Returns <see langword="null"/> if <paramref name="defaultValue"/> is <see langword="null"/> and parsing failed.</returns>
    /// <exception cref="InvalidOperationException">The environment variable could not be accessed, or contained an unparseable value.</exception>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static Uri[]? GetAddressUris(string variableName, Uri? defaultValue)
    {
        try
        {
            var urls = Environment.GetEnvironmentVariable(variableName);

            if (string.IsNullOrWhiteSpace(urls))
            {
                return defaultValue switch
                {
                    not null => [defaultValue],
                    null => null
                };
            }
            else
            {
                return urls
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(url => new Uri(url, UriKind.Absolute))
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
        }
    }
}
