// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp;
using Aspire.Cli.Mcp.Docs;
using Aspire.Cli.Mcp.Tools;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;
using Aspire.Shared.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command that starts the MCP (Model Context Protocol) server.
/// This is the new command under 'aspire agent mcp'.
/// </summary>
internal sealed class AgentMcpCommand : BaseCommand
{
    private readonly Dictionary<string, CliMcpTool> _knownTools;
    private readonly IMcpResourceToolRefreshService _resourceToolRefreshService;
    private McpServer? _server;
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly CliExecutionContext _executionContext;
    private readonly IMcpTransportFactory _transportFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentMcpCommand> _logger;
    private readonly IDocsIndexService _docsIndexService;

    /// <summary>
    /// Gets the dictionary of known MCP tools. Exposed for testing purposes.
    /// </summary>
    internal IReadOnlyDictionary<string, CliMcpTool> KnownTools => _knownTools;

    public AgentMcpCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        IMcpTransportFactory transportFactory,
        ILoggerFactory loggerFactory,
        ILogger<AgentMcpCommand> logger,
        IPackagingService packagingService,
        IEnvironmentChecker environmentChecker,
        IDocsSearchService docsSearchService,
        IDocsIndexService docsIndexService,
        AspireCliTelemetry telemetry)
        : base("mcp", AgentCommandStrings.McpCommand_Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _executionContext = executionContext;
        _transportFactory = transportFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _docsIndexService = docsIndexService;
        _resourceToolRefreshService = new McpResourceToolRefreshService(auxiliaryBackchannelMonitor, loggerFactory.CreateLogger<McpResourceToolRefreshService>());
        _knownTools = new Dictionary<string, CliMcpTool>
        {
            [KnownMcpTools.ListResources] = new ListResourcesTool(auxiliaryBackchannelMonitor, loggerFactory.CreateLogger<ListResourcesTool>()),
            [KnownMcpTools.ListConsoleLogs] = new ListConsoleLogsTool(auxiliaryBackchannelMonitor, loggerFactory.CreateLogger<ListConsoleLogsTool>()),
            [KnownMcpTools.ExecuteResourceCommand] = new ExecuteResourceCommandTool(auxiliaryBackchannelMonitor, loggerFactory.CreateLogger<ExecuteResourceCommandTool>()),
            [KnownMcpTools.ListStructuredLogs] = new ListStructuredLogsTool(),
            [KnownMcpTools.ListTraces] = new ListTracesTool(),
            [KnownMcpTools.ListTraceStructuredLogs] = new ListTraceStructuredLogsTool(),
            [KnownMcpTools.SelectAppHost] = new SelectAppHostTool(auxiliaryBackchannelMonitor, executionContext),
            [KnownMcpTools.ListAppHosts] = new ListAppHostsTool(auxiliaryBackchannelMonitor, executionContext),
            [KnownMcpTools.ListIntegrations] = new ListIntegrationsTool(packagingService, executionContext, auxiliaryBackchannelMonitor),
            [KnownMcpTools.Doctor] = new DoctorTool(environmentChecker),
            [KnownMcpTools.RefreshTools] = new RefreshToolsTool(_resourceToolRefreshService),
            [KnownMcpTools.ListDocs] = new ListDocsTool(docsIndexService),
            [KnownMcpTools.SearchDocs] = new SearchDocsTool(docsSearchService, docsIndexService),
            [KnownMcpTools.GetDoc] = new GetDocTool(docsIndexService)
        };
    }

    protected override bool UpdateNotificationsEnabled => false;

    /// <summary>
    /// Public entry point for executing the MCP server command.
    /// This allows McpStartCommand to delegate to this implementation.
    /// </summary>
    internal Task<int> ExecuteCommandAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return ExecuteAsync(parseResult, cancellationToken);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var icons = McpIconHelper.GetAspireIcons(typeof(AgentMcpCommand).Assembly, "Aspire.Cli.Mcp.Resources");

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

        var transport = _transportFactory.CreateTransport();
        await using var server = McpServer.Create(transport, options, _loggerFactory);

        // Configure the refresh service with the server
        _resourceToolRefreshService.SetMcpServer(server);
        _server = server;

        // Starts the MCP server, it's blocking until cancellation is requested
        await server.RunAsync(cancellationToken);

        // Clear the server reference on exit
        _resourceToolRefreshService.SetMcpServer(null);
        _server = null;

        return ExitCodeConstants.Success;
    }

    private async ValueTask<ListToolsResult> HandleListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP ListTools request received");

        var tools = new List<Tool>();

        tools.AddRange(KnownTools.Values.Select(tool => new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.GetInputSchema()
        }));

        try
        {
            // Refresh resource tools if needed (e.g., AppHost selection changed or invalidated)
            if (_resourceToolRefreshService.NeedsRefresh())
            {
                await _resourceToolRefreshService.RefreshResourceToolMapAsync(cancellationToken);
                await _resourceToolRefreshService.SendToolsListChangedNotificationAsync(cancellationToken).ConfigureAwait(false);
            }

            tools.AddRange(_resourceToolRefreshService.ResourceToolMap.Select(x => new Tool
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
        if (KnownTools.TryGetValue(toolName, out var tool))
        {
            // Handle tools that don't need an MCP connection to the AppHost
            if (KnownMcpTools.IsLocalTool(toolName))
            {
                var args = request.Params?.Arguments;
                var context = new CallToolContext
                {
                    Notifier = new McpServerNotifier(_server!),
                    McpClient = null,
                    Arguments = args,
                    ProgressToken = request.Params?.ProgressToken
                };
                return await tool.CallToolAsync(context, cancellationToken).ConfigureAwait(false);
            }

            if (KnownMcpTools.IsDashboardTool(toolName))
            {
                var args = request.Params?.Arguments;
                return await CallDashboardToolAsync(toolName, tool, request.Params?.ProgressToken, args, cancellationToken).ConfigureAwait(false);
            }

            // If a tool is registered in _tools, it must be classified as either local or dashboard-backed.
            throw new McpProtocolException(
                $"Tool '{toolName}' is not classified as local or dashboard-backed.",
                McpErrorCode.InternalError);
        }

        var toolsRefreshed = false;

        // Refresh resource tools if needed (e.g., AppHost selection changed or invalidated)
        if (_resourceToolRefreshService.NeedsRefresh())
        {
            await _resourceToolRefreshService.RefreshResourceToolMapAsync(cancellationToken);
            await _resourceToolRefreshService.SendToolsListChangedNotificationAsync(cancellationToken).ConfigureAwait(false);
            toolsRefreshed = true;
        }

        // Resource MCP tools are invoked via the AppHost backchannel (AppHost proxies to the resource MCP endpoint).
        if (_resourceToolRefreshService.ResourceToolMap.TryGetValue(toolName, out var resourceAndTool))
        {
            var connection = await GetSelectedConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                throw new McpProtocolException(
                    "No Aspire AppHost is currently running. To use resource MCP tools, start an Aspire application (e.g. 'aspire run') and then retry.",
                    McpErrorCode.InternalError);
            }

            var args = request.Params?.Arguments;

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
            _resourceToolRefreshService.InvalidateToolMap();
            return await HandleCallToolAsync(request, cancellationToken).ConfigureAwait(false);
        }

        throw new McpProtocolException($"Unknown tool: '{toolName}'", McpErrorCode.MethodNotFound);
    }

    private async ValueTask<CallToolResult> CallDashboardToolAsync(
        string toolName,
        CliMcpTool tool,
        ProgressToken? progressToken,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken)
    {
        var connection = await GetSelectedConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (connection is null)
        {
            _logger.LogWarning("No Aspire AppHost is currently running");
            throw new McpProtocolException(McpErrorMessages.NoAppHostRunning, McpErrorCode.InternalError);
        }

        if (connection.McpInfo is null)
        {
            _logger.LogWarning("Dashboard is not available in the running AppHost");
            throw new McpProtocolException(McpErrorMessages.DashboardNotAvailable, McpErrorCode.InternalError);
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
            var context = new CallToolContext
            {
                Notifier = new McpServerNotifier(_server!),
                McpClient = mcpClient,
                Arguments = arguments,
                ProgressToken = progressToken
            };
            var result = await tool.CallToolAsync(context, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Tool {ToolName} completed successfully", toolName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling tool {ToolName}", toolName);
            throw;
        }
    }

    /// <summary>
    /// Gets the appropriate AppHost connection based on the selection logic.
    /// </summary>
    private Task<IAppHostAuxiliaryBackchannel?> GetSelectedConnectionAsync(CancellationToken cancellationToken)
    {
        return AppHostConnectionHelper.GetSelectedConnectionAsync(_auxiliaryBackchannelMonitor, _logger, cancellationToken);
    }
}
