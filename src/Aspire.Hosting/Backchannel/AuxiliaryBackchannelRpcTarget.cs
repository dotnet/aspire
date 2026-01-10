// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// RPC target for the auxiliary backchannel that provides MCP-related operations.
/// </summary>
internal sealed class AuxiliaryBackchannelRpcTarget(
    ILogger<AuxiliaryBackchannelRpcTarget> logger,
    IServiceProvider serviceProvider)
{
    private const string McpEndpointName = "mcp";
    private static readonly TimeSpan s_mcpDiscoveryTimeout = TimeSpan.FromSeconds(5);

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
        if (appModel.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is not IResourceWithEndpoints dashboardResource)
        {
            logger.LogDebug("Dashboard resource not found in application model.");
            return null;
        }

        var mcpEndpoint = dashboardResource.GetEndpoint(McpEndpointName);
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
    /// Watches for resource snapshot changes and streams them to the client.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of resource snapshots as they change.</returns>
    public async IAsyncEnumerable<ResourceSnapshot> WatchResourceSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var notificationService = serviceProvider.GetRequiredService<ResourceNotificationService>();

        var resourceEvents = notificationService.WatchAsync(cancellationToken);

        await foreach (var resourceEvent in resourceEvents.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Skip the dashboard resource
            if (StringComparers.ResourceName.Equals(resourceEvent.Resource.Name, KnownResourceNames.AspireDashboard))
            {
                continue;
            }

            var snapshot = await CreateResourceSnapshotFromEventAsync(resourceEvent, cancellationToken).ConfigureAwait(false);
            if (snapshot is not null)
            {
                yield return snapshot;
            }
        }
    }

    private async Task<ResourceSnapshot?> CreateResourceSnapshotFromEventAsync(
        ResourceEvent resourceEvent,
        CancellationToken cancellationToken)
    {
        var resource = resourceEvent.Resource;
        var customSnapshot = resourceEvent.Snapshot;

        // Get MCP server info if available
        ResourceSnapshotMcpServer? mcpServer = null;
        if (resource is IResourceWithEndpoints resourceWithEndpoints &&
            resourceWithEndpoints.TryGetLastAnnotation<McpServerEndpointAnnotation>(out var mcpAnnotation))
        {
            var endpointUri = await mcpAnnotation.EndpointUrlResolver(resourceWithEndpoints, cancellationToken).ConfigureAwait(false);
            if (endpointUri is not null)
            {
                var tools = await TryListToolsAsync(endpointUri, cancellationToken).ConfigureAwait(false);
                if (tools is not null)
                {
                    mcpServer = new ResourceSnapshotMcpServer
                    {
                        EndpointUrl = endpointUri.ToString(),
                        Tools = tools
                    };
                }
            }
        }

        return new ResourceSnapshot
        {
            Name = resource.Name,
            Type = customSnapshot.ResourceType,
            State = customSnapshot.State?.Text,
            McpServer = mcpServer
        };
    }

    /// <summary>
    /// Invokes a tool on the MCP server exposed by a resource annotated with <see cref="McpServerEndpointAnnotation"/>.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="toolName">The tool name to invoke.</param>
    /// <param name="arguments">Tool arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A JSON representation of the MCP <see cref="CallToolResult"/>.</returns>
    public async Task<CallToolResult> CallResourceMcpToolAsync(
        string resourceName,
        string toolName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var appModel = serviceProvider.GetService<DistributedApplicationModel>();
        if (appModel is null)
        {
            throw new InvalidOperationException("Application model not found.");
        }

        var resource = appModel.Resources
            .OfType<IResourceWithEndpoints>()
            .FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparisons.ResourceName));

        if (resource is null)
        {
            throw new InvalidOperationException($"Resource '{resourceName}' not found.");
        }

        if (!resource.TryGetLastAnnotation<McpServerEndpointAnnotation>(out var annotation))
        {
            throw new InvalidOperationException($"Resource '{resourceName}' does not have an MCP endpoint annotation.");
        }

        var endpointUri = await annotation.EndpointUrlResolver(resource, cancellationToken).ConfigureAwait(false);
        if (endpointUri is null)
        {
            throw new InvalidOperationException($"MCP endpoint for resource '{resourceName}' is not available.");
        }

        var transport = CreateHttpClientTransport(endpointUri);

        McpClient? mcpClient = null;
        try
        {
            mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to create MCP client.");

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Invoking tool {Name} with arguments {Arguments}", toolName, JsonSerializer.Serialize(arguments));
            }

            var result = await mcpClient.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Result: {Result}", JsonSerializer.Serialize(result));
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error invoking tool {ToolName} on resource {ResourceName}", toolName, resourceName);
            throw;
        }
        finally
        {
            if (mcpClient is not null)
            {
                await mcpClient.DisposeAsync().ConfigureAwait(false);
            }

            await transport.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Requests the AppHost to stop gracefully. The stop is initiated asynchronously in the background.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that completes immediately after initiating the stop request. The actual stop occurs asynchronously.
    /// </returns>
    public Task StopAppHostAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Unused but kept for API consistency
        logger.LogInformation("Received request to stop AppHost");

        // Start a background task to delay the stop by 500ms to allow the RPC response to be sent
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);

                // Cancel inflight RPC calls in AppHostRpcTarget before stopping
                var appHostRpcTarget = serviceProvider.GetService<AppHostRpcTarget>();
                appHostRpcTarget?.CancelInflightRpcCalls();

                var lifetime = serviceProvider.GetService<IHostApplicationLifetime>();
                if (lifetime is not null)
                {
                    logger.LogInformation("Stopping AppHost application");
                    lifetime.StopApplication();
                }
                else
                {
                    logger.LogWarning("IHostApplicationLifetime not found, cannot stop AppHost");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while stopping AppHost");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    private async Task<Tool[]?> TryListToolsAsync(Uri endpointUri, CancellationToken cancellationToken)
    {
        var transport = CreateHttpClientTransport(endpointUri);

        using var timeoutCts = new CancellationTokenSource(s_mcpDiscoveryTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: linked.Token).ConfigureAwait(false);
            try
            {
                var toolsList = await mcpClient.ListToolsAsync(cancellationToken: linked.Token).ConfigureAwait(false);

                return toolsList.Select(c => c.ProtocolTool).ToArray();
            }
            finally
            {
                await mcpClient.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to list tools from MCP endpoint {EndpointUri}", endpointUri);
            return null;
        }
        finally
        {
            await transport.DisposeAsync().ConfigureAwait(false);
        }
    }

    private HttpClientTransport CreateHttpClientTransport(Uri endpointUri)
    {
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();

        return new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = endpointUri },
            httpClient,
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            ownsHttpClient: true);
    }
}
