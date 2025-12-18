// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Mcp;

/// <summary>
/// Represents an MCP bridge resource that proxies stdio-based MCP servers to HTTP endpoints.
/// </summary>
/// <remarks>
/// The MCP bridge spawns a stdio-based MCP server process and exposes it via an HTTP endpoint
/// that the Aspire Dashboard can connect to. This enables stdio MCP servers (like npx-based servers)
/// to integrate with the Dashboard's HTTP-only MCP proxy.
/// </remarks>
public class McpBridgeResource : ExecutableResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpBridgeResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="command">The command to execute for the bridge.</param>
    /// <param name="workingDirectory">The working directory of the bridge executable.</param>
    public McpBridgeResource(string name, string command, string workingDirectory)
        : base(name, command, workingDirectory)
    {
    }

    /// <summary>
    /// Gets or sets the command to execute for the stdio MCP server.
    /// </summary>
    public string? McpServerCommand { get; set; }

    /// <summary>
    /// Gets or sets the arguments to pass to the stdio MCP server command.
    /// </summary>
    public string[]? McpServerArguments { get; set; }

    /// <summary>
    /// Gets or sets the working directory for the stdio MCP server process.
    /// </summary>
    public string? McpServerWorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the namespace for the MCP server tools.
    /// </summary>
    /// <remarks>
    /// The namespace is used to prefix tool names to avoid conflicts when multiple MCP servers are registered.
    /// </remarks>
    public string? McpNamespace { get; set; }

    /// <summary>
    /// Gets or sets the API key for securing the MCP bridge HTTP endpoint.
    /// </summary>
    /// <remarks>
    /// If specified, the API key is required in the Authorization header when connecting to the bridge endpoint.
    /// </remarks>
    public string? ApiKey { get; set; }
}
