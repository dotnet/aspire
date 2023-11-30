// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

internal static class LogStatus
{
    public const string Initializing = "Initializing...";
    public const string InitializingLogViewer = "Initializing log viewer...";
    public const string LogsNotYetAvailable = "Logs not yet available";
    public const string WatchingLogs = "Watching logs...";
    public const string FailedToInitialize = "Failed to initialize";
    public const string FinishedWatchingLogs = "Finished watching logs";
    public const string LoadingResources = "Loading resources ...";
    public const string NoResourceSelected = "No resource selected";
}
