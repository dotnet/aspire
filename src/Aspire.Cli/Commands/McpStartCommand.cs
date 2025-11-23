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

    public McpStartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, ILoggerFactory loggerFactory)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _loggerFactory = loggerFactory;
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
                ListToolsHandler = (request, cancellationToken) =>
                    ValueTask.FromResult(new ListToolsResult
                    {
                        Tools = _tools.Values.Select(tool => new Tool
                        {
                            Name = tool.Name,
                            Description = tool.Description,
                            InputSchema = tool.GetInputSchema()
                        }).ToArray()
                    }),
                CallToolHandler = async (request, cancellationToken) =>
                {
                    var toolName = request.Params?.Name ?? string.Empty;

                    if (_tools.TryGetValue(toolName, out var tool))
                    {
                        // Get the first auxiliary backchannel connection
                        var connection = _auxiliaryBackchannelMonitor.Connections.Values.FirstOrDefault();
                        if (connection == null)
                        {
                            throw new McpProtocolException(
                                "No Aspire AppHost is currently running. " +
                                "To use Aspire MCP tools, you must first start an Aspire application by running 'aspire run' in your AppHost project directory. " +
                                "Once the application is running, the MCP tools will be able to connect to the dashboard and execute commands.",
                                McpErrorCode.InternalError);
                        }

                        if (connection.McpInfo == null)
                        {
                            throw new McpProtocolException(
                                "The Aspire Dashboard is not available in the running AppHost. " +
                                "The dashboard must be enabled to use MCP tools. " +
                                "Ensure your AppHost is configured with the dashboard enabled (this is the default configuration).",
                                McpErrorCode.InternalError);
                        }

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

                        // Call the tool with the MCP client
                        return await tool.CallToolAsync(mcpClient, request.Params?.Arguments, cancellationToken);
                    }

                    throw new McpProtocolException($"Unknown tool: '{toolName}'", McpErrorCode.MethodNotFound);
                }
            },
        
        };

        await using var server = McpServer.Create(new StdioServerTransport("aspire-mcp-server"), options);
        await server.RunAsync(cancellationToken);

        return ExitCodeConstants.Success;
    }
}
