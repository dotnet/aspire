// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Shared.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Commands;

internal sealed class McpStartCommand : BaseCommand
{
    private readonly Dictionary<string, CliMcpTool> _cliTools;
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly CliExecutionContext _executionContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpStartCommand> _logger;

    private McpServer? _mcpServer;

    // Persistent MCP client for listening to tool list changes
    private McpClient? _notificationClient;
    private IAsyncDisposable? _toolListChangedHandler;

    public McpStartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, ILoggerFactory loggerFactory, ILogger<McpStartCommand> logger, IPackagingService packagingService)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _executionContext = executionContext;
        _loggerFactory = loggerFactory;
        _logger = logger;
        // Only CLI-specific tools are hardcoded; AppHost tools are fetched dynamically
        _cliTools = new Dictionary<string, CliMcpTool>
        {
            ["select_apphost"] = new SelectAppHostTool(auxiliaryBackchannelMonitor, executionContext),
            ["list_apphosts"] = new ListAppHostsTool(auxiliaryBackchannelMonitor, executionContext),
            ["list_integrations"] = new ListIntegrationsTool(packagingService, executionContext, auxiliaryBackchannelMonitor),
            ["get_integration_docs"] = new GetIntegrationDocsTool()
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
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability
                {
                    // Indicate that this server supports tools/list_changed notifications
                    ListChanged = true
                }
            },
            Handlers = new McpServerHandlers()
            {
                ListToolsHandler = HandleListToolsAsync,
                CallToolHandler = HandleCallToolAsync
            },
        };

        await using var server = McpServer.Create(new StdioServerTransport("aspire-mcp-server"), options);
        _mcpServer = server;

        // Subscribe to AppHost selection changes to notify clients
        _auxiliaryBackchannelMonitor.SelectedAppHostChanged += OnSelectedAppHostChanged;

        try
        {
            await server.RunAsync(cancellationToken);
        }
        finally
        {
            _auxiliaryBackchannelMonitor.SelectedAppHostChanged -= OnSelectedAppHostChanged;
        }

        // Dispose notification resources
        if (_toolListChangedHandler is not null)
        {
            await _toolListChangedHandler.DisposeAsync();
        }
        if (_notificationClient is not null)
        {
            await _notificationClient.DisposeAsync();
        }

        return ExitCodeConstants.Success;
    }

    /// <summary>
    /// Called when the selected AppHost changes. Invalidates the cache and notifies clients.
    /// </summary>
    private void OnSelectedAppHostChanged()
    {
        _logger.LogDebug("Selected AppHost changed, notifying clients");

        // Notify clients that the tool list has changed
        if (_mcpServer is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _mcpServer.SendMessageAsync(
                        new JsonRpcNotification
                        {
                            Method = NotificationMethods.ToolListChangedNotification
                        },
                        CancellationToken.None);
                    _logger.LogInformation("Sent tool list changed notification after AppHost selection change");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send tool list changed notification");
                }
            });
        }
    }

    private async ValueTask<ListToolsResult> HandleListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP ListTools request received");

        var tools = new List<Tool>();

        // Always add CLI-specific tools
        foreach (var cliTool in _cliTools.Values)
        {
            tools.Add(new Tool
            {
                Name = cliTool.Name,
                Description = cliTool.Description,
                InputSchema = cliTool.GetInputSchema()
            });
        }

        // Try to get tools from the selected AppHost
        var appHostTools = await GetAppHostToolsAsync(cancellationToken);
        if (appHostTools is not null)
        {
            tools.AddRange(appHostTools);
        }

        _logger.LogDebug("Returning {ToolCount} tools: {ToolNames}", tools.Count, string.Join(", ", tools.Select(t => t.Name)));

        return new ListToolsResult
        {
            Tools = tools
        };
    }

    /// <summary>
    /// Gets tools from the currently selected AppHost, using cache when possible.
    /// </summary>
    private async ValueTask<IList<Tool>?> GetAppHostToolsAsync(CancellationToken cancellationToken)
    {
        var connection = TryGetSelectedConnection();
        if (connection is null || connection.McpInfo is null)
        {
            return null;
        }

        var currentAppHostPath = connection.AppHostInfo?.AppHostPath;

        // Fetch tools from the AppHost's MCP server
        try
        {
            _logger.LogDebug("Fetching tools from AppHost: {AppHostPath}", currentAppHostPath);

            await using var mcpClient = await CreateMcpClientAsync(connection, cancellationToken);
            var clientTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

            // Convert McpClientTool to Tool
            var tools = clientTools.Select(t => t.ProtocolTool).ToList();

            // Subscribe to tool list changes from the AppHost
            await SubscribeToToolListChangesAsync(connection, cancellationToken);

            _logger.LogDebug("Fetched {ToolCount} tools from AppHost: {ToolNames}", tools.Count, string.Join(", ", tools.Select(t => t.Name)));
            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch tools from AppHost: {AppHostPath}", currentAppHostPath);
            return null;
        }
    }

    /// <summary>
    /// Subscribes to tool list changes from the AppHost and forwards them to clients.
    /// </summary>
    private async Task SubscribeToToolListChangesAsync(AppHostConnection connection, CancellationToken cancellationToken)
    {
        // Dispose previous resources if any
        if (_toolListChangedHandler is not null)
        {
            await _toolListChangedHandler.DisposeAsync();
            _toolListChangedHandler = null;
        }
        if (_notificationClient is not null)
        {
            await _notificationClient.DisposeAsync();
            _notificationClient = null;
        }

        if (connection.McpInfo is null)
        {
            return;
        }

        try
        {
            // Create a persistent MCP client for receiving notifications
            _notificationClient = await CreateMcpClientAsync(connection, cancellationToken);

            // Register handler for tool list changes
            _toolListChangedHandler = _notificationClient.RegisterNotificationHandler(
                NotificationMethods.ToolListChangedNotification,
                async (notification, ct) =>
                {
                    _logger.LogDebug("Received tool list changed notification from AppHost");

                    // Forward the notification to our clients
                    if (_mcpServer is not null)
                    {
                        try
                        {
                            await _mcpServer.SendMessageAsync(
                                new JsonRpcNotification
                                {
                                    Method = NotificationMethods.ToolListChangedNotification
                                },
                                ct);
                            _logger.LogDebug("Forwarded tool list changed notification to clients");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to forward tool list changed notification");
                        }
                    }
                });

            _logger.LogDebug("Subscribed to tool list changes from AppHost: {AppHostPath}", connection.AppHostInfo?.AppHostPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to subscribe to tool list changes from AppHost");
        }
    }

    /// <summary>
    /// Creates an MCP client to communicate with the AppHost's dashboard MCP server.
    /// </summary>
    private async Task<McpClient> CreateMcpClientAsync(AppHostConnection connection, CancellationToken cancellationToken)
    {
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(connection.McpInfo!.EndpointUrl),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["x-mcp-api-key"] = connection.McpInfo.ApiToken
            }
        };

        var httpClient = new HttpClient();
        var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    private async ValueTask<CallToolResult> HandleCallToolAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        var toolName = request.Params?.Name ?? string.Empty;

        _logger.LogInformation("MCP CallTool request received for tool: {ToolName}", toolName);

        // Handle CLI-specific tools - these don't need an MCP connection to the AppHost
        if (_cliTools.TryGetValue(toolName, out var cliTool))
        {
            return await cliTool.CallToolAsync(null!, request.Params?.Arguments, cancellationToken);
        }

        // For all other tools, forward to the AppHost's MCP server
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
            "Sending tool command to dashboard MCP server: {ToolName} " +
            "Dashboard URL: {EndpointUrl}, " +
            "AppHost Path: {AppHostPath}, " +
            "AppHost PID: {AppHostPid}, " +
            "CLI PID: {CliPid}",
            toolName,
            connection.McpInfo.EndpointUrl,
            connection.AppHostInfo?.AppHostPath ?? "N/A",
            connection.AppHostInfo?.ProcessId.ToString(CultureInfo.InvariantCulture) ?? "N/A",
            connection.AppHostInfo?.CliProcessId?.ToString(CultureInfo.InvariantCulture) ?? "N/A");

        // Forward the tool call to the AppHost's MCP server
        try
        {
            await using var mcpClient = await CreateMcpClientAsync(connection, cancellationToken);

            _logger.LogDebug("Calling tool {ToolName} on dashboard MCP server", toolName);

            // Convert JsonElement arguments to Dictionary<string, object?>
            Dictionary<string, object?>? convertedArgs = null;
            if (request.Params?.Arguments is not null)
            {
                convertedArgs = new Dictionary<string, object?>();
                foreach (var kvp in request.Params.Arguments)
                {
                    convertedArgs[kvp.Key] = kvp.Value.ValueKind == JsonValueKind.Null ? null : kvp.Value;
                }
            }

            var result = await mcpClient.CallToolAsync(
                toolName,
                convertedArgs,
                serializerOptions: McpJsonUtilities.DefaultOptions,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Tool {ToolName} completed successfully", toolName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling tool {ToolName}", toolName);
            throw;
        }
    }

    /// <summary>
    /// Tries to get the appropriate AppHost connection without throwing exceptions.
    /// Returns null if no suitable connection is found.
    /// </summary>
    private AppHostConnection? TryGetSelectedConnection()
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
                c.AppHostInfo?.AppHostPath is not null &&
                string.Equals(c.AppHostInfo.AppHostPath, selectedPath, StringComparison.OrdinalIgnoreCase));

            if (selectedConnection is not null)
            {
                return selectedConnection;
            }

            // Clear the selection since the AppHost is no longer available
            _auxiliaryBackchannelMonitor.SelectedAppHostPath = null;
        }

        // Get in-scope connections
        var inScopeConnections = connections.Where(c => c.IsInScope).ToList();

        if (inScopeConnections.Count == 1)
        {
            return inScopeConnections[0];
        }

        // Multiple or no in-scope connections - return null
        return null;
    }

    /// <summary>
    /// Gets the appropriate AppHost connection based on the selection logic:
    /// 1. If a specific AppHost is selected via select_apphost, use that
    /// 2. Otherwise, look for in-scope connections (AppHosts within the working directory)
    /// 3. If exactly one in-scope connection exists, use it
    /// 4. If multiple in-scope connections exist, throw an error listing them
    /// 5. If no in-scope connections exist, fall back to the first available connection
    /// </summary>
    private AppHostConnection? GetSelectedConnection()
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
        else
        {
            _logger.LogDebug("No in-scope AppHosts found in scope: {WorkingDirectory}", _executionContext.WorkingDirectory);
            throw new McpProtocolException(

                $"No Aspire AppHosts are running in the scope of the MCP server's working directory: {_executionContext.WorkingDirectory}");
        }
    }
}
