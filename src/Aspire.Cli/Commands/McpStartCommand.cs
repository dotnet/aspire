// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Commands;

internal sealed class McpStartCommand : BaseCommand
{
    private readonly Dictionary<string, CliMcpTool> _tools;

    public McpStartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
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
                Version = "1.0.0"
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
                CallToolHandler = (request, cancellationToken) =>
                {
                    var toolName = request.Params?.Name ?? string.Empty;

                    if (_tools.TryGetValue(toolName, out var tool))
                    {
                        return tool.CallToolAsync(request.Params?.Arguments, cancellationToken);
                    }

                    throw new McpProtocolException($"Unknown tool: '{toolName}'", McpErrorCode.MethodNotFound);
                }
            }
        };

        await using var server = McpServer.Create(new StdioServerTransport("aspire-mcp-server"), options);
        await server.RunAsync(cancellationToken);

        return ExitCodeConstants.Success;
    }
}
