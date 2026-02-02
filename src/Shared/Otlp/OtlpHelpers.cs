// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Dashboard.Otlp.Model;

/// <summary>
/// Shared helper methods for working with OTLP data.
/// Used by both Dashboard and CLI.
/// </summary>
public static partial class OtlpHelpers
{
    /// <summary>
    /// The standard length for shortened trace/span IDs.
    /// </summary>
    public const int ShortenedIdLength = 7;

    /// <summary>
    /// Shortens a trace or span ID to the standard display length.
    /// </summary>
    public static string ToShortenedId(string id) => TruncateString(id, maxLength: ShortenedIdLength);

    /// <summary>
    /// Truncates a string to the specified maximum length.
    /// </summary>
    public static string TruncateString(string value, int maxLength)
    {
        return value.Length > maxLength ? value[..maxLength] : value;
    }

    /// <summary>
    /// Converts Unix nanoseconds to a DateTime (UTC).
    /// </summary>
    public static DateTime UnixNanoSecondsToDateTime(ulong unixTimeNanoseconds)
    {
        var ticks = NanosecondsToTicks(unixTimeNanoseconds);
        return DateTime.UnixEpoch.AddTicks(ticks);
    }

    /// <summary>
    /// Converts a DateTime to Unix nanoseconds.
    /// </summary>
    public static ulong DateTimeToUnixNanoseconds(DateTime dateTime)
    {
        var timeSinceEpoch = dateTime.ToUniversalTime() - DateTime.UnixEpoch;
        return (ulong)timeSinceEpoch.Ticks * TimeSpan.NanosecondsPerTick;
    }

    /// <summary>
    /// Converts nanoseconds to ticks.
    /// </summary>
    public static long NanosecondsToTicks(ulong nanoseconds)
    {
        return (long)(nanoseconds / TimeSpan.NanosecondsPerTick);
    }

    /// <summary>
    /// Converts nanoseconds to a TimeSpan.
    /// </summary>
    public static TimeSpan NanosecondsToTimeSpan(ulong nanoseconds)
    {
        return TimeSpan.FromTicks(NanosecondsToTicks(nanoseconds));
    }

    /// <summary>
    /// Calculates duration as a TimeSpan from start and end nanosecond timestamps.
    /// </summary>
    public static TimeSpan CalculateDuration(ulong? startNano, ulong? endNano)
    {
        if (startNano.HasValue && endNano.HasValue && endNano.Value >= startNano.Value)
        {
            return NanosecondsToTimeSpan(endNano.Value - startNano.Value);
        }
        return TimeSpan.Zero;
    }

    /// <summary>
    /// Formats a Unix nanosecond timestamp to a time string (HH:mm:ss.fff).
    /// </summary>
    /// <returns>Formatted time string or empty string if null.</returns>
    public static string FormatNanoTimestamp(ulong? nanos)
    {
        if (nanos.HasValue)
        {
            return UnixNanoSecondsToDateTime(nanos.Value)
                .ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }
        return "";
    }
}
