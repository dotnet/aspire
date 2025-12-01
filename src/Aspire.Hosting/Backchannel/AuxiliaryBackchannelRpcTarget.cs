// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
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
        // The cancellationToken parameter is not currently used, but is retained for API consistency and potential future support for cancellation.
        _ = cancellationToken;

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

    /// <summary>
    /// Gets documentation for a specific resource in the AppHost.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The documentation text for the resource, or null if the resource doesn't exist.</returns>
    public async Task<string?> GetResourceDocsAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        var appModel = serviceProvider.GetService<DistributedApplicationModel>();
        if (appModel is null)
        {
            logger.LogWarning("Application model not found.");
            return null;
        }

        // Find the resource by name
        var resource = appModel.Resources.FirstOrDefault(r => 
            string.Equals(r.Name, resourceName, StringComparisons.ResourceName));

        if (resource is null)
        {
            logger.LogDebug("Resource '{ResourceName}' not found in application model.", resourceName);
            return null;
        }

        // Check if the resource has an AgentContentAnnotation
#pragma warning disable ASPIREAGENTCONTENT001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        if (resource.TryGetLastAnnotation<AgentContentAnnotation>(out var annotation))
        {
            try
            {
                // Create the context and invoke the callback
                var context = new AgentContentContext(resource, serviceProvider, cancellationToken);
                await annotation.Callback(context).ConfigureAwait(false);
                return context.GetContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting agent content for resource '{ResourceName}'.", resourceName);
                // Fall through to generate generic documentation
            }
        }
#pragma warning restore ASPIREAGENTCONTENT001

        // Generate generic documentation based on the resource type's assembly
        return GenerateGenericResourceDocs(resource);
    }

    private static string GenerateGenericResourceDocs(IResource resource)
    {
        var resourceType = resource.GetType();
        var assembly = resourceType.Assembly;
        var assemblyName = assembly.GetName().Name;

        // Get the version from AssemblyInformationalVersionAttribute (which contains the NuGet package version)
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Strip the commit hash suffix if present (e.g., "9.0.0+abc123" -> "9.0.0")
        if (version is not null)
        {
            var plusIndex = version.IndexOf('+');
            if (plusIndex > 0)
            {
                version = version[..plusIndex];
            }
        }

        // Fall back to assembly version if informational version is not available
        version ??= assembly.GetName().Version?.ToString();

        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(resourceType.Name);
        sb.AppendLine();
        sb.Append("This is a `").Append(resourceType.Name).Append("` resource from the `").Append(assemblyName).AppendLine("` package.");
        sb.AppendLine();
        sb.AppendLine("## Documentation");
        sb.AppendLine();
        sb.AppendLine("For detailed documentation on how to configure and use this resource, including connection patterns, configuration options, and best practices, see the package README:");
        sb.AppendLine();

        if (assemblyName is not null && version is not null)
        {
            sb.Append("https://www.nuget.org/packages/").Append(assemblyName).Append('/').Append(version).AppendLine("#readme-body-tab");
        }
        else if (assemblyName is not null)
        {
            sb.Append("https://www.nuget.org/packages/").Append(assemblyName).AppendLine("#readme-body-tab");
        }

        sb.AppendLine();
        sb.AppendLine("## Note");
        sb.AppendLine();
        sb.AppendLine("This resource does not have custom agent documentation configured. The resource author can add detailed guidance for AI agents by using the `WithAgentContent` API.");

        return sb.ToString();
    }
}
