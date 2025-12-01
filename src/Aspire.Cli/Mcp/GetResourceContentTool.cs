// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

/// <summary>
/// MCP tool for getting agent content from a resource in the AppHost.
/// </summary>
internal sealed class GetResourceContentTool(IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, CliExecutionContext executionContext) : CliMcpTool
{
    public override string Name => "get_resource_content";

    public override string Description => "Get agent content from a resource in the AppHost. Resources can expose content specifically for AI agents via the WithAgentContent API. This tool invokes the callback on the resource to retrieve contextual information.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The name of the resource to get content from."
                }
              },
              "required": ["resourceName"],
              "additionalProperties": false,
              "description": "Gets agent content from a specific resource. The resource must have been configured with WithAgentContent in the AppHost."
            }
            """).RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates through the auxiliary backchannel
        _ = mcpClient;

        if (arguments == null || !arguments.TryGetValue("resourceName", out var resourceNameElement))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'resourceName' parameter is required." }]
            };
        }

        var resourceName = resourceNameElement.GetString();
        if (string.IsNullOrEmpty(resourceName))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'resourceName' parameter cannot be empty." }]
            };
        }

        try
        {
            // Find all connections from AppHosts that were started within our working directory
            var connections = auxiliaryBackchannelMonitor.GetConnectionsForWorkingDirectory(executionContext.WorkingDirectory);

            if (connections.Count == 0)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = "No Aspire AppHost is currently running. Start an Aspire application with 'aspire run' first." }]
                };
            }

            // Get the selected connection
            var selectedConnection = auxiliaryBackchannelMonitor.SelectedConnection;
            if (selectedConnection == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = "No AppHost is selected. Use select_apphost to select an AppHost first." }]
                };
            }

            // Call the RPC method to get agent content from the resource
            var content = await selectedConnection.GetResourceAgentContentAsync(resourceName, cancellationToken);

            if (content == null)
            {
                return new CallToolResult
                {
                    Content = [new TextContentBlock { Text = $"Resource '{resourceName}' does not have any agent content configured." }]
                };
            }

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = content }]
            };
        }
        catch (Exception ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Failed to get resource content: {ex.Message}" }]
            };
        }
    }
}
