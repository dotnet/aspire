// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Mcp;

/// <summary>
/// Service responsible for refreshing resource-based MCP tools and sending tool list change notifications.
/// </summary>
internal interface IMcpResourceToolRefreshService
{
    /// <summary>
    /// Gets the current resource tool map. Keys are exposed tool names, values are tuples of (ResourceName, Tool).
    /// </summary>
    IReadOnlyDictionary<string, (string ResourceName, Tool Tool)> ResourceToolMap { get; }

    /// <summary>
    /// Determines whether the resource tool map needs to be refreshed.
    /// </summary>
    /// <returns><c>true</c> if the tool map needs refresh; otherwise, <c>false</c>.</returns>
    bool NeedsRefresh();

    /// <summary>
    /// Marks the resource tool map as needing a refresh.
    /// </summary>
    void InvalidateToolMap();

    /// <summary>
    /// Refreshes the resource tool map by discovering MCP tools from connected resources.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The count of resource tools discovered.</returns>
    Task<int> RefreshResourceToolMapAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends a tools list changed notification to connected MCP clients.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendToolsListChangedNotificationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets the MCP server instance used for sending notifications.
    /// </summary>
    /// <param name="server">The MCP server, or null to clear.</param>
    void SetMcpServer(McpServer? server);
}
