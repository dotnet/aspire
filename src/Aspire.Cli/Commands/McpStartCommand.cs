// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Commands;

internal sealed class McpStartCommand : BaseCommand
{
    private readonly Dictionary<string, CliMcpTool> _tools;
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly CliExecutionContext _executionContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpStartCommand> _logger;

    public McpStartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, ILoggerFactory loggerFactory, ILogger<McpStartCommand> logger)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _executionContext = executionContext;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _tools = new Dictionary<string, CliMcpTool>
        {
            ["list_resources"] = new ListResourcesTool(),
            ["list_console_logs"] = new ListConsoleLogsTool(),
            ["execute_resource_command"] = new ExecuteResourceCommandTool(),
            ["list_structured_logs"] = new ListStructuredLogsTool(),
            ["list_traces"] = new ListTracesTool(),
            ["list_trace_structured_logs"] = new ListTraceStructuredLogsTool(),
            ["select_apphost"] = new SelectAppHostTool(auxiliaryBackchannelMonitor, executionContext)
        };
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var options = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = "aspire-mcp-server",
                Version = VersionHelper.GetDefaultTemplateVersion()
            },
            Handlers = new McpServerHandlers()
            {
                ListToolsHandler = HandleListToolsAsync,
                CallToolHandler = HandleCallToolAsync
            },        
        };

        await using var server = McpServer.Create(new StdioServerTransport("aspire-mcp-server"), options);
        await server.RunAsync(cancellationToken);

        return ExitCodeConstants.Success;
    }

    private ValueTask<ListToolsResult> HandleListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        // Parameters required by delegate signature
        _ = request;
        _ = cancellationToken;

        _logger.LogDebug("MCP ListTools request received");

        var tools = _tools.Values.Select(tool => new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.GetInputSchema()
        }).ToArray();

        _logger.LogDebug("Returning {ToolCount} tools: {ToolNames}", tools.Length, string.Join(", ", tools.Select(t => t.Name)));

        return ValueTask.FromResult(new ListToolsResult
        {
            Tools = tools
        });
    }

    private async ValueTask<CallToolResult> HandleCallToolAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        var toolName = request.Params?.Name ?? string.Empty;

        _logger.LogDebug("MCP CallTool request received for tool: {ToolName}", toolName);

        if (_tools.TryGetValue(toolName, out var tool))
        {
            // Handle select_apphost tool specially - it doesn't need an MCP connection
            if (toolName == "select_apphost")
            {
                return await tool.CallToolAsync(null!, request.Params?.Arguments, cancellationToken);
            }

            // Get the appropriate connection using the new selection logic
            var connection = GetSelectedConnection();
            if (connection == null)
            {
                _logger.LogWarning("No Aspire AppHost is currently running");
                throw new McpProtocolException(
                    "No Aspire AppHost is currently running. " +
                    "To use Aspire MCP tools, you must first start an Aspire application by running 'aspire run' in your AppHost project directory. " +
                    "Once the application is running, the MCP tools will be able to connect to the dashboard and execute commands.",
                    McpErrorCode.InternalError);
            }

            if (connection.McpInfo == null)
            {
                _logger.LogWarning("Dashboard is not available in the running AppHost");
                throw new McpProtocolException(
                    "The Aspire Dashboard is not available in the running AppHost. " +
                    "The dashboard must be enabled to use MCP tools. " +
                    "Ensure your AppHost is configured with the dashboard enabled (this is the default configuration).",
                    McpErrorCode.InternalError);
            }

            _logger.LogInformation(
                "Connecting to dashboard MCP server. " +
                "Dashboard URL: {EndpointUrl}, " +
                "AppHost Path: {AppHostPath}, " +
                "AppHost PID: {AppHostPid}, " +
                "CLI PID: {CliPid}",
                connection.McpInfo.EndpointUrl,
                connection.AppHostInfo?.AppHostPath ?? "N/A",
                connection.AppHostInfo?.ProcessId.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                connection.AppHostInfo?.CliProcessId?.ToString(CultureInfo.InvariantCulture) ?? "N/A");

            // Create HTTP transport to the dashboard's MCP server
            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(connection.McpInfo.EndpointUrl),
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["x-mcp-api-key"] = connection.McpInfo.ApiToken
                }
            };

            using var httpClient = new HttpClient();

            await using var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

            // Create MCP client to communicate with the dashboard
            await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

            _logger.LogDebug("Calling tool {ToolName} on dashboard MCP server", toolName);

            // Call the tool with the MCP client
            try
            {
                _logger.LogDebug("Invoking CallToolAsync for tool {ToolName} with arguments: {Arguments}", toolName, request.Params?.Arguments);
                var result = await tool.CallToolAsync(mcpClient, request.Params?.Arguments, cancellationToken);
                _logger.LogDebug("CallToolAsync for tool {ToolName} completed successfully", toolName);

                _logger.LogDebug("Tool {ToolName} completed successfully", toolName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling tool {ToolName}", toolName);
                throw;
            }
        }

        _logger.LogWarning("Unknown tool requested: {ToolName}", toolName);
        throw new McpProtocolException($"Unknown tool: '{toolName}'", McpErrorCode.MethodNotFound);
    }

    /// <summary>
    /// Gets the appropriate AppHost connection based on the selection logic:
    /// 1. If a specific AppHost is selected via select_apphost, use that
    /// 2. Otherwise, look for in-scope connections (AppHosts within the working directory)
    /// 3. If exactly one in-scope connection exists, use it
    /// 4. If multiple in-scope connections exist, throw an error listing them
    /// 5. If no in-scope connections exist, fall back to the first available connection
    /// </summary>
    private AppHostConnection? GetSelectedConnection()
    {
        var connections = _auxiliaryBackchannelMonitor.Connections.Values.ToList();

        if (connections.Count == 0)
        {
            return null;
        }

        // Check if a specific AppHost was selected
        var selectedPath = _auxiliaryBackchannelMonitor.SelectedAppHostPath;
        if (!string.IsNullOrEmpty(selectedPath))
        {
            var selectedConnection = connections.FirstOrDefault(c =>
                c.AppHostInfo?.AppHostPath != null &&
                string.Equals(c.AppHostInfo.AppHostPath, selectedPath, StringComparison.OrdinalIgnoreCase));

            if (selectedConnection != null)
            {
                _logger.LogDebug("Using explicitly selected AppHost: {AppHostPath}", selectedPath);
                return selectedConnection;
            }

            _logger.LogWarning("Selected AppHost at '{SelectedPath}' is no longer running, falling back to selection logic", selectedPath);
            // Clear the selection since the AppHost is no longer available
            _auxiliaryBackchannelMonitor.SelectedAppHostPath = null;
        }

        // Get in-scope connections
        var inScopeConnections = connections.Where(c => c.IsInScope).ToList();

        if (inScopeConnections.Count == 1)
        {
            _logger.LogDebug("Using single in-scope AppHost: {AppHostPath}", inScopeConnections[0].AppHostInfo?.AppHostPath ?? "N/A");
            return inScopeConnections[0];
        }

        if (inScopeConnections.Count > 1)
        {
            var paths = inScopeConnections
                .Where(c => c.AppHostInfo?.AppHostPath != null)
                .Select(c => c.AppHostInfo!.AppHostPath)
                .ToList();

            var pathsList = string.Join("\n", paths.Select(p => $"  - {p}"));

            throw new McpProtocolException(
                $"Multiple Aspire AppHosts are running in the scope of the MCP server's working directory. " +
                $"Use the 'select_apphost' tool to specify which AppHost to use.\n\nRunning AppHosts:\n{pathsList}",
                McpErrorCode.InternalError);
        }

        // No in-scope connections, fall back to first available
        _logger.LogDebug("No in-scope AppHost found, using first available connection");
        return connections.FirstOrDefault();
    }
}
