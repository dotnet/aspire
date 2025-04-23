// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

/// <summary>
/// This time provider is used to provide the time zone information from the browser to the server.
/// It is a different type because we want to log setting the time zone, and we want a distinct type
/// to register with DI:
/// - BrowserTimeProvider must be scoped to the user's session.
/// - The built-in TimeProvider registration must be singleton for the system time (used by auth).
/// </summary>
public class BrowserTimeProvider : TimeProvider
{
    private readonly ILogger _logger;
    private TimeZoneInfo? _browserLocalTimeZone;

    public BrowserTimeProvider(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(BrowserTimeProvider));
    }

    public override TimeZoneInfo LocalTimeZone
    {
        get => _browserLocalTimeZone ?? base.LocalTimeZone;
    }

    public void SetBrowserTimeZone(string? timeZone)
    {
        if (string.IsNullOrEmpty(timeZone) || !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var timeZoneInfo))
        {
            _logger.LogWarning("Couldn't find time zone '{TimeZone}'. Defaulting to UTC.", timeZone);
            timeZoneInfo = TimeZoneInfo.Utc;
        }

        _logger.LogDebug("Browser time zone set to '{TimeZone}' with UTC offset {UtcOffset}.", timeZoneInfo.Id, timeZoneInfo.BaseUtcOffset);
        _browserLocalTimeZone = timeZoneInfo;
    }
}
