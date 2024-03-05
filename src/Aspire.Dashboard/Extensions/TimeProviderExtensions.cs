// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Extensions;

internal static class TimeProviderExtensions
{
    public static DateTime ToLocal(this TimeProvider timeProvider, DateTimeOffset utcDateTimeOffset)
    {
        var dateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTimeOffset.UtcDateTime, timeProvider.LocalTimeZone);
        dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);

        return dateTime;
    }
}
