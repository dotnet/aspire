// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// Provides helper methods for formatting values in specific formats.
/// </summary>
internal static class FormattingHelpers
{
    /// <summary>
    /// Formats the specified value according to the provided format string.
    /// Currently supports encoding a value as a URI component.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format to apply. Supported value: "uri".</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified format is not supported.</exception>
    public static string FormatValue(string value, string format)
    {
        return format.ToLowerInvariant() switch
        {
            "uri" => Uri.EscapeDataString(value),
            _ => throw new NotSupportedException($"The format '{format}' is not supported. Supported formats are 'uri' (encodes a URI)")
        };
    }
}
