// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Aspire.Shared.Mcp;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Dashboard.Mcp;

public static class McpExtensions
{
    public static IMcpServerBuilder AddAspireMcpTools(this IServiceCollection services, DashboardOptions dashboardOptions)
    {
        services.AddSingleton<ResourceMcpProxyService>();

        var builder = services.AddMcpServer(options =>
        {
            var icons = McpIconHelper.GetAspireIcons(typeof(McpExtensions).Assembly, "Aspire.Dashboard.Mcp.Resources");

            options.ServerInfo = new Implementation
            {
                Name = "Aspire MCP",
                Version = VersionHelpers.DashboardDisplayVersion ?? "1.0.0",
                Icons = icons
            };
            options.ServerInstructions =
                """
                ## Description
                This MCP Server provides various tools for managing Aspire resources, logs, traces and commands.

                ## Instructions
                - When a resource, structured log or trace is returned, include a link to the Aspire dashboard using dashboard_link
                - When a resource state (running, stopped, starting, ...) is returned, and add an emoji colored badge next to it (green, red, orange, etc).

                ## Tools

                """;
            // Configure server capabilities to support ListChanged event for dynamic tool discovery
            options.Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability
                {
                    ListChanged = true
                }
            };
        }).WithHttpTransport();

        // Always register telemetry tools
        builder.WithTools<AspireTelemetryMcpTools>();

        // Only register resource tools if the resource service is configured
        if (dashboardOptions.ResourceServiceClient.GetUri() is not null)
        {
            builder.WithTools<AspireResourceMcpTools>();

            // Intercept ListTools and CallTool to proxy calls to resource MCP servers
            // This has two purposes:
            // 1. To add the proxied tools to the list of available tools
            // 2. To record telemetry about tool usage
            builder
                .AddListToolsFilter((next) => async (RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken) =>
                {
                    // Calls here are via the tools/list endpoint. See https://modelcontextprotocol.info/docs/concepts/tools/
                    // There is no tool name so we hardcode name to list_tools here so we can reuse the same event.
                    //
                    // We want to track when users list tools as it's an indicator of whether Aspire MCP is configured (client tools refresh tools via it).
                    // It's called even if no Aspire tools end up being used.
                    var result = await RecordCallToolNameAsync<ListToolsRequestParams, ListToolsResult>(next, request, "list_tools", cancellationToken).ConfigureAwait(false);

                    var proxyService = request.Services?.GetService<ResourceMcpProxyService>();
                    if (proxyService is not null)
                    {
                        result.Tools.Clear();

                        // Add registered tools (telemetry and resource tools)
                        var mcpTools = request.Services?.GetServices<McpServerTool>() ?? [];
                        foreach (var tool in mcpTools)
                        {
                            result.Tools.Add(tool.ProtocolTool);
                        }

                        // Add proxied tools from resource MCP servers
                        var proxiedTools = await proxyService.GetToolsAsync(cancellationToken).ConfigureAwait(false);
                        if (proxiedTools.Count > 0)
                        {
                            var tools = result.Tools?.ToList() ?? [];
                            tools.AddRange(proxiedTools);
                            result.Tools = tools;
                        }
                    }

                    return result;
                })
                .AddCallToolFilter((next) => async (RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken) =>
                {
                    var proxyService = request.Services?.GetService<ResourceMcpProxyService>();
                    if (proxyService is not null && request.Params?.Name is { Length: > 0 } toolName)
                    {
                        var proxiedResult = await proxyService.TryHandleCallAsync(toolName, request.Params.Arguments, cancellationToken).ConfigureAwait(false);
                        if (proxiedResult is not null)
                        {
                            return await RecordCallToolNameAsync<CallToolRequestParams, CallToolResult>(
                                (_, _) => ValueTask.FromResult(proxiedResult),
                                request,
                                toolName,
                                cancellationToken).ConfigureAwait(false);
                        }
                    }

                    return await RecordCallToolNameAsync<CallToolRequestParams, CallToolResult>(next, request, request.Params?.Name, cancellationToken).ConfigureAwait(false);
                });
        }

        return builder;
    }

    private static async ValueTask<TResult> RecordCallToolNameAsync<TParams, TResult>(McpRequestHandler<TParams, TResult> next, RequestContext<TParams> request, string? toolCallName, CancellationToken cancellationToken)
    {
        // Record the tool name to telemetry.
        OperationContextProperty? operationId = null;
        var telemetryService = request.Services?.GetService<DashboardTelemetryService>();
        if (telemetryService != null && toolCallName != null)
        {
            var startToolCall = telemetryService.StartOperation(TelemetryEventKeys.McpToolCall,
                new Dictionary<string, AspireTelemetryProperty>
                {
                    { TelemetryPropertyKeys.McpToolName, new AspireTelemetryProperty(toolCallName) },
                });

            operationId = startToolCall.Properties.FirstOrDefault();
        }

        try
        {
            var result = await next(request, cancellationToken).ConfigureAwait(false);

            if (telemetryService is not null && operationId is not null)
            {
                telemetryService.EndOperation(operationId, TelemetryResult.Success);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (telemetryService is not null && operationId is not null)
            {
                telemetryService.EndOperation(operationId, TelemetryResult.Failure, ex.Message);
            }

            throw;
        }
    }
}
