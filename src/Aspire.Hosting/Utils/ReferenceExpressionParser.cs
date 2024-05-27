// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.Utils;
internal static partial class ReferenceExpressionParser
{
    [GeneratedRegex(@"{(?!\d)")]
    private static partial Regex LeftNonParameterBraceRegex();
    [GeneratedRegex(@"(?<!\d)}")]
    private static partial Regex RightNonParameterBraceRegex();

    internal static string ParseFormat(string format)
    {
        // Escape curly braces which aren't used for a parameter.
        var parsedFormat = LeftNonParameterBraceRegex().Replace(format, "{{");
        parsedFormat = RightNonParameterBraceRegex().Replace(parsedFormat, "}}");

        return parsedFormat;
    }
}
