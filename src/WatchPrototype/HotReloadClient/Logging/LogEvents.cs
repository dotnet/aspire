// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

internal readonly record struct LogEvent(EventId Id, LogLevel Level, string Message);

internal static class LogEvents
{
    // Non-shared event ids start at 0.
    private static int s_id = 1000;

    private static LogEvent Create(LogLevel level, string message)
        => new(new EventId(s_id++), level, message);

    public static void Log(this ILogger logger, LogEvent logEvent, params object[] args)
        => logger.Log(logEvent.Level, logEvent.Id, logEvent.Message, args);

    public static readonly LogEvent UpdatesApplied = Create(LogLevel.Debug, "Updates applied: {0} out of {1}.");
    public static readonly LogEvent Capabilities = Create(LogLevel.Debug, "Capabilities: '{1}'.");
    public static readonly LogEvent HotReloadSucceeded = Create(LogLevel.Information, "Hot reload succeeded.");
    public static readonly LogEvent RefreshingBrowser = Create(LogLevel.Debug, "Refreshing browser.");
    public static readonly LogEvent ReloadingBrowser = Create(LogLevel.Debug, "Reloading browser.");
    public static readonly LogEvent SendingWaitMessage = Create(LogLevel.Debug, "Sending wait message.");
    public static readonly LogEvent NoBrowserConnected = Create(LogLevel.Debug, "No browser is connected.");
    public static readonly LogEvent FailedToReceiveResponseFromConnectedBrowser = Create(LogLevel.Debug, "Failed to receive response from a connected browser.");
    public static readonly LogEvent UpdatingDiagnostics = Create(LogLevel.Debug, "Updating diagnostics.");
    public static readonly LogEvent SendingStaticAssetUpdateRequest = Create(LogLevel.Debug, "Sending static asset update request to connected browsers: '{0}'.");
    public static readonly LogEvent RefreshServerRunningAt = Create(LogLevel.Debug, "Refresh server running at {0}.");
    public static readonly LogEvent ConnectedToRefreshServer = Create(LogLevel.Debug, "Connected to refresh server.");
}
