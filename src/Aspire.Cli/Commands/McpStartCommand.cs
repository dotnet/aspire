// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
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
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpStartCommand> _logger;

    public McpStartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, ILoggerFactory loggerFactory, ILogger<McpStartCommand> logger)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _tools = new Dictionary<string, CliMcpTool>
        {
            ["list_resources"] = new ListResourcesTool(),
            ["list_console_logs"] = new ListConsoleLogsTool(),
            ["execute_resource_command"] = new ExecuteResourceCommandTool(),
            ["list_structured_logs"] = new ListStructuredLogsTool(),
            ["list_traces"] = new ListTracesTool(),
            ["list_trace_structured_logs"] = new ListTraceStructuredLogsTool()
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
            // Get the first auxiliary backchannel connection
            var connection = _auxiliaryBackchannelMonitor.Connections.Values.FirstOrDefault();
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

            _logger.LogDebug("Connecting to dashboard MCP server at {EndpointUrl}", connection.McpInfo.EndpointUrl);

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
}
