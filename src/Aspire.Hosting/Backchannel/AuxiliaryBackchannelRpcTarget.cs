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
    /// <returns>The MCP connection information, or null if the dashboard is not part of the application model.</returns>
    public async Task<DashboardMcpConnectionInfo?> GetDashboardMcpConnectionInfoAsync()
    {
        var appModel = serviceProvider.GetService<DistributedApplicationModel>();
        if (appModel is null)
        {
            logger.LogWarning("Application model not found.");
            return null;
        }

        // Find the dashboard resource
        var dashboardResource = appModel.Resources.FirstOrDefault(r => 
            string.Equals(r.Name, KnownResourceNames.AspireDashboard, StringComparisons.ResourceName)) as IResourceWithEndpoints;

        if (dashboardResource is null)
        {
            logger.LogDebug("Dashboard resource not found in application model.");
            return null;
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

        if (!mcpEndpoint.Exists)
        {
            logger.LogWarning("Dashboard MCP endpoint not found or not allocated.");
            return null;
        }

        var endpointUrl = await mcpEndpoint.GetValueAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(endpointUrl))
        {
            logger.LogWarning("Dashboard MCP endpoint URL is not allocated.");
            return null;
        }

        // Get the API key from dashboard options
        var dashboardOptions = serviceProvider.GetService<IOptions<DashboardOptions>>();
        var mcpApiKey = dashboardOptions?.Value.McpApiKey;
        
        if (string.IsNullOrEmpty(mcpApiKey))
        {
            logger.LogWarning("Dashboard MCP API key is not available.");
            return null;
        }

        return new DashboardMcpConnectionInfo
        {
            EndpointUrl = $"{endpointUrl}/mcp",
            ApiToken = mcpApiKey
        };
    }
}

/// <summary>
/// Represents the connection information for the Dashboard MCP server.
/// </summary>
internal sealed class DashboardMcpConnectionInfo
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
