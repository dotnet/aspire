// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS;

internal static class StringExtensions
{
    /// <summary>
    /// Trims a string from the start of the target
    /// </summary>
    /// <param name="target">Target string</param>
    /// <param name="trimString">String to trim</param>
    /// <returns>Trimmed string</returns>
    public static string TrimStart(this string target, string trimString)
    {
        if (string.IsNullOrEmpty(trimString))
        {
            return target;
        }

        var result = target;
        while (result.StartsWith(trimString))
        {
            result = result[trimString.Length..];
        }

        return result;

    }

    /// <summary>
    /// Replaces : characters to __ for environment variables support with Microsoft.Extensions.Configuration
    /// </summary>
    /// <param name="configuration">Configuration key</param>
    public static string ToEnvironmentVariables(this string configuration)
    {
        return configuration
            .Replace("::", "__")
            .Replace(":", "__");
    }
}
