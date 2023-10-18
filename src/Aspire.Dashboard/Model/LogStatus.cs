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

    public const string LoadingProjects = "Loading Projects...";
    public const string NoProjectSelected = "No Project Selected";
    public const string LoadingExecutables = "Loading Executables...";
    public const string NoExecutableSelected = "No Executable Selected";
    public const string LoadingContainers = "Loading Containers...";
    public const string NoContainerSelected = "No Container Selected";
}
