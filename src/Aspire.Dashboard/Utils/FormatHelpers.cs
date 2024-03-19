// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Utils;

internal static partial class FormatHelpers
{
    // There are an unbound number of CultureInfo instances so we don't want to use it as the key.
    // Someone could have also customized their culture so we don't want to use the name as the key.
    // This struct contains required information from the culture that is used in cached format strings.
    private readonly record struct CultureDetailsKey(string LongTimePattern, string ShortDatePattern, string NumberDecimalSeparator);
    private sealed record MillisecondFormatStrings(string LongTimePattern, string ShortDateLongTimePattern);
    private static readonly ConcurrentDictionary<CultureDetailsKey, MillisecondFormatStrings> s_formatStrings = new();

    [GeneratedRegex("(:ss|:s)")]
    private static partial Regex MatchSecondsInTimeFormatPattern();

    private static MillisecondFormatStrings GetMillisecondFormatStrings(CultureInfo cultureInfo)
    {
        var key = new CultureDetailsKey(cultureInfo.DateTimeFormat.LongTimePattern, cultureInfo.DateTimeFormat.ShortDatePattern, cultureInfo.NumberFormat.NumberDecimalSeparator);

        return s_formatStrings.GetOrAdd(key, static k =>
        {
            var longTimePatternWithMilliseconds = GetLongTimePatternWithMillisecondsCore(k);
            return new MillisecondFormatStrings(longTimePatternWithMilliseconds, k.ShortDatePattern + " " + longTimePatternWithMilliseconds);
        });

        static string GetLongTimePatternWithMillisecondsCore(CultureDetailsKey key)
        {
            // From https://learn.microsoft.com/dotnet/standard/base-types/how-to-display-milliseconds-in-date-and-time-values

            // Gets the long time pattern, which is something like "h:mm:ss tt" (en-US), "H:mm:ss" (ja-JP), "HH:mm:ss" (fr-FR).
            var longTimePattern = key.LongTimePattern;

            // Create a format similar to .fff but based on the current culture.
            // Intentionally use fff here instead of FFF so output has a consistent length.
            var millisecondFormat = $"'{key.NumberDecimalSeparator}'fff";

            // Append millisecond pattern to current culture's long time pattern.
            return MatchSecondsInTimeFormatPattern().Replace(longTimePattern, $"$1{millisecondFormat}");
        }
    }

    private static string GetLongTimePatternWithMilliseconds(CultureInfo cultureInfo) => GetMillisecondFormatStrings(cultureInfo).LongTimePattern;

    private static string GetShortDateLongTimePatternWithMilliseconds(CultureInfo cultureInfo) => GetMillisecondFormatStrings(cultureInfo).ShortDateLongTimePattern;

    public static string FormatTime(BrowserTimeProvider timeProvider, DateTime value, bool includeMilliseconds = false, CultureInfo? cultureInfo = null)
    {
        cultureInfo ??= CultureInfo.CurrentCulture;
        var local = timeProvider.ToLocal(value);

        // Long time
        return includeMilliseconds
            ? local.ToString(GetLongTimePatternWithMilliseconds(cultureInfo), cultureInfo)
            : local.ToString("T", cultureInfo);
    }

    public static string FormatDateTime(BrowserTimeProvider timeProvider, DateTime value, bool includeMilliseconds = false, CultureInfo? cultureInfo = null)
    {
        cultureInfo ??= CultureInfo.CurrentCulture;
        var local = timeProvider.ToLocal(value);

        // Short date, long time
        return includeMilliseconds
            ? local.ToString(GetShortDateLongTimePatternWithMilliseconds(cultureInfo), cultureInfo)
            : local.ToString("G", cultureInfo);
    }

    public static string FormatTimeWithOptionalDate(BrowserTimeProvider timeProvider, DateTime value, bool includeMilliseconds = false, CultureInfo? cultureInfo = null)
    {
        var local = timeProvider.ToLocal(value);

        // If the date is today then only return time, otherwise return entire date time text.
        if (local.Date == DateTime.Now.Date)
        {
            // e.g. "08:57:44" (based on user's culture and preferences)
            // Don't include milliseconds as resource server returned time stamp is second precision.
            return FormatTime(timeProvider, local, includeMilliseconds, cultureInfo);
        }
        else
        {
            // e.g. "9/02/2024 08:57:44" (based on user's culture and preferences)
            return FormatDateTime(timeProvider, local, includeMilliseconds, cultureInfo);
        }
    }

    public static string FormatNumberWithOptionalDecimalPlaces(double value, CultureInfo? provider = null)
    {
        return value.ToString("##,0.######", provider ?? CultureInfo.CurrentCulture);
    }
}
