// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Mcp;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Commands;

internal sealed class TelemetryTracesCommand : BaseCommand
{
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TelemetryTracesCommand> _logger;

    private readonly Option<string?> _resourceOption;
    private readonly Option<string[]> _filterOption;
    private readonly Option<string?> _searchOption;
    private readonly Option<int> _limitOption;
    private readonly Option<bool> _jsonOption;
    private readonly Argument<string?> _traceIdArgument;

    public TelemetryTracesCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILoggerFactory loggerFactory,
        ILogger<TelemetryTracesCommand> logger)
        : base("traces", TelemetryCommandStrings.TracesDescription, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _loggerFactory = loggerFactory;
        _logger = logger;

        _resourceOption = new Option<string?>("--resource", "-r")
        {
            Description = TelemetryCommandStrings.TracesResourceOptionDescription
        };
        Options.Add(_resourceOption);

        _filterOption = new Option<string[]>("--filter", "-f")
        {
            Description = TelemetryCommandStrings.TracesFilterOptionDescription,
            AllowMultipleArgumentsPerToken = true
        };
        Options.Add(_filterOption);

        _searchOption = new Option<string?>("--search", "-s")
        {
            Description = TelemetryCommandStrings.TracesSearchOptionDescription
        };
        Options.Add(_searchOption);

        _limitOption = new Option<int>("--limit", "-l")
        {
            Description = TelemetryCommandStrings.TracesLimitOptionDescription,
            DefaultValueFactory = _ => 100
        };
        Options.Add(_limitOption);

        _jsonOption = new Option<bool>("--json", "-j")
        {
            Description = TelemetryCommandStrings.TracesJsonOptionDescription
        };
        Options.Add(_jsonOption);

        _traceIdArgument = new Argument<string?>("trace-id")
        {
            Description = TelemetryCommandStrings.TracesTraceIdArgumentDescription,
            Arity = ArgumentArity.ZeroOrOne
        };
        Arguments.Add(_traceIdArgument);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue(_resourceOption);
        var filters = parseResult.GetValue(_filterOption) ?? [];
        var searchText = parseResult.GetValue(_searchOption);
        var outputJson = parseResult.GetValue(_jsonOption);
        var traceId = parseResult.GetValue(_traceIdArgument);

        // Get the parent command's options (they are recursive)
        var dashboardUrl = parseResult.GetValue<string?>("--dashboard-url");
        var apiKey = parseResult.GetValue<string?>("--api-key");

        _logger.LogDebug("Telemetry traces command executing with resource={Resource}, filters={FilterCount}, search={Search}, json={Json}, traceId={TraceId}",
            resourceName, filters.Length, searchText, outputJson, traceId);

        // Validate filter syntax BEFORE connecting to Dashboard
        // This provides early feedback on invalid filter expressions
        List<ParsedFilter> parsedFilters = [];
        foreach (var filterExpr in filters)
        {
            try
            {
                var parsed = FilterExpressionParser.Parse(filterExpr);
                parsedFilters.Add(parsed);
            }
            catch (FilterParseException ex)
            {
                _logger.LogWarning("Invalid filter expression '{Filter}': {Message}", filterExpr, ex.Message);
                InteractionService.DisplayError($"Invalid filter expression '{filterExpr}': {ex.Message}");
                return ExitCodeConstants.InvalidArguments;
            }
        }

        try
        {
            // Get Dashboard connection
            var (endpointUrl, apiToken) = GetDashboardConnection(dashboardUrl, apiKey);

            if (string.IsNullOrEmpty(endpointUrl))
            {
                InteractionService.DisplayError(TelemetryCommandStrings.TracesNoDashboardError);
                return ExitCodeConstants.FailedToConnectToDashboard;
            }

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
            await using var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: false);
            await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

            string result;
            if (!string.IsNullOrEmpty(traceId))
            {
                // Get specific trace by ID
                result = await GetTraceByIdAsync(mcpClient, traceId, cancellationToken);
            }
            else
            {
                // List traces with filters
                result = await ListTracesAsync(mcpClient, resourceName, parsedFilters, searchText, cancellationToken);
            }

            if (outputJson)
            {
                // For JSON output, try to extract just the JSON part from the result
                InteractionService.DisplayMessage(string.Empty, ExtractJsonFromResult(result));
            }
            else
            {
                // For human-readable output, use the formatter
                var console = Spectre.Console.AnsiConsole.Console;
                var formatter = new TelemetryOutputFormatter(console);

                if (!string.IsNullOrEmpty(traceId))
                {
                    formatter.FormatSingleTrace(ExtractJsonFromResult(result));
                }
                else
                {
                    formatter.FormatTraces(ExtractJsonFromResult(result));
                }
            }

            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing telemetry traces command");
            InteractionService.DisplayError(ex.Message);
            return ExitCodeConstants.FailedToConnectToDashboard;
        }
    }

    private static async Task<string> ListTracesAsync(McpClient mcpClient, string? resourceName, List<ParsedFilter> parsedFilters, string? searchText, CancellationToken cancellationToken)
    {
        var tool = new ListTracesTool();
        var arguments = new Dictionary<string, JsonElement>();

        if (!string.IsNullOrEmpty(resourceName))
        {
            arguments["resourceName"] = JsonDocument.Parse($"\"{EscapeJsonString(resourceName)}\"").RootElement;
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            arguments["searchText"] = JsonDocument.Parse($"\"{EscapeJsonString(searchText)}\"").RootElement;
        }

        // Convert pre-parsed filters to JSON array format for MCP tool
        if (parsedFilters.Count > 0)
        {
            var filterDtos = parsedFilters.Select(f => f.ToTelemetryFilter()).ToList();
            var filtersJson = JsonSerializer.Serialize(filterDtos, TelemetryJsonContext.Default.ListTelemetryFilterDto);
            arguments["filters"] = JsonDocument.Parse(filtersJson).RootElement;
        }

        var result = await tool.CallToolAsync(mcpClient, arguments.Count > 0 ? arguments : null, cancellationToken);
        return GetTextFromResult(result);
    }

    private static async Task<string> GetTraceByIdAsync(McpClient mcpClient, string traceId, CancellationToken cancellationToken)
    {
        // Use the get_trace tool to get a specific trace by ID
        var arguments = new Dictionary<string, object?>
        {
            ["traceId"] = traceId
        };

        var result = await mcpClient.CallToolAsync(
            "get_trace",
            arguments,
            serializerOptions: McpJsonUtilities.DefaultOptions,
            cancellationToken: cancellationToken);

        return GetTextFromResult(result);
    }

    private static string GetTextFromResult(CallToolResult result)
    {
        if (result.Content == null || result.Content.Count == 0)
        {
            return string.Empty;
        }

        var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        if (textContent?.Text != null)
        {
            return textContent.Text;
        }

        return string.Empty;
    }

    private static string ExtractJsonFromResult(string result)
    {
        // The MCP tool response contains markdown headers followed by JSON
        // We need to extract just the JSON part for formatting
        var lines = result.Split('\n');
        var jsonStartIndex = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            {
                jsonStartIndex = i;
                break;
            }
        }

        if (jsonStartIndex >= 0)
        {
            return string.Join('\n', lines.Skip(jsonStartIndex));
        }

        // If no JSON found, return the original
        return result;
    }

    private static string EscapeJsonString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private (string? EndpointUrl, string? ApiToken) GetDashboardConnection(string? dashboardUrl, string? apiKey)
    {
        // If dashboard URL is provided, use standalone mode
        if (!string.IsNullOrEmpty(dashboardUrl))
        {
            _logger.LogDebug("Using standalone Dashboard connection: {Url}", dashboardUrl);
            return (dashboardUrl, apiKey);
        }

        // Try to get connection from running AppHost via backchannel
        var connections = _auxiliaryBackchannelMonitor.Connections.Values.ToList();

        if (connections.Count == 0)
        {
            _logger.LogDebug("No AppHost connections available");
            return (null, null);
        }

        // Get in-scope connections
        var inScopeConnections = connections.Where(c => c.IsInScope).ToList();

        AppHostAuxiliaryBackchannel? connection = null;

        if (inScopeConnections.Count == 1)
        {
            connection = inScopeConnections[0];
        }
        else if (inScopeConnections.Count > 1)
        {
            // Multiple in-scope connections - use the first one but log a warning
            _logger.LogWarning("Multiple AppHosts running in scope, using first one");
            connection = inScopeConnections[0];
        }
        else if (connections.Count > 0)
        {
            // No in-scope connections, use the first available
            connection = connections[0];
        }

        if (connection?.McpInfo == null)
        {
            _logger.LogDebug("No Dashboard MCP info available from AppHost");
            return (null, null);
        }

        _logger.LogDebug("Using AppHost Dashboard connection: {Url}", connection.McpInfo.EndpointUrl);
        return (connection.McpInfo.EndpointUrl, connection.McpInfo.ApiToken);
    }
}

/// <summary>
/// JSON serialization context for telemetry command types.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(List<TelemetryFilterDto>))]
[System.Text.Json.Serialization.JsonSourceGenerationOptions(PropertyNamingPolicy = System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class TelemetryJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
