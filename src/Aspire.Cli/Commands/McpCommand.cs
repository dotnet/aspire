// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Commands;

internal sealed class McpCommand : BaseCommand
{
    public McpCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
        : base("mcp", McpCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var schemaJson = """
            { 
              "type": "object",
              "properties": {
                "message": {
                  "type": "string",
                  "description": "The input to echo back"
                }
              },
              "required": ["message"]
            }
            """;

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
                        Tools = new[]
                        {
                            new Tool
                            {
                                Name = "echo",
                                Description = "Echoes the input back to the client.",
                                InputSchema = JsonDocument.Parse(schemaJson).RootElement
                            }
                        }
                    }),
                CallToolHandler = (request, cancellationToken) =>
                {
                    if (request.Params?.Name == "echo")
                    {
                        if (request.Params.Arguments?.TryGetValue("message", out var message) is not true)
                        {
                            throw new McpProtocolException(
                                "Missing required argument 'message'",
                                McpErrorCode.InvalidParams
                            );
                        }
                        return ValueTask.FromResult(new CallToolResult
                        {
                            Content = new[]
                            {
                                new TextContentBlock
                                {
                                    Text = $"Echo: {message}",
                                    Type = "text"
                                }
                            }
                        });
                    }
                    throw new McpProtocolException(
                        $"Unknown tool: '{request.Params?.Name}'",
                        McpErrorCode.InvalidRequest
                    );
                }
            }
        };

        await using var server = McpServer.Create(new StdioServerTransport("aspire-mcp-server"), options);
        await server.RunAsync(cancellationToken);

        return ExitCodeConstants.Success;
    }
}
