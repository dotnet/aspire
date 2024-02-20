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
        // Intentionally use fff here instead of FFF so output has a consistent length.
        var millisecondFormat = $"{NumberFormatInfo.CurrentInfo.NumberDecimalSeparator}fff";

        // Append millisecond pattern to current culture's long time pattern.
        return MatchSecondsInTimeFormatPattern().Replace(longTimePattern, $"$1{millisecondFormat}");
    }

    [GeneratedRegex("(:ss|:s)")]
    private static partial Regex MatchSecondsInTimeFormatPattern();

    private static readonly string s_longTimePatternWithMilliseconds = GetLongTimePatternWithMilliseconds();
    private static readonly string s_shortDateLongTimePatternWithMilliseconds = DateTimeFormatInfo.CurrentInfo.ShortDatePattern + " " + s_longTimePatternWithMilliseconds;

    public static string FormatTime(DateTime value, bool includeMilliseconds = false)
    {
        var local = value.ToLocalTime();

        // Long time
        return includeMilliseconds
            ? local.ToString(s_longTimePatternWithMilliseconds, CultureInfo.CurrentCulture)
            : local.ToString("T", CultureInfo.CurrentCulture);
    }

    public static string FormatDateTime(DateTime value, bool includeMilliseconds = false)
    {
        var local = value.ToLocalTime();

        // Short date, long time
        return includeMilliseconds
            ? local.ToString(s_shortDateLongTimePatternWithMilliseconds, CultureInfo.CurrentCulture)
            : local.ToString("G", CultureInfo.CurrentCulture);
    }

    public static string FormatTimeWithOptionalDate(DateTime value, bool includeMilliseconds = false)
    {
        var local = value.ToLocalTime();

        // If the date is today then only return time, otherwise return entire date time text.
        if (local.Date == DateTime.Now.Date)
        {
            // e.g. "08:57:44" (based on user's culture and preferences)
            // Don't include milliseconds as resource server returned time stamp is second precision.
            return FormatTime(local, includeMilliseconds);
        }
        else
        {
            // e.g. "9/02/2024 08:57:44" (based on user's culture and preferences)
            return FormatDateTime(local, includeMilliseconds);
        }
    }

    public static string FormatNumberWithOptionalDecimalPlaces(double value, IFormatProvider? provider = null)
    {
        return value.ToString("##,0.######", provider ?? CultureInfo.CurrentCulture);
    }
}
