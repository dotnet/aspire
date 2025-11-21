// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// RPC target for the auxiliary backchannel that provides MCP-related operations.
/// </summary>
/// <remarks>
/// This target provides stateless request/response operations for clients connecting
/// to the auxiliary backchannel, primarily to obtain Dashboard MCP connection information.
/// </remarks>
internal sealed class AuxiliaryBackchannelRpcTarget(
    ILogger<AuxiliaryBackchannelRpcTarget> logger,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Gets the Dashboard MCP connection information including endpoint URL and API token.
    /// </summary>
    /// <returns>The MCP connection information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Dashboard is not enabled or MCP endpoint is not available.</exception>
    public Task<McpConnectionInfo> GetMcpConnectionInfoAsync()
    {
        var dashboardOptions = serviceProvider.GetService<IOptions<DashboardOptions>>();

        if (dashboardOptions is null)
        {
            logger.LogWarning("Dashboard options not found.");
            throw new InvalidOperationException("Dashboard options not found.");
        }

        if (!StringUtils.TryGetUriFromDelimitedString(dashboardOptions.Value.McpEndpointUrl, ";", out var mcpEndpointUri))
        {
            logger.LogWarning("Dashboard MCP endpoint URL could not be parsed from dashboard options.");
            throw new InvalidOperationException("Dashboard MCP endpoint URL is not available.");
        }

        var mcpApiKey = dashboardOptions.Value.McpApiKey;
        if (string.IsNullOrEmpty(mcpApiKey))
        {
            logger.LogWarning("Dashboard MCP API key is not available.");
            throw new InvalidOperationException("Dashboard MCP API key is not available.");
        }

        return Task.FromResult(new McpConnectionInfo
        {
            EndpointUrl = mcpEndpointUri.ToString(),
            ApiToken = mcpApiKey
        });
    }
}

/// <summary>
/// Represents the connection information for the Dashboard MCP server.
/// </summary>
internal sealed class McpConnectionInfo
{
    /// <summary>
    /// Gets or sets the endpoint URL for the Dashboard MCP server.
    /// </summary>
    public required string EndpointUrl { get; init; }

    /// <summary>
    /// Gets or sets the API token for authenticating with the Dashboard MCP server.
    /// </summary>
    public required string ApiToken { get; init; }
}
