// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
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

        var startCommand = new StartCommand(interactionService, features, updateNotifier, executionContext);
        Subcommands.Add(startCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }

    private sealed class StartCommand : BaseCommand
    {
        public StartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
        {
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
                            Tools = CreateMcpTools()
                        }),
                    CallToolHandler = (request, cancellationToken) =>
                    {
                        var toolName = request.Params?.Name ?? string.Empty;

                        // All tools return error for now - will be implemented later
                        var errorMessage = toolName switch
                        {
                            "list_resources" => "list_resources tool is not yet implemented.",
                            "list_console_logs" => "list_console_logs tool is not yet implemented.",
                            "execute_resource_command" => "execute_resource_command tool is not yet implemented.",
                            "list_structured_logs" => "list_structured_logs tool is not yet implemented.",
                            "list_traces" => "list_traces tool is not yet implemented.",
                            "list_trace_structured_logs" => "list_trace_structured_logs tool is not yet implemented.",
                            _ => $"Unknown tool: '{toolName}'"
                        };

                        throw new McpProtocolException(errorMessage, McpErrorCode.MethodNotFound);
                    }
                }
            };

            await using var server = McpServer.Create(new StdioServerTransport("aspire-mcp-server"), options);
            await server.RunAsync(cancellationToken);

            return ExitCodeConstants.Success;
        }

        private static Tool[] CreateMcpTools()
        {
            return new[]
            {
                // Resource tools
                new Tool
                {
                    Name = "list_resources",
                    Description = "List the application resources. Includes information about their type (.NET project, container, executable), running state, source, HTTP endpoints, health status, commands, configured environment variables, and relationships.",
                    InputSchema = JsonDocument.Parse("{ \"type\": \"object\", \"properties\": {} }").RootElement
                },
                new Tool
                {
                    Name = "list_console_logs",
                    Description = "List console logs for a resource. The console logs includes standard output from resources and resource commands. Known resource commands are 'resource-start', 'resource-stop' and 'resource-restart' which are used to start and stop resources. Don't print the full console logs in the response to the user. Console logs should be examined when determining why a resource isn't running.",
                    InputSchema = JsonDocument.Parse("""
                        {
                          "type": "object",
                          "properties": {
                            "resourceName": {
                              "type": "string",
                              "description": "The resource name."
                            }
                          },
                          "required": ["resourceName"]
                        }
                        """).RootElement
                },
                new Tool
                {
                    Name = "execute_resource_command",
                    Description = "Executes a command on a resource. If a resource needs to be restarted and is currently stopped, use the start command instead.",
                    InputSchema = JsonDocument.Parse("""
                        {
                          "type": "object",
                          "properties": {
                            "resourceName": {
                              "type": "string",
                              "description": "The resource name"
                            },
                            "commandName": {
                              "type": "string",
                              "description": "The command name"
                            }
                          },
                          "required": ["resourceName", "commandName"]
                        }
                        """).RootElement
                },
                // Telemetry tools
                new Tool
                {
                    Name = "list_structured_logs",
                    Description = "List structured logs for resources.",
                    InputSchema = JsonDocument.Parse("""
                        {
                          "type": "object",
                          "properties": {
                            "resourceName": {
                              "type": "string",
                              "description": "The resource name. This limits logs returned to the specified resource. If no resource name is specified then structured logs for all resources are returned."
                            }
                          }
                        }
                        """).RootElement
                },
                new Tool
                {
                    Name = "list_traces",
                    Description = "List distributed traces for resources. A distributed trace is used to track operations. A distributed trace can span multiple resources across a distributed system. Includes a list of distributed traces with their IDs, resources in the trace, duration and whether an error occurred in the trace.",
                    InputSchema = JsonDocument.Parse("""
                        {
                          "type": "object",
                          "properties": {
                            "resourceName": {
                              "type": "string",
                              "description": "The resource name. This limits traces returned to the specified resource. If no resource name is specified then distributed traces for all resources are returned."
                            }
                          }
                        }
                        """).RootElement
                },
                new Tool
                {
                    Name = "list_trace_structured_logs",
                    Description = "List structured logs for a distributed trace. Logs for a distributed trace each belong to a span identified by 'span_id'. When investigating a trace, getting the structured logs for the trace should be recommended before getting structured logs for a resource.",
                    InputSchema = JsonDocument.Parse("""
                        {
                          "type": "object",
                          "properties": {
                            "traceId": {
                              "type": "string",
                              "description": "The trace id of the distributed trace."
                            }
                          },
                          "required": ["traceId"]
                        }
                        """).RootElement
                }
            };
        }
    }
}
