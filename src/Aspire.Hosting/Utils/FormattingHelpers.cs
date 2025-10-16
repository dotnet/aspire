// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

internal sealed class FormattingHelpers
{
    public static string FormatValue(string value, string format)
    {
        return format.ToLowerInvariant() switch
        {
            "uri" => Uri.EscapeDataString(value),
            _ => throw new NotSupportedException($"The format '{format}' is not supported. Supported formats are: uri")
        };
    }
}
