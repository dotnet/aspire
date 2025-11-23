// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
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
    private const string McpEndpointName = "mcp";

    /// <summary>
    /// Gets the Dashboard MCP connection information including endpoint URL and API token.
    /// </summary>
    /// <returns>The MCP connection information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Dashboard is not enabled or MCP endpoint is not available.</exception>
    public Task<McpConnectionInfo> GetMcpConnectionInfoAsync()
    {
        var appModel = serviceProvider.GetService<DistributedApplicationModel>();
        if (appModel is null)
        {
            logger.LogWarning("Application model not found.");
            throw new InvalidOperationException("Application model not found.");
        }

        // Find the dashboard resource
        var dashboardResource = appModel.Resources.FirstOrDefault(r => 
            string.Equals(r.Name, KnownResourceNames.AspireDashboard, StringComparisons.ResourceName)) as IResourceWithEndpoints;

        if (dashboardResource is null)
        {
            logger.LogWarning("Dashboard resource not found in application model.");
            throw new InvalidOperationException("Dashboard is not enabled.");
        }

        // Get the MCP endpoint from the dashboard resource
        var mcpEndpoint = dashboardResource.GetEndpoint(McpEndpointName);
        if (!mcpEndpoint.Exists)
        {
            // Fallback to the frontend endpoint (http/https) as done in DashboardEventHandlers
            mcpEndpoint = dashboardResource.GetEndpoint("https");
            if (!mcpEndpoint.Exists)
            {
                mcpEndpoint = dashboardResource.GetEndpoint("http");
            }
        }

        if (!mcpEndpoint.Exists || string.IsNullOrEmpty(mcpEndpoint.Url))
        {
            logger.LogWarning("Dashboard MCP endpoint not found or not allocated.");
            throw new InvalidOperationException("Dashboard MCP endpoint URL is not available.");
        }

        // Get the API key from dashboard options
        var dashboardOptions = serviceProvider.GetService<IOptions<DashboardOptions>>();
        var mcpApiKey = dashboardOptions?.Value.McpApiKey;
        
        if (string.IsNullOrEmpty(mcpApiKey))
        {
            logger.LogWarning("Dashboard MCP API key is not available.");
            throw new InvalidOperationException("Dashboard MCP API key is not available.");
        }

        return Task.FromResult(new McpConnectionInfo
        {
            EndpointUrl = $"{mcpEndpoint.Url}/mcp",
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
