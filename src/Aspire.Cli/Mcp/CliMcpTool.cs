// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

/// <summary>
/// Base class for MCP tools in the Aspire CLI.
/// </summary>
internal abstract class CliMcpTool
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the description of the tool.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the input schema for the tool as a JsonElement.
    /// </summary>
    /// <returns>The JSON schema describing the tool's input parameters.</returns>
    public abstract JsonElement GetInputSchema();

    /// <summary>
    /// Executes the tool with the provided arguments.
    /// </summary>
    /// <param name="mcpClient">The MCP client instance to use for communicating with the dashboard.</param>
    /// <param name="arguments">The arguments passed to the tool.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    public abstract ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken);
}
