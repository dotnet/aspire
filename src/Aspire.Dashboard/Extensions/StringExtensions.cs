// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Shortens a string by replacing the middle with an ellipsis.
    /// </summary>
    /// <param name="text">The string to shorten</param>
    /// <param name="maxLength">The max length of the result</param>
    public static string TrimMiddle(this string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        var firstPart = (maxLength - 1) / 2;
        var lastPart = firstPart + ((maxLength - 1) % 2);

        return $"{text[..firstPart]}…{text[^lastPart..]}";
    }
}
