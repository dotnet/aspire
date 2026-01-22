// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Mcp;

/// <summary>
/// Defines the names of MCP tools exposed by the Aspire CLI.
/// </summary>
internal static class KnownMcpTools
{
    internal const string ListResources = "list_resources";
    internal const string ListConsoleLogs = "list_console_logs";
    internal const string ExecuteResourceCommand = "execute_resource_command";
    internal const string ListStructuredLogs = "list_structured_logs";
    internal const string ListTraces = "list_traces";
    internal const string ListTraceStructuredLogs = "list_trace_structured_logs";

    internal const string SelectAppHost = "select_apphost";
    internal const string ListAppHosts = "list_apphosts";
    internal const string ListIntegrations = "list_integrations";
    internal const string GetIntegrationDocs = "get_integration_docs";
    internal const string Doctor = "doctor";
    internal const string RefreshTools = "refresh_tools";

    public static bool IsLocalTool(string toolName) => toolName is
        KnownMcpTools.SelectAppHost or
        KnownMcpTools.ListAppHosts or
        KnownMcpTools.ListIntegrations or
        KnownMcpTools.GetIntegrationDocs or
        KnownMcpTools.Doctor or
        KnownMcpTools.RefreshTools;

    public static bool IsDashboardTool(string toolName) => toolName is
        KnownMcpTools.ListResources or
        KnownMcpTools.ListConsoleLogs or
        KnownMcpTools.ExecuteResourceCommand or
        KnownMcpTools.ListStructuredLogs or
        KnownMcpTools.ListTraces or
        KnownMcpTools.ListTraceStructuredLogs;

}
