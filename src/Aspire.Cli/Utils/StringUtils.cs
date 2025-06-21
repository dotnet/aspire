// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Cli.Utils;

internal static partial class StringUtils
{
    public static string RemoveFormatting(this string input)
    {
        return RemoveFormattingRegex().Replace(input, string.Empty).Trim();
    }

    [GeneratedRegex(@"\[[^\]]+\]")]
    private static partial Regex RemoveFormattingRegex();
}
