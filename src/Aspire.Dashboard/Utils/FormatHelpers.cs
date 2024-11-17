// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Utils;

public enum MillisecondsDisplay
{
    None,
    Truncated,
    Full
}

internal static partial class FormatHelpers
{
    // There are an unbound number of CultureInfo instances so we don't want to use it as the key.
    // Someone could have also customized their culture so we don't want to use the name as the key.
    // This struct contains required information from the culture that is used in cached format strings.
    private readonly record struct CultureDetailsKey(string LongTimePattern, string ShortDatePattern, string NumberDecimalSeparator);
    private sealed record MillisecondFormatStrings(MillisecondFormatString LongTimePattern, MillisecondFormatString ShortDateLongTimePattern);
    private sealed record MillisecondFormatString(string TruncatedMilliseconds, string FullMilliseconds);
    private static readonly ConcurrentDictionary<CultureDetailsKey, MillisecondFormatStrings> s_formatStrings = new();

    // Colon and dot are the only time separators used by registered cultures. Regex checks for both.
    [GeneratedRegex(@"(:ss|\.ss|:s|\.s)")]
    private static partial Regex MatchSecondsInTimeFormatPattern();

    private static MillisecondFormatStrings GetMillisecondFormatStrings(CultureInfo cultureInfo)
    {
        var key = new CultureDetailsKey(cultureInfo.DateTimeFormat.LongTimePattern, cultureInfo.DateTimeFormat.ShortDatePattern, cultureInfo.NumberFormat.NumberDecimalSeparator);

        return s_formatStrings.GetOrAdd(key, static k =>
        {
            var (truncated, full) = GetLongTimePatternWithMillisecondsCore(k);
            return new MillisecondFormatStrings(
                new MillisecondFormatString(truncated, full),
                new MillisecondFormatString(
                    k.ShortDatePattern + " " + truncated,
                    k.ShortDatePattern + " " + full));
        });

        static (string Truncated, string Full) GetLongTimePatternWithMillisecondsCore(CultureDetailsKey key)
        {
            // From https://learn.microsoft.com/dotnet/standard/base-types/how-to-display-milliseconds-in-date-and-time-values

            // Create a format similar to .fff but based on the current culture.
            // Intentionally use fff here instead of FFF so output has a consistent length.
            var truncatedMillisecondFormat = "fff";

            // Append millisecond pattern to current culture's long time pattern.
            return (
                FormatPattern(key, truncatedMillisecondFormat),
                FormatPattern(key, "FFFFFFF"));
        }

        static string FormatPattern(CultureDetailsKey key, string millisecondFormat)
        {
            // Gets the long time pattern, which is something like "h:mm:ss tt" (en-US), "H:mm:ss" (ja-JP), "HH:mm:ss" (fr-FR).
            var longTimePattern = key.LongTimePattern;

            return MatchSecondsInTimeFormatPattern().Replace(longTimePattern, $"$1'{key.NumberDecimalSeparator}'{millisecondFormat}");
        }
    }

    private static MillisecondFormatString GetLongTimePatternWithMilliseconds(CultureInfo cultureInfo) => GetMillisecondFormatStrings(cultureInfo).LongTimePattern;

    private static MillisecondFormatString GetShortDateLongTimePatternWithMilliseconds(CultureInfo cultureInfo) => GetMillisecondFormatStrings(cultureInfo).ShortDateLongTimePattern;

    public static string FormatTime(BrowserTimeProvider timeProvider, DateTime value, MillisecondsDisplay millisecondsDisplay = MillisecondsDisplay.None, CultureInfo? cultureInfo = null)
    {
        cultureInfo ??= CultureInfo.CurrentCulture;
        var local = timeProvider.ToLocal(value);

        // Long time
        return millisecondsDisplay switch
        {
            MillisecondsDisplay.None => local.ToString("T", cultureInfo),
            MillisecondsDisplay.Truncated => local.ToString(GetLongTimePatternWithMilliseconds(cultureInfo).TruncatedMilliseconds, cultureInfo),
            MillisecondsDisplay.Full => local.ToString(GetLongTimePatternWithMilliseconds(cultureInfo).FullMilliseconds, cultureInfo),
            _ => throw new NotImplementedException()
        };
    }

    public static string FormatDateTime(BrowserTimeProvider timeProvider, DateTime value, MillisecondsDisplay millisecondsDisplay = MillisecondsDisplay.None, CultureInfo? cultureInfo = null)
    {
        cultureInfo ??= CultureInfo.CurrentCulture;
        var local = timeProvider.ToLocal(value);

        // Short date, long time
        return millisecondsDisplay switch
        {
            MillisecondsDisplay.None => local.ToString("G", cultureInfo),
            MillisecondsDisplay.Truncated => local.ToString(GetShortDateLongTimePatternWithMilliseconds(cultureInfo).TruncatedMilliseconds, cultureInfo),
            MillisecondsDisplay.Full => local.ToString(GetShortDateLongTimePatternWithMilliseconds(cultureInfo).FullMilliseconds, cultureInfo),
            _ => throw new NotImplementedException()
        };
    }

    public static string FormatTimeWithOptionalDate(BrowserTimeProvider timeProvider, DateTime value, MillisecondsDisplay millisecondsDisplay = MillisecondsDisplay.None, CultureInfo? cultureInfo = null)
    {
        var local = timeProvider.ToLocal(value);

        // If the date is today then only return time, otherwise return entire date time text.
        if (local.Date == DateTime.Now.Date)
        {
            // e.g. "08:57:44" (based on user's culture and preferences)
            // Don't include milliseconds as resource server returned time stamp is second precision.
            return FormatTime(timeProvider, local, millisecondsDisplay, cultureInfo);
        }
        else
        {
            // e.g. "9/02/2024 08:57:44" (based on user's culture and preferences)
            return FormatDateTime(timeProvider, local, millisecondsDisplay, cultureInfo);
        }
    }

    public static string FormatNumberWithOptionalDecimalPlaces(double value, int maxDecimalPlaces, CultureInfo? provider = null)
    {
        var formatString = maxDecimalPlaces switch
        {
            1 => "##,0.#",
            2 => "##,0.##",
            3 => "##,0.###",
            4 => "##,0.####",
            5 => "##,0.#####",
            6 => "##,0.######",
            _ => throw new ArgumentException("Unexpected value.", nameof(maxDecimalPlaces))
        };
        return value.ToString(formatString, provider ?? CultureInfo.CurrentCulture);
    }
}
