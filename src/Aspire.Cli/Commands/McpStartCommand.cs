// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;
using Aspire.Shared.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Commands;

internal sealed class McpStartCommand : BaseCommand
{
    private readonly Dictionary<string, CliMcpTool> _knownTools;
    private string? _selectedAppHostPath;
    private Dictionary<string, (string ResourceName, Tool Tool)>? _resourceToolMap;
    private McpServer? _server;
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly CliExecutionContext _executionContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpStartCommand> _logger;

    public McpStartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, ILoggerFactory loggerFactory, ILogger<McpStartCommand> logger, IPackagingService packagingService, IEnvironmentChecker environmentChecker)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _executionContext = executionContext;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _knownTools = new Dictionary<string, CliMcpTool>
        {
            [KnownMcpTools.ListResources] = new ListResourcesTool(),
            [KnownMcpTools.ListConsoleLogs] = new ListConsoleLogsTool(),
            [KnownMcpTools.ExecuteResourceCommand] = new ExecuteResourceCommandTool(),
            [KnownMcpTools.ListStructuredLogs] = new ListStructuredLogsTool(),
            [KnownMcpTools.ListTraces] = new ListTracesTool(),
            [KnownMcpTools.ListTraceStructuredLogs] = new ListTraceStructuredLogsTool(),
            [KnownMcpTools.SelectAppHost] = new SelectAppHostTool(auxiliaryBackchannelMonitor, executionContext),
            [KnownMcpTools.ListAppHosts] = new ListAppHostsTool(auxiliaryBackchannelMonitor, executionContext),
            [KnownMcpTools.ListIntegrations] = new ListIntegrationsTool(packagingService, executionContext, auxiliaryBackchannelMonitor),
            [KnownMcpTools.GetIntegrationDocs] = new GetIntegrationDocsTool(),
            [KnownMcpTools.Doctor] = new DoctorTool(environmentChecker),
            [KnownMcpTools.RefreshTools] = new RefreshToolsTool(RefreshResourceToolMapAsync, SendToolsListChangedNotificationAsync)
        };
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var icons = McpIconHelper.GetAspireIcons(typeof(McpStartCommand).Assembly, "Aspire.Cli.Mcp.Resources");

        var options = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = "aspire-mcp-server",
                Version = VersionHelper.GetDefaultTemplateVersion(),
                Icons = icons
            },
            Handlers = new McpServerHandlers()
            {
                ListToolsHandler = HandleListToolsAsync,
                CallToolHandler = HandleCallToolAsync
            },
        };

        await using var server = McpServer.Create(new StdioServerTransport("aspire-mcp-server"), options);

        // Keep a reference to the server for sending notifications
        _server = server;

        // Starts the MCP server, it's blocking until cancellation is requested
        await server.RunAsync(cancellationToken);

        // Clear the server reference on exit
        _server = null;

        return ExitCodeConstants.Success;
    }

    private async ValueTask<ListToolsResult> HandleListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        _ = request;

        _logger.LogDebug("MCP ListTools request received");

        var tools = new List<Tool>();

        tools.AddRange(_knownTools.Values.Select(tool => new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.GetInputSchema()
        }));

        try
        {
            // Detect if the tools list should be refreshed due to AppHost selection change
            if (_resourceToolMap is null || _selectedAppHostPath != _auxiliaryBackchannelMonitor.SelectedAppHostPath)
            {
                await RefreshResourceToolMapAsync(cancellationToken);
                await SendToolsListChangedNotificationAsync(cancellationToken).ConfigureAwait(false);
                _selectedAppHostPath = _auxiliaryBackchannelMonitor.SelectedAppHostPath;
            }

            tools.AddRange(_resourceToolMap.Select(x => new Tool
            {
                Name = x.Key,
                Description = x.Value.Tool.Description,
                InputSchema = x.Value.Tool.InputSchema
            }));
        }
        catch (Exception ex)
        {
            // Don't fail ListTools if resource discovery fails; still return CLI tools.
            _logger.LogDebug(ex, "Failed to aggregate resource MCP tools");
        }

        _logger.LogDebug("Returning {ToolCount} tools", tools.Count);

        return new ListToolsResult { Tools = [.. tools] };
    }

    private async ValueTask<CallToolResult> HandleCallToolAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        var toolName = request.Params?.Name ?? string.Empty;

        _logger.LogDebug("MCP CallTool request received for tool: {ToolName}", toolName);

        // Known tools?
        if (_knownTools.TryGetValue(toolName, out var tool))
        {
            // Handle tools that don't need an MCP connection to the AppHost
            if (KnownMcpTools.IsLocalTool(toolName))
            {
                var args = request.Params?.Arguments as IReadOnlyDictionary<string, JsonElement>;
                return await tool.CallToolAsync(null!, args, cancellationToken).ConfigureAwait(false);
            }

            if (KnownMcpTools.IsDashboardTool(toolName))
            {
                var args = request.Params?.Arguments as IReadOnlyDictionary<string, JsonElement>;
                return await CallDashboardToolAsync(toolName, tool, args, cancellationToken).ConfigureAwait(false);
            }

            // If a tool is registered in _tools, it must be classified as either local or dashboard-backed.
            throw new McpProtocolException(
                $"Tool '{toolName}' is not classified as local or dashboard-backed.",
                McpErrorCode.InternalError);
        }

        var toolsRefreshed = false;

        // Detect if the tools list should be refreshed due to AppHost selection change
        if (_resourceToolMap is null || _selectedAppHostPath != _auxiliaryBackchannelMonitor.SelectedAppHostPath)
        {
            await RefreshResourceToolMapAsync(cancellationToken);
            _selectedAppHostPath = _auxiliaryBackchannelMonitor.SelectedAppHostPath;
            toolsRefreshed = true;
            await SendToolsListChangedNotificationAsync(cancellationToken).ConfigureAwait(false);
        }

        // Resource MCP tools are invoked via the AppHost backchannel (AppHost proxies to the resource MCP endpoint).
        if (_resourceToolMap.TryGetValue(toolName, out var resourceAndTool))
        {
            var connection = GetSelectedConnection();
            if (connection == null)
            {
                throw new McpProtocolException(
                    "No Aspire AppHost is currently running. To use resource MCP tools, start an Aspire application (e.g. 'aspire run') and then retry.",
                    McpErrorCode.InternalError);
            }

            var args = request.Params?.Arguments as IReadOnlyDictionary<string, JsonElement>;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Invoking tool {Name} with arguments {Arguments}", toolName, JsonSerializer.Serialize(args, BackchannelJsonSerializerContext.Default.DictionaryStringJsonElement));
            }

            var result = await connection.CallResourceMcpToolAsync(resourceAndTool.ResourceName, resourceAndTool.Tool.Name, args, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                throw new McpProtocolException($"Failed to get MCP tool result for '{toolName}'. Try refreshing the tools with 'refresh_tools'.", McpErrorCode.InternalError);
            }

            return result;
        }

        _logger.LogWarning("Unknown tool requested: {ToolName}", toolName);

        // If we haven't refreshed yet, try refreshing once more in case the resource list changed
        if (!toolsRefreshed)
        {
            _resourceToolMap = null;
            return await HandleCallToolAsync(request, cancellationToken).ConfigureAwait(false);
        }

        throw new McpProtocolException($"Unknown tool: '{toolName}'", McpErrorCode.MethodNotFound);
    }

    private async ValueTask<CallToolResult> CallDashboardToolAsync(
        string toolName,
        CliMcpTool tool,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken)
    {
        var connection = GetSelectedConnection();
        if (connection is null)
        {
            _logger.LogWarning("No Aspire AppHost is currently running");
            throw new McpProtocolException(
                "No Aspire AppHost is currently running. " +
                "To use Aspire MCP tools, you must first start an Aspire application by running 'aspire run' in your AppHost project directory. " +
                "Once the application is running, the MCP tools will be able to connect to the dashboard and execute commands.",
                McpErrorCode.InternalError);
        }

        if (connection.McpInfo is null)
        {
            _logger.LogWarning("Dashboard is not available in the running AppHost");
            throw new McpProtocolException(
                "The Aspire Dashboard is not available in the running AppHost. " +
                "The dashboard must be enabled to use MCP tools. " +
                "Ensure your AppHost is configured with the dashboard enabled (this is the default configuration).",
                McpErrorCode.InternalError);
        }

        _logger.LogInformation(
            "Connecting to dashboard MCP server. " +
            "Dashboard URL: {EndpointUrl}, " +
            "AppHost Path: {AppHostPath}, " +
            "AppHost PID: {AppHostPid}, " +
            "CLI PID: {CliPid}",
            connection.McpInfo.EndpointUrl,
            connection.AppHostInfo?.AppHostPath ?? "N/A",
            connection.AppHostInfo?.ProcessId.ToString(CultureInfo.InvariantCulture) ?? "N/A",
            connection.AppHostInfo?.CliProcessId?.ToString(CultureInfo.InvariantCulture) ?? "N/A");

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(connection.McpInfo.EndpointUrl),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["x-mcp-api-key"] = connection.McpInfo.ApiToken
            }
        };

        using var httpClient = new HttpClient();
        await using var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

        // Create MCP client to communicate with the dashboard
        await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

        _logger.LogDebug("Calling tool {ToolName} on dashboard MCP server", toolName);

        try
        {
            _logger.LogDebug("Invoking CallToolAsync for tool {ToolName} with arguments: {Arguments}", toolName, arguments);
            var result = await tool.CallToolAsync(mcpClient, arguments, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Tool {ToolName} completed successfully", toolName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling tool {ToolName}", toolName);
            throw;
        }
    }

    private Task SendToolsListChangedNotificationAsync(CancellationToken cancellationToken)
    {
        var server = _server;
        if (server is null)
        {
            throw new InvalidOperationException("MCP server is not running.");
        }

        return server.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, cancellationToken);
    }

    [MemberNotNull(nameof(_resourceToolMap))]
    private async Task<int> RefreshResourceToolMapAsync(CancellationToken cancellationToken)
    {
        var refreshedMap = new Dictionary<string, (string, Tool)>(StringComparer.Ordinal);

        try
        {
            var connection = GetSelectedConnection();

            if (connection is not null)
            {
                // Collect initial snapshots from the stream
                // The stream yields initial snapshots for all resources first
                var resourcesWithTools = new List<ResourceSnapshot>();
                var seenResources = new HashSet<string>(StringComparer.Ordinal);

                await foreach (var snapshot in connection.WatchResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Stop after we've seen all resources once (initial batch)
                    if (!seenResources.Add(snapshot.Name))
                    {
                        break;
                    }

                    if (snapshot.McpServer is not null)
                    {
                        resourcesWithTools.Add(snapshot);
                    }
                }

                _logger.LogDebug("Resources with MCP tools received: {Count}", resourcesWithTools.Count);

                foreach (var resource in resourcesWithTools)
                {
                    if (resource.McpServer is null)
                    {
                        continue;
                    }

                    foreach (var tool in resource.McpServer.Tools)
                    {
                        var exposedName = $"{resource.Name.Replace("-", "_")}_{tool.Name}";
                        refreshedMap[exposedName] = (resource.Name, tool);

                        _logger.LogDebug("{Tool}: {Description}", exposedName, tool.Description);
                    }
                }
            }

        }
        catch (Exception ex)
        {
            // Don't fail refresh_tools if resource discovery fails; still emit notification.
            _logger.LogDebug(ex, "Failed to refresh resource MCP tool routing map");
        }
        finally
        {
            // Ensure _resourceToolMap is always non-null when exiting, even if connection is null or an exception occurs.
            _resourceToolMap = refreshedMap;
        }

        return _resourceToolMap.Count + _knownTools.Count;
    }

    /// <summary>
    /// Gets the appropriate AppHost connection based on the selection logic:
    /// 1. If a specific AppHost is selected via select_apphost, use that
    /// 2. Otherwise, look for in-scope connections (AppHosts within the working directory)
    /// 3. If exactly one in-scope connection exists, use it
    /// 4. If multiple in-scope connections exist, throw an error listing them
    /// 5. If no in-scope connections exist, fall back to the first available connection
    /// </summary>
    private AppHostAuxiliaryBackchannel? GetSelectedConnection()
    {
        var connections = _auxiliaryBackchannelMonitor.Connections.Values.ToList();

        if (connections.Count == 0)
        {
            return null;
        }

        // Check if a specific AppHost was selected
        var selectedPath = _auxiliaryBackchannelMonitor.SelectedAppHostPath;
        if (!string.IsNullOrEmpty(selectedPath))
        {
            var selectedConnection = connections.FirstOrDefault(c =>
                c.AppHostInfo?.AppHostPath != null &&
                string.Equals(c.AppHostInfo.AppHostPath, selectedPath, StringComparison.OrdinalIgnoreCase));

            if (selectedConnection != null)
            {
                _logger.LogDebug("Using explicitly selected AppHost: {AppHostPath}", selectedPath);
                return selectedConnection;
            }

            _logger.LogWarning("Selected AppHost at '{SelectedPath}' is no longer running, falling back to selection logic", selectedPath);
            // Clear the selection since the AppHost is no longer available
            _auxiliaryBackchannelMonitor.SelectedAppHostPath = null;
        }

        // Get in-scope connections
        var inScopeConnections = connections.Where(c => c.IsInScope).ToList();

        if (inScopeConnections.Count == 1)
        {
            _logger.LogDebug("Using single in-scope AppHost: {AppHostPath}", inScopeConnections[0].AppHostInfo?.AppHostPath ?? "N/A");
            return inScopeConnections[0];
        }

        if (inScopeConnections.Count > 1)
        {
            var paths = inScopeConnections
                .Where(c => c.AppHostInfo?.AppHostPath != null)
                .Select(c => c.AppHostInfo!.AppHostPath)
                .ToList();

            var pathsList = string.Join("\n", paths.Select(p => $"  - {p}"));

            throw new McpProtocolException(
                $"Multiple Aspire AppHosts are running in the scope of the MCP server's working directory. " +
                $"Use the 'select_apphost' tool to specify which AppHost to use.\n\nRunning AppHosts:\n{pathsList}",
                McpErrorCode.InternalError);
        }

        var fallback = connections
            .OrderBy(c => c.AppHostInfo?.AppHostPath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.AppHostInfo?.ProcessId ?? int.MaxValue)
            .FirstOrDefault();

        _logger.LogDebug(
            "No in-scope AppHosts found for working directory {WorkingDirectory}. Falling back to first available AppHost: {AppHostPath}",
            _executionContext.WorkingDirectory,
            fallback?.AppHostInfo?.AppHostPath ?? "N/A");

        return fallback;
    }
}
