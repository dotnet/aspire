// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Aspire.Dashboard.Utils;

internal static partial class FormatHelpers
{
    static string GetLongTimePatternWithMilliseconds()
    {
        // From https://learn.microsoft.com/dotnet/standard/base-types/how-to-display-milliseconds-in-date-and-time-values

        // Gets the long time pattern, which is something like "h:mm:ss tt" (en-US), "H:mm:ss" (ja-JP), "HH:mm:ss" (fr-FR).
        var longTimePattern = DateTimeFormatInfo.CurrentInfo.LongTimePattern;

        // Create a format similar to .fff but based on the current culture.
        var millisecondFormat = $"{NumberFormatInfo.CurrentInfo.NumberDecimalSeparator}fff";

        // Append millisecond pattern to current culture's long time pattern.
        return MatchSecondsInTimeFormatPattern().Replace(longTimePattern, $"$1{millisecondFormat}");
    }

    [GeneratedRegex("(:ss|:s)")]
    private static partial Regex MatchSecondsInTimeFormatPattern();

    private static readonly string s_longTimePatternWithMilliseconds = GetLongTimePatternWithMilliseconds();

    public static string FormatTimeStamp(DateTime time)
    {
        // Long time with milliseconds
        return time.ToLocalTime().ToString(s_longTimePatternWithMilliseconds, CultureInfo.CurrentCulture);
    }

    public static string FormatTime(DateTime time)
    {
        // Long time
        return time.ToLocalTime().ToString("T", CultureInfo.CurrentCulture);
    }

    public static string FormatDateTime(DateTime dateTime)
    {
        // Short date, long time
        return dateTime.ToLocalTime().ToString("G", CultureInfo.CurrentCulture);
    }
}
