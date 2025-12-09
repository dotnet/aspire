// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// RPC target for the auxiliary backchannel that provides MCP-related operations.
/// </summary>
internal sealed class AuxiliaryBackchannelRpcTarget(
    ILogger<AuxiliaryBackchannelRpcTarget> logger,
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime)
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
    /// Gets the test results by waiting for all test resources to complete.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when all test resources have reached a completed state.</returns>
    public async Task<TestResults> GetTestResultsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(15000, cancellationToken).ConfigureAwait(false);

        var appModel = serviceProvider.GetService<DistributedApplicationModel>();
        if (appModel is null)
        {
            logger.LogWarning("Application model not found.");
            return new TestResults { Success = false, Message = "Application model not found." };
        }

        var resourceNotificationService = serviceProvider.GetService<ResourceNotificationService>();
        if (resourceNotificationService is null)
        {
            logger.LogWarning("ResourceNotificationService not found.");
            return new TestResults { Success = false, Message = "ResourceNotificationService not found." };
        }

        // Find all resources with TestResourceAnnotation
        var testResources = appModel.Resources
            .Where(r => r.Annotations.OfType<TestResourceAnnotation>().Any())
            .ToList();

        if (testResources.Count == 0)
        {
            logger.LogInformation("No test resources found in the application model.");
            return new TestResults { Success = true, Message = "No test resources found." };
        }

        logger.LogInformation("Waiting for {Count} test resource(s) to complete", testResources.Count);

        // Wait for all test resources to reach a completed state and collect results
        var waitTasks = testResources.Select(async resource =>
        {
            try
            {
                await resourceNotificationService.WaitForResourceAsync(
                    resource.Name,
                    KnownResourceStates.Finished,
                    cancellationToken).ConfigureAwait(false);
                
                logger.LogInformation("Test resource '{ResourceName}' completed", resource.Name);

                // Invoke the test results callback if present
                var callbackAnnotation = resource.Annotations.OfType<TestResultsCallbackAnnotation>().FirstOrDefault();
                TestResultFileInfo[]? resultFiles = null;

                if (callbackAnnotation is not null)
                {
                    var context = new TestResultsCallbackContext(serviceProvider, cancellationToken);
                    await callbackAnnotation.Callback(context).ConfigureAwait(false);

                    resultFiles = context.ResultFiles
                        .Select(f => new TestResultFileInfo
                        {
                            FilePath = f.File.FullName,
                            Format = f.Format.ToString()
                        })
                        .ToArray();

                    logger.LogInformation("Collected {Count} result file(s) from test resource '{ResourceName}'", resultFiles.Length, resource.Name);
                }

                return (resource.Name, Success: true, Error: (string?)null, ResultFiles: resultFiles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error waiting for test resource '{ResourceName}'", resource.Name);
                return (resource.Name, Success: false, Error: ex.Message, ResultFiles: (TestResultFileInfo[]?)null);
            }
        });

        var results = await Task.WhenAll(waitTasks).ConfigureAwait(false);

        var allSuccessful = results.All(r => r.Success);
        var message = allSuccessful 
            ? $"All {testResources.Count} test resource(s) completed successfully."
            : $"Some test resources failed: {string.Join(", ", results.Where(r => !r.Success).Select(r => r.Name))}";

        return new TestResults 
        { 
            Success = allSuccessful, 
            Message = message,
            TestResourceResults = results.Select(r => new TestResourceResult
            {
                ResourceName = r.Name,
                Success = r.Success,
                Error = r.Error,
                ResultFiles = r.ResultFiles
            }).ToArray()
        };
    }

    /// <summary>
    /// Initiates an orderly shutdown of the AppHost.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes immediately. The actual shutdown occurs after the RPC channel disconnects.</returns>
    public Task StopAppHostAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Unused - shutdown is intentionally not cancellable once requested
        
        logger.LogInformation("Received request to stop AppHost. Scheduling shutdown after RPC disconnect.");

        // Fire off a background task that waits for the RPC to disconnect, then stops the application
        _ = Task.Run(async () =>
        {
            try
            {
                // Give the RPC response time to be sent back
                await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
                
                logger.LogInformation("Stopping AppHost application.");
                hostApplicationLifetime.StopApplication();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping AppHost");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}

