// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public static class TelemetryPropertyKeys
{
    private const string AspireDashboardPropertyPrefix = "Aspire.Dashboard";

    // Default properties
    public const string DashboardVersion = AspireDashboardPropertyPrefix + ".Version";
    public const string DashboardBuildId = AspireDashboardPropertyPrefix + ".BuildId";

    public const string DashboardPageId = AspireDashboardPropertyPrefix + ".PageId";

    public const string ConsoleLogsShowTimestamp = AspireDashboardPropertyPrefix + ".ConsoleLogs.ShowTimestamp";
    public const string ConsoleLogsApplicationName = AspireDashboardPropertyPrefix + ".ConsoleLogs.ApplicationName";
}
