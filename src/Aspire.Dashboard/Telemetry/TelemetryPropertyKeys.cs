﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public static class TelemetryPropertyKeys
{
    private const string AspireDashboardPropertyPrefix = "Aspire.Dashboard.";

    // Default properties
    public const string DashboardVersion = AspireDashboardPropertyPrefix + "Version";
    public const string DashboardBuildId = AspireDashboardPropertyPrefix + "BuildId";

    // IComponentWithTelemetry properties
    public const string DashboardComponentId = AspireDashboardPropertyPrefix + "ComponentId";
    public const string UserAgent = AspireDashboardPropertyPrefix + "UserAgent";

    public const string ApplicationInstanceId = AspireDashboardPropertyPrefix + "Metrics.ApplicationInstanceId";

    // ConsoleLogs properties
    public const string ConsoleLogsShowTimestamp = AspireDashboardPropertyPrefix + "ConsoleLogs.ShowTimestamp";
    public const string ConsoleLogsApplicationName = AspireDashboardPropertyPrefix + "ConsoleLogs.ApplicationName";

    // Metrics properties
    public const string MetricsApplicationIsReplica = AspireDashboardPropertyPrefix + "Metrics.ApplicationIsReplica";
    public const string MetricsInstrumentsCount = AspireDashboardPropertyPrefix + "Metrics.InstrumentsCount";
    public const string MetricsSelectedMeter = AspireDashboardPropertyPrefix + "Metrics.SelectedMeter";
    public const string MetricsSelectedInstrument = AspireDashboardPropertyPrefix + "Metrics.SelectedInstrument";
    public const string MetricsSelectedDuration = AspireDashboardPropertyPrefix + "Metrics.SelectedDuration";
    public const string MetricsSelectedView = AspireDashboardPropertyPrefix + "Metrics.SelectedView";

    // Exception properties
    public const string ExceptionType = AspireDashboardPropertyPrefix + "Exception.Type";
    public const string ExceptionMessage = AspireDashboardPropertyPrefix + "Exception.Message";
    public const string ExceptionStackTrace = AspireDashboardPropertyPrefix + "Exception.StackTrace";

    // Resources properties
    public const string ResourceType = AspireDashboardPropertyPrefix + "Resource.Type";
    public const string ResourceView = AspireDashboardPropertyPrefix + "Resource.View";

    // Error properties
    public const string ErrorRequestId = AspireDashboardPropertyPrefix + "RequestId";

    // Trace detail properties
    public const string TraceDetailTraceId = AspireDashboardPropertyPrefix + "TraceDetail.TraceId";

    // Structured logs properties
    public const string StructuredLogsSelectedApplication = AspireDashboardPropertyPrefix + "StructuredLogs.SelectedApplication";
    public const string StructuredLogsSelectedLogLevel = AspireDashboardPropertyPrefix + "StructuredLogs.SelectedLogLevel";
    public const string StructuredLogsFilterCount = AspireDashboardPropertyPrefix + "StructuredLogs.FilterCount";
    public const string StructuredLogsTraceId = AspireDashboardPropertyPrefix + "StructuredLogs.TraceId";
    public const string StructuredLogsSpanId = AspireDashboardPropertyPrefix + "StructuredLogs.SpanId";

    // Command properties
    public const string CommandName = AspireDashboardPropertyPrefix + "Command.Name";
}
