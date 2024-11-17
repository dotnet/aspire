// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Utils;

internal static class BrowserStorageKeys
{
    public const string UnsecuredTelemetryMessageDismissedKey = "Aspire_Telemetry_UnsecuredMessageDismissed";

    public const string TracesPageState = "Aspire_PageState_Traces";
    public const string StructuredLogsPageState = "Aspire_PageState_StructuredLogs";
    public const string MetricsPageState = "Aspire_PageState_Metrics";
    public const string ConsoleLogsPageState = "Aspire_PageState_ConsoleLogs";

    public static string SplitterOrientationKey(string viewKey)
    {
        return $"Aspire_SplitterOrientation_{viewKey}";
    }

    public static string SplitterSizeKey(string viewKey, Orientation orientation)
    {
        return $"Aspire_SplitterSize_{orientation}_{viewKey}";
    }
}
