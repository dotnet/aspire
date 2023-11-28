// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

internal static class LogStatus
{
    public const string Initializing = "Initializing...";
    public const string InitializingLogViewer = "Initializing Log Viewer...";
    public const string LogsNotYetAvailable = "Logs Not Yet Available";
    public const string WatchingLogs = "Watching Logs...";
    public const string FailedToInitialize = "Failed to Initialize";
    public const string FinishedWatchingLogs = "Finished Watching Logs";
    public const string LoadingResources = "Loading Resources ...";
    public const string NoResourceSelected = "No Resource Selected";
}
