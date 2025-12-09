// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Hosting.ConsoleLogs;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Aspire.Dashboard.Mcp;

/// <summary>
/// MCP tools that require a resource service to be configured.
/// </summary>
internal sealed class AspireResourceMcpTools
{
    private readonly IDashboardClient _dashboardClient;
    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptions;
    private readonly ILogger<AspireResourceMcpTools> _logger;

    public AspireResourceMcpTools(IDashboardClient dashboardClient,
        IOptionsMonitor<DashboardOptions> dashboardOptions,
        ILogger<AspireResourceMcpTools> logger)
    {
        _dashboardClient = dashboardClient;
        _dashboardOptions = dashboardOptions;
        _logger = logger;
    }

    [McpServerTool(Name = "list_resources")]
    [Description("List the application resources. Includes information about their type (.NET project, container, executable), running state, source, HTTP endpoints, health status, commands, configured environment variables, and relationships.")]
    public string ListResources()
    {
        _logger.LogDebug("MCP tool list_resources called");

        try
        {
            var resources = _dashboardClient.GetResources().ToList();
            var filteredResources = GetFilteredResources(resources);

            var resourceGraphData = AIHelpers.GetResponseGraphJson(
                filteredResources,
                _dashboardOptions.CurrentValue,
                includeDashboardUrl: true,
                includeEnvironmentVariables: true,
                getResourceName: r => ResourceViewModel.GetResourceName(r, resources));

            var response = $"""
            resource_name is the identifier of resources. Use the dashboard_link when displaying resource_name. For example: [`frontend-abcxyz`](https://localhost:1234/resource?name=frontend-abcxyz)
            environment_variables is a list of environment variables configured for the resource. Environment variable values aren't provided because they could contain sensitive information.
            Console logs for a resource can provide more information about why a resource is not in a running state.

            # RESOURCE DATA

            {resourceGraphData}
            """;

            return response;
        }
        catch { }

        return "No resources found.";
    }

    private static List<ResourceViewModel> GetFilteredResources(List<ResourceViewModel> resources)
    {
        return resources.Where(r => !AIHelpers.IsResourceAIOptOut(r)).ToList();
    }

    [McpServerTool(Name = "list_console_logs")]
    [Description("List console logs for a resource. The console logs includes standard output from resources and resource commands. Known resource commands are 'resource-start', 'resource-stop' and 'resource-restart' which are used to start and stop resources. Don't print the full console logs in the response to the user. Console logs should be examined when determining why a resource isn't running.")]
    public async Task<string> ListConsoleLogsAsync(
        [Description("The resource name.")]
        string resourceName,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP tool list_console_logs called with resource '{ResourceName}'.", resourceName);

        var resources = _dashboardClient.GetResources().ToList();
        var filteredResources = GetFilteredResources(resources);

        if (AIHelpers.TryGetResource(filteredResources, resourceName, out var resource))
        {
            resourceName = resource.Name;
        }
        else
        {
            return $"Unable to find a resource named '{resourceName}'.";
        }

        var logParser = new LogParser(ConsoleColor.Black);
        var logEntries = new LogEntries(maximumEntryCount: AIHelpers.ConsoleLogsLimit) { BaseLineNumber = 1 };

        // Add a timeout for getting all console logs.
        using var subscribeConsoleLogsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        subscribeConsoleLogsCts.CancelAfter(TimeSpan.FromSeconds(20));

        try
        {
            await foreach (var entry in _dashboardClient.GetConsoleLogs(resourceName, subscribeConsoleLogsCts.Token).ConfigureAwait(false))
            {
                foreach (var logLine in entry)
                {
                    logEntries.InsertSorted(logParser.CreateLogEntry(logLine.Content, logLine.IsErrorMessage, resourceName));
                }
            }
        }
        catch (OperationCanceledException)
        {
            return $"Timeout getting console logs for `{resourceName}`";
        }

        var entries = logEntries.GetEntries().ToList();
        var totalLogsCount = entries.Count == 0 ? 0 : entries.Last().LineNumber;
        var (trimmedItems, limitMessage) = AIHelpers.GetLimitFromEndWithSummary<LogEntry>(
            entries,
            totalLogsCount,
            AIHelpers.ConsoleLogsLimit,
            "console log",
            AIHelpers.SerializeLogEntry,
            logEntry => AIHelpers.EstimateTokenCount((string)logEntry));
        var consoleLogsText = AIHelpers.SerializeConsoleLogs(trimmedItems.Cast<string>().ToList());

        var consoleLogsData = $"""
            {limitMessage}

            # CONSOLE LOGS

            ```plaintext
            {consoleLogsText.Trim()}
            ```
            """;

        return consoleLogsData;
    }

