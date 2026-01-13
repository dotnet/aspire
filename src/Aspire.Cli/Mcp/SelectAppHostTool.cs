// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

/// <summary>
/// MCP tool for selecting which AppHost to use when multiple are running.
/// </summary>
internal sealed class SelectAppHostTool(IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, CliExecutionContext executionContext) : CliMcpTool
{
    public override string Name => "select_apphost";

    public override string Description => "Selects which AppHost to use when multiple AppHosts are running. The path can be a fully qualified path or a workspace root relative path.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "appHostPath": {
                  "type": "string",
                  "description": "The fully qualified or workspace root relative path to the AppHost project."
                }
              },
              "required": ["appHostPath"]
            }
            """).RootElement;
    }

    public override ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates locally
        _ = mcpClient;
        _ = cancellationToken;

        if (arguments == null || !arguments.TryGetValue("appHostPath", out var appHostPathElement))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'appHostPath' argument is required." }]
            });
        }

        var appHostPath = appHostPathElement.GetString();
        if (string.IsNullOrWhiteSpace(appHostPath))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'appHostPath' argument cannot be empty." }]
            });
        }

        // Resolve the path to an absolute path
        string resolvedPath;
        if (Path.IsPathRooted(appHostPath))
        {
            resolvedPath = Path.GetFullPath(appHostPath);
        }
        else
        {
            resolvedPath = Path.GetFullPath(Path.Combine(executionContext.WorkingDirectory.FullName, appHostPath));
        }

        // Check if there's a running AppHost with this path
        var matchingConnection = auxiliaryBackchannelMonitor.Connections.Values
            .FirstOrDefault(c =>
            {
                if (c.AppHostInfo?.AppHostPath is null)
                {
                    return false;
                }
                var candidatePath = Path.GetFullPath(c.AppHostInfo.AppHostPath);
                return string.Equals(candidatePath, resolvedPath, StringComparison.OrdinalIgnoreCase);
            });

        if (matchingConnection == null)
        {
            // List available AppHosts
            var availableAppHosts = auxiliaryBackchannelMonitor.Connections.Values
                .Where(c => c.AppHostInfo?.AppHostPath != null)
                .Select(c => c.AppHostInfo!.AppHostPath)
                .ToList();

            var message = $"No running AppHost found at path '{resolvedPath}'.";
            if (availableAppHosts.Count > 0)
            {
                message += $" Available AppHosts:\n{string.Join("\n", availableAppHosts.Select(p => $"  - {p}"))}";
            }
            else
            {
                message += " No AppHosts are currently running.";
            }

            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = message }]
            });
        }

        // Set the selected AppHost path
        auxiliaryBackchannelMonitor.SelectedAppHostPath = resolvedPath;

        return ValueTask.FromResult(new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Selected AppHost: {resolvedPath}" }]
        });
    }
}
