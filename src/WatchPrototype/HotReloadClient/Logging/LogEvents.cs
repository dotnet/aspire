// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

internal readonly record struct LogEvent<TArgs>(EventId Id, LogLevel Level, string Message);

internal static class LogEvents
{
    // Non-shared event ids start at 0.
    private static int s_id = 1000;

    private static LogEvent<None> Create(LogLevel level, string message)
        => Create<None>(level, message);

    private static LogEvent<TArgs> Create<TArgs>(LogLevel level, string message)
        => new(new EventId(s_id++), level, message);

    public static void Log(this ILogger logger, LogEvent<None> logEvent)
        => logger.Log(logEvent.Level, logEvent.Id, logEvent.Message);

    public static void Log<TArgs>(this ILogger logger, LogEvent<TArgs> logEvent, TArgs args)
    {
        if (logger.IsEnabled(logEvent.Level))
        {
            logger.Log(logEvent.Level, logEvent.Id, logEvent.Message, GetArgumentValues(args));
        }
    }

    public static void Log<TArg1, TArg2>(this ILogger logger, LogEvent<(TArg1, TArg2)> logEvent, TArg1 arg1, TArg2 arg2)
        => Log(logger, logEvent, (arg1, arg2));

    public static void Log<TArg1, TArg2, TArg3>(this ILogger logger, LogEvent<(TArg1, TArg2, TArg3)> logEvent, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        => Log(logger, logEvent, (arg1, arg2, arg3));

    public static object?[] GetArgumentValues<TArgs>(TArgs args)
    {
        if (args?.GetType() == typeof(None))
        {
            return [];
        }

        if (args is ITuple tuple)
        {
            var values = new object?[tuple.Length];
            for (int i = 0; i < tuple.Length; i++)
            {
                values[i] = tuple[i];
            }

            return values;
        }

        return [args];
    }

    public static readonly LogEvent<int> SendingUpdateBatch = Create<int>(LogLevel.Debug, "Sending update batch #{0}");
    public static readonly LogEvent<int> UpdateBatchCompleted = Create<int>(LogLevel.Debug, "Update batch #{0} completed.");
    public static readonly LogEvent<int> UpdateBatchFailed = Create<int>(LogLevel.Debug, "Update batch #{0} failed.");
    public static readonly LogEvent<int> UpdateBatchCanceled = Create<int>(LogLevel.Debug, "Update batch #{0} canceled.");
    public static readonly LogEvent<(int, string)> UpdateBatchFailedWithError = Create<(int, string)>(LogLevel.Debug, "Update batch #{0} failed with error: {1}");
    public static readonly LogEvent<(int, string)> UpdateBatchExceptionStackTrace = Create<(int, string)>(LogLevel.Debug, "Update batch #{0} exception stack trace: {1}");
    public static readonly LogEvent<string> Capabilities = Create<string>(LogLevel.Debug, "Capabilities: '{0}'.");
    public static readonly LogEvent<None> RefreshingBrowser = Create(LogLevel.Debug, "Refreshing browser.");
    public static readonly LogEvent<None> ReloadingBrowser = Create(LogLevel.Debug, "Reloading browser.");
    public static readonly LogEvent<None> SendingWaitMessage = Create(LogLevel.Debug, "Sending wait message.");
    public static readonly LogEvent<None> NoBrowserConnected = Create(LogLevel.Debug, "No browser is connected.");
    public static readonly LogEvent<None> FailedToReceiveResponseFromConnectedBrowser = Create(LogLevel.Debug, "Failed to receive response from a connected browser.");
    public static readonly LogEvent<None> UpdatingDiagnostics = Create(LogLevel.Debug, "Updating diagnostics.");
    public static readonly LogEvent<string> SendingStaticAssetUpdateRequest = Create<string>(LogLevel.Debug, "Sending static asset update request to connected browsers: '{0}'.");
    public static readonly LogEvent<string> RefreshServerRunningAt = Create<string>(LogLevel.Debug, "Refresh server running at {0}.");
    public static readonly LogEvent<None> ConnectedToRefreshServer = Create(LogLevel.Debug, "Connected to refresh server.");
}

