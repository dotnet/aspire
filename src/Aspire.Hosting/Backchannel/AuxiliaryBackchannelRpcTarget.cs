// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// RPC target for the auxiliary backchannel that provides MCP-related operations.
/// </summary>
internal sealed class AuxiliaryBackchannelRpcTarget(
    ILogger<AuxiliaryBackchannelRpcTarget> logger,
    IServiceProvider serviceProvider)
{
    private const string McpEndpointName = "mcp";

    /// <summary>
    /// Gets information about the AppHost for the MCP server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The AppHost information including the fully qualified path and process ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when AppHost information is not available.</exception>
    public Task<AppHostInformation> GetAppHostInformationAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Reserved for future use

        var configuration = serviceProvider.GetService<IConfiguration>();
        if (configuration is null)
        {
            logger.LogError("Configuration not found.");
            throw new InvalidOperationException("Configuration not found.");
        }

        // First try to get the file path (with extension), otherwise fall back to the path (without extension)
        var appHostPath = configuration["AppHost:FilePath"] ?? configuration["AppHost:Path"];
        if (string.IsNullOrEmpty(appHostPath))
        {
            logger.LogError("AppHost path not found in configuration.");
            throw new InvalidOperationException("AppHost path not found in configuration.");
        }

        // Get the CLI process ID if the AppHost was launched via the CLI
        int? cliProcessId = null;
        var cliPidString = configuration[KnownConfigNames.CliProcessId];
        if (!string.IsNullOrEmpty(cliPidString) && int.TryParse(cliPidString, out var parsedCliPid))
        {
            cliProcessId = parsedCliPid;
        }

        return Task.FromResult(new AppHostInformation
        {
            AppHostPath = appHostPath,
            ProcessId = Environment.ProcessId,
            CliProcessId = cliProcessId
        });
    }

    /// <summary>
    /// Gets the Dashboard MCP connection information including endpoint URL and API token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The MCP connection information, or null if the dashboard is not part of the application model.</returns>
    public async Task<DashboardMcpConnectionInfo?> GetDashboardMcpConnectionInfoAsync(CancellationToken cancellationToken = default)
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

        var endpointUrl = await mcpEndpoint.GetValueAsync(cancellationToken).ConfigureAwait(false);
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
