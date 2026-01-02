// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
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
    private readonly Dictionary<string, CliMcpTool> _tools = new();
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly CliExecutionContext _executionContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpStartCommand> _logger;
    private readonly IPackagingService _packagingService;
    private readonly IEnvironmentChecker _environmentChecker;

    // Standalone Dashboard mode settings
    private string? _standaloneDashboardUrl;
    private string? _standaloneApiKey;
    private bool IsStandaloneMode => !string.IsNullOrEmpty(_standaloneDashboardUrl);

    // Command options
    private readonly Option<string?> _dashboardUrlOption;
    private readonly Option<string?> _apiKeyOption;

    public McpStartCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, ILoggerFactory loggerFactory, ILogger<McpStartCommand> logger, IPackagingService packagingService, IEnvironmentChecker environmentChecker)
        : base("start", McpCommandStrings.StartCommand_Description, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _executionContext = executionContext;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _packagingService = packagingService;
        _environmentChecker = environmentChecker;

        _dashboardUrlOption = new Option<string?>("--dashboard-url")
        {
            Description = McpCommandStrings.StartCommand_DashboardUrlDescription,
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("ASPIRE_MCP_DASHBOARD_URL")
        };
        Options.Add(_dashboardUrlOption);

        _apiKeyOption = new Option<string?>("--api-key", "-k")
        {
            Description = McpCommandStrings.StartCommand_ApiKeyDescription,
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("ASPIRE_MCP_API_KEY")
        };
        Options.Add(_apiKeyOption);
    }

    /// <summary>
    /// Initializes the available tools based on the current mode (standalone vs AppHost).
    /// In standalone mode, AppHost-specific tools are excluded to reduce context for AI agents.
    /// </summary>
    private void InitializeTools()
    {
        _tools.Clear();

        // Always available tools (don't require Dashboard connection)
        _tools["list_integrations"] = new ListIntegrationsTool(_packagingService, _executionContext, _auxiliaryBackchannelMonitor);
        _tools["get_integration_docs"] = new GetIntegrationDocsTool();
        _tools["doctor"] = new DoctorTool(_environmentChecker);

        // Telemetry tools (proxied to Dashboard - available in both modes)
        _tools["list_structured_logs"] = new ListStructuredLogsTool();
        _tools["list_traces"] = new ListTracesTool();
        _tools["list_trace_structured_logs"] = new ListTraceStructuredLogsTool();
        _tools["list_telemetry_fields"] = new ListTelemetryFieldsTool();
        _tools["get_telemetry_field_values"] = new GetTelemetryFieldValuesTool();

        // AppHost-only tools (not available in standalone mode)
        if (!IsStandaloneMode)
        {
            _tools["list_resources"] = new ListResourcesTool();
            _tools["list_console_logs"] = new ListConsoleLogsTool();
            _tools["execute_resource_command"] = new ExecuteResourceCommandTool();
            _tools["select_apphost"] = new SelectAppHostTool(_auxiliaryBackchannelMonitor, _executionContext);
            _tools["list_apphosts"] = new ListAppHostsTool(_auxiliaryBackchannelMonitor, _executionContext);
        }
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Parse standalone Dashboard options
        _standaloneDashboardUrl = parseResult.GetValue(_dashboardUrlOption);
        _standaloneApiKey = parseResult.GetValue(_apiKeyOption);

        // Validate that --api-key is only used with --dashboard-url
        if (!string.IsNullOrEmpty(_standaloneApiKey) && !IsStandaloneMode)
        {
            InteractionService.DisplayError(McpCommandStrings.StartCommand_ApiKeyWithoutDashboardUrlError);
            return ExitCodeConstants.FailedToParseCli;
        }

        if (IsStandaloneMode)
        {
            _logger.LogInformation("Starting MCP server in standalone Dashboard mode. Dashboard URL: {DashboardUrl}", _standaloneDashboardUrl);
        }

        // Initialize tools based on mode (standalone vs AppHost)
        InitializeTools();

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
        await server.RunAsync(cancellationToken);

        return ExitCodeConstants.Success;
    }

    private ValueTask<ListToolsResult> HandleListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        // Parameters required by delegate signature
        _ = request;
        _ = cancellationToken;

        _logger.LogDebug("MCP ListTools request received");

        var tools = _tools.Values.Select(tool => new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.GetInputSchema()
        }).ToArray();

        _logger.LogDebug("Returning {ToolCount} tools: {ToolNames}", tools.Length, string.Join(", ", tools.Select(t => t.Name)));

        return ValueTask.FromResult(new ListToolsResult
        {
            Tools = tools
        });
    }

    private async ValueTask<CallToolResult> HandleCallToolAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        var toolName = request.Params?.Name ?? string.Empty;

        _logger.LogDebug("MCP CallTool request received for tool: {ToolName}", toolName);

        if (_tools.TryGetValue(toolName, out var tool))
        {
            // Handle tools that don't need a Dashboard connection
            if (toolName is "list_integrations" or "get_integration_docs" or "doctor")
            {
                return await tool.CallToolAsync(null!, request.Params?.Arguments, cancellationToken);
            }

            // Handle AppHost-specific tools (only available in non-standalone mode)
            if (toolName is "select_apphost" or "list_apphosts")
            {
                return await tool.CallToolAsync(null!, request.Params?.Arguments, cancellationToken);
            }

            // Get Dashboard connection (either standalone or via AppHost)
            var (endpointUrl, apiToken) = GetDashboardConnection();

            // Create HTTP transport to the dashboard's MCP server
            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpointUrl),
            };

            if (!string.IsNullOrEmpty(apiToken))
            {
                transportOptions.AdditionalHeaders = new Dictionary<string, string>
                {
                    ["x-mcp-api-key"] = apiToken
                };
            }

            using var httpClient = new HttpClient();

            await using var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

            // Create MCP client to communicate with the dashboard
            await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

            _logger.LogDebug("Calling tool {ToolName} on dashboard MCP server", toolName);

            // Call the tool with the MCP client
            try
            {
                _logger.LogDebug("Invoking CallToolAsync for tool {ToolName} with arguments: {Arguments}", toolName, request.Params?.Arguments);
                var result = await tool.CallToolAsync(mcpClient, request.Params?.Arguments, cancellationToken);
                _logger.LogDebug("CallToolAsync for tool {ToolName} completed successfully", toolName);

                _logger.LogDebug("Tool {ToolName} completed successfully", toolName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling tool {ToolName}", toolName);
                throw;
            }
        }

        _logger.LogWarning("Unknown tool requested: {ToolName}", toolName);
        throw new McpProtocolException($"Unknown tool: '{toolName}'", McpErrorCode.MethodNotFound);
    }

    /// <summary>
    /// Gets the Dashboard MCP connection information (endpoint URL and optional API token).
    /// In standalone mode, returns the configured Dashboard URL and API key.
    /// In AppHost mode, discovers the connection via the backchannel.
    /// </summary>
    private (string EndpointUrl, string? ApiToken) GetDashboardConnection()
    {
        if (IsStandaloneMode)
        {
            _logger.LogDebug("Using standalone Dashboard connection: {Url}", _standaloneDashboardUrl);
            return (_standaloneDashboardUrl!, _standaloneApiKey);
        }

        // AppHost mode - use backchannel discovery
        var connection = GetSelectedAppHostConnection();
        if (connection == null)
        {
            _logger.LogWarning("No Aspire AppHost is currently running");
            throw new McpProtocolException(
                "No Aspire AppHost is currently running. " +
                "To use Aspire MCP tools, either start an Aspire application with 'aspire run', " +
                "or connect directly to a standalone Dashboard using --dashboard-url.",
                McpErrorCode.InternalError);
        }

        if (connection.McpInfo == null)
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

        return (connection.McpInfo.EndpointUrl, connection.McpInfo.ApiToken);
    }

    /// <summary>
    /// Gets the appropriate AppHost connection based on the selection logic:
    /// 1. If a specific AppHost is selected via select_apphost, use that
    /// 2. Otherwise, look for in-scope connections (AppHosts within the working directory)
    /// 3. If exactly one in-scope connection exists, use it
    /// 4. If multiple in-scope connections exist, throw an error listing them
    /// 5. If no in-scope connections exist, fall back to the first available connection
    /// </summary>
    private AppHostAuxiliaryBackchannel? GetSelectedAppHostConnection()
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
