// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

/// <summary>
/// MCP tool for getting documentation and guidance for a specific resource in the AppHost.
/// </summary>
internal sealed class GetResourceDocsTool(IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, CliExecutionContext executionContext) : CliMcpTool
{
    public override string Name => "get_resource_docs";

    public override string Description => "IMPORTANT: Before writing ANY Aspire code that interacts with a specific resource (such as configuring, connecting to, or using a resource), you MUST call this tool first to get resource-specific documentation and guidance. This tool retrieves essential information about how to properly work with the resource, including connection patterns, configuration options, and best practices. Failing to consult this documentation may result in incorrect or suboptimal code.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The name of the resource to get documentation for."
                }
              },
              "required": ["resourceName"],
              "additionalProperties": false,
              "description": "Gets documentation and guidance for a specific resource. MUST be called before writing any code that interacts with the resource."
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

            // Call the RPC method to get documentation for the resource
            var content = await selectedConnection.GetResourceDocsAsync(resourceName, cancellationToken);

            if (content == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = $"Resource '{resourceName}' was not found in the application model." }]
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
                Content = [new TextContentBlock { Text = $"Failed to get resource documentation: {ex.Message}" }]
            };
        }
    }
}