    [McpServerTool(Name = "execute_resource_command")]
    [Description("Executes a command on a resource. If a resource needs to be restarted and is currently stopped, use the start command instead.")]
    public async Task ExecuteResourceCommand([Description("The resource name")] string resourceName, [Description("The command name")] string commandName)
    {
        _logger.LogDebug("MCP tool execute_resource_command called with resource '{ResourceName}' and command '{CommandName}'.", resourceName, commandName);

        var resources = _dashboardClient.GetResources().ToList();
        var filteredResources = GetFilteredResources(resources);

        if (!AIHelpers.TryGetResource(filteredResources, resourceName, out var resource))
        {
            throw new McpProtocolException($"Resource '{resourceName}' not found.", McpErrorCode.InvalidParams);
        }

        var command = resource.Commands.FirstOrDefault(c => string.Equals(c.Name, commandName, StringComparisons.CommandName));

        if (command is null)
        {
            throw new McpProtocolException($"Command '{commandName}' not found for resource '{resourceName}'.", McpErrorCode.InvalidParams);
        }

        // Block execution when command isn't available.
        if (command.State == CommandViewModelState.Hidden)
        {
            throw new McpProtocolException($"Command '{commandName}' is not available for resource '{resourceName}'.", McpErrorCode.InvalidParams);
        }

        if (command.State == CommandViewModelState.Disabled)
        {
            if (command.Name == "resource-restart" && resource.Commands.Any(c => c.Name == "resource-start" && c.State == CommandViewModelState.Enabled))
            {
                throw new McpProtocolException($"Resource '{resourceName}' is stopped. Use the 'resource-start' command instead of 'resource-restart'.", McpErrorCode.InvalidParams);
            }

            throw new McpProtocolException($"Command '{commandName}' is currently disabled for resource '{resourceName}'.", McpErrorCode.InvalidParams);
        }

        try
        {
            var response = await _dashboardClient.ExecuteResourceCommandAsync(resource.Name, resource.ResourceType, command, CancellationToken.None).ConfigureAwait(false);

            switch (response.Kind)
            {
                case ResourceCommandResponseKind.Succeeded:
                    return;
                case ResourceCommandResponseKind.Cancelled:
                    throw new McpProtocolException($"Command '{commandName}' was cancelled.", McpErrorCode.InternalError);
                case ResourceCommandResponseKind.Failed:
                default:
                    var message = response.ErrorMessage is { Length: > 0 } ? response.ErrorMessage : "Unknown error. See logs for details.";
                    throw new McpProtocolException($"Command '{commandName}' failed for resource '{resourceName}': {message}", McpErrorCode.InternalError);
            }
        }
        catch (McpProtocolException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new McpProtocolException($"Error executing command '{commandName}' for resource '{resourceName}': {ex.Message}", McpErrorCode.InternalError);
        }
    }

    [McpServerTool(Name = "wait_for_resource_state")]
    [Description("Waits for a resource to enter a specific state. The tool will wait up to 30 seconds for the resource to reach the desired state. Valid states include: Running, Starting, Stopped, Exited, FailedToStart, Finished, Building, Waiting, Stopping, Unknown, RuntimeUnhealthy, NotStarted.")]
    public async Task<string> WaitForResourceStateAsync(
        [Description("The resource name.")]
        string resourceName,
        [Description("The desired state to wait for (e.g., 'Running', 'Stopped').")]
        string desiredState,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP tool wait_for_resource_state called with resource '{ResourceName}' and state '{DesiredState}'.", resourceName, desiredState);

        var resources = _dashboardClient.GetResources().ToList();
        var filteredResources = GetFilteredResources(resources);

        if (!AIHelpers.TryGetResource(filteredResources, resourceName, out var resource))
        {
            return $"Unable to find a resource named '{resourceName}'.";
        }

        resourceName = resource.Name;

        // Validate the desired state
        if (!Enum.TryParse<KnownResourceState>(desiredState, ignoreCase: true, out var targetState))
        {
            var validStates = string.Join(", ", Enum.GetNames<KnownResourceState>());
            return $"Invalid state '{desiredState}'. Valid states are: {validStates}";
        }

        // Check if the resource is already in the desired state
        if (resource.KnownState == targetState)
        {
            return $"Resource '{resourceName}' is already in state '{desiredState}'.";
        }

        // Subscribe to resource updates to monitor state changes
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            var subscription = await _dashboardClient.SubscribeResourcesAsync(timeoutCts.Token).ConfigureAwait(false);

            // Check initial state again in case it changed while subscribing
            var currentResource = subscription.InitialState.FirstOrDefault(r => r.Name == resourceName);
            if (currentResource?.KnownState == targetState)
            {
                return $"Resource '{resourceName}' is now in state '{desiredState}'.";
            }

            await foreach (var changes in subscription.Subscription.WithCancellation(timeoutCts.Token).ConfigureAwait(false))
            {
                foreach (var change in changes)
                {
                    if (change.Resource.Name == resourceName && change.Resource.KnownState == targetState)
                    {
                        return $"Resource '{resourceName}' is now in state '{desiredState}'.";
                    }
                }
            }

            // If we get here, the subscription ended without reaching the desired state
            return $"Resource '{resourceName}' did not reach state '{desiredState}' in time. Current state: {currentResource?.State ?? "Unknown"}. " +
                   $"Use 'list_console_logs' to examine console output for diagnostic information. " +
                   $"You can also use 'list_resources' to check the current state and 'wait_for_resource_state' again if needed.";
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred
            var currentResource = _dashboardClient.GetResource(resourceName);
            var currentState = currentResource?.State ?? "Unknown";
            
            return $"Timeout waiting for resource '{resourceName}' to reach state '{desiredState}' (waited 30 seconds). Current state: {currentState}. " +
                   $"Use 'list_console_logs' to examine console output for diagnostic information. " +
                   $"You can also use 'list_resources' to check the current state and 'wait_for_resource_state' again if needed.";
        }
    }
}
