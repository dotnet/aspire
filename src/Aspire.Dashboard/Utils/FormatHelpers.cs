// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Aspire.Dashboard.Utils;

internal static partial class FormatHelpers
{
    private sealed record MillisecondFormatStrings(string LongTimePattern, string ShortDateLongTimePattern);

    // Use culture name as key. Culture name isn't case sensitive.
    private static readonly ConcurrentDictionary<string, MillisecondFormatStrings> s_formatStrings = new(StringComparer.OrdinalIgnoreCase);

    static string GetLongTimePatternWithMillisecondsCore(CultureInfo cultureInfo)
    {
        // From https://learn.microsoft.com/dotnet/standard/base-types/how-to-display-milliseconds-in-date-and-time-values

        // Gets the long time pattern, which is something like "h:mm:ss tt" (en-US), "H:mm:ss" (ja-JP), "HH:mm:ss" (fr-FR).
        var longTimePattern = cultureInfo.DateTimeFormat.LongTimePattern;

        // Create a format similar to .fff but based on the current culture.
        // Intentionally use fff here instead of FFF so output has a consistent length.
        var millisecondFormat = $"'{cultureInfo.NumberFormat.NumberDecimalSeparator}'fff";

        // Append millisecond pattern to current culture's long time pattern.
        return MatchSecondsInTimeFormatPattern().Replace(longTimePattern, $"$1{millisecondFormat}");
    }

    [GeneratedRegex("(:ss|:s)")]
    private static partial Regex MatchSecondsInTimeFormatPattern();

    private static MillisecondFormatStrings GetMillisecondFormatStrings(CultureInfo cultureInfo)
    {
        return s_formatStrings.GetOrAdd(cultureInfo.Name, static name =>
        {
            var c = CultureInfo.GetCultureInfo(name);
            var longTimePatternWithMilliseconds = GetLongTimePatternWithMillisecondsCore(c);
            return new MillisecondFormatStrings(longTimePatternWithMilliseconds, c.DateTimeFormat.ShortDatePattern + " " + longTimePatternWithMilliseconds);
        });
    }

    private static string GetLongTimePatternWithMilliseconds(CultureInfo cultureInfo) => GetMillisecondFormatStrings(cultureInfo).LongTimePattern;

    private static string GetShortDateLongTimePatternWithMilliseconds(CultureInfo cultureInfo) => GetMillisecondFormatStrings(cultureInfo).ShortDateLongTimePattern;

    public static string FormatTime(DateTime value, bool includeMilliseconds = false, CultureInfo? provider = null)
    {
        provider ??= CultureInfo.CurrentCulture;
        var local = value.ToLocalTime();

        // Long time
        return includeMilliseconds
            ? local.ToString(GetLongTimePatternWithMilliseconds(provider), provider)
            : local.ToString("T", provider);
    }

    public static string FormatDateTime(DateTime value, bool includeMilliseconds = false, CultureInfo? provider = null)
    {
        provider ??= CultureInfo.CurrentCulture;
        var local = value.ToLocalTime();

        // Short date, long time
        return includeMilliseconds
            ? local.ToString(GetShortDateLongTimePatternWithMilliseconds(provider), provider)
            : local.ToString("G", provider);
    }

    public static string FormatTimeWithOptionalDate(DateTime value, bool includeMilliseconds = false, CultureInfo? provider = null)
    {
        var local = value.ToLocalTime();

        // If the date is today then only return time, otherwise return entire date time text.
        if (local.Date == DateTime.Now.Date)
        {
            // e.g. "08:57:44" (based on user's culture and preferences)
            // Don't include milliseconds as resource server returned time stamp is second precision.
            return FormatTime(local, includeMilliseconds, provider);
        }
        else
        {
            // e.g. "9/02/2024 08:57:44" (based on user's culture and preferences)
            return FormatDateTime(local, includeMilliseconds, provider);
        }
    }

    public static string FormatNumberWithOptionalDecimalPlaces(double value, CultureInfo? provider = null)
    {
        return value.ToString("##,0.######", provider ?? CultureInfo.CurrentCulture);
    }
}
