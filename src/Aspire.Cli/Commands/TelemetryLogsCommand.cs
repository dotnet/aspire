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
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Commands;

internal sealed class TelemetryLogsCommand : BaseCommand
{
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TelemetryLogsCommand> _logger;

    private readonly Option<string?> _resourceOption;
    private readonly Option<string?> _traceOption;
    private readonly Option<string?> _spanOption;
    private readonly Option<string[]> _filterOption;
    private readonly Option<string?> _severityOption;
    private readonly Option<int> _limitOption;
    private readonly Option<bool> _jsonOption;

    public TelemetryLogsCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILoggerFactory loggerFactory,
        ILogger<TelemetryLogsCommand> logger)
        : base("logs", TelemetryCommandStrings.LogsDescription, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _loggerFactory = loggerFactory;
        _logger = logger;

        _resourceOption = new Option<string?>("--resource", "-r")
        {
            Description = TelemetryCommandStrings.LogsResourceOptionDescription
        };
        Options.Add(_resourceOption);

        _traceOption = new Option<string?>("--trace", "-t")
        {
            Description = TelemetryCommandStrings.LogsTraceOptionDescription
        };
        Options.Add(_traceOption);

        _spanOption = new Option<string?>("--span")
        {
            Description = TelemetryCommandStrings.LogsSpanOptionDescription
        };
        Options.Add(_spanOption);

        _filterOption = new Option<string[]>("--filter", "-f")
        {
            Description = TelemetryCommandStrings.LogsFilterOptionDescription,
            AllowMultipleArgumentsPerToken = true
        };
        Options.Add(_filterOption);

        _severityOption = new Option<string?>("--severity", "-s")
        {
            Description = TelemetryCommandStrings.LogsSeverityOptionDescription
        };
        Options.Add(_severityOption);

        _limitOption = new Option<int>("--limit", "-l")
        {
            Description = TelemetryCommandStrings.LogsLimitOptionDescription,
            DefaultValueFactory = _ => 100
        };
        Options.Add(_limitOption);

        _jsonOption = new Option<bool>("--json", "-j")
        {
            Description = TelemetryCommandStrings.LogsJsonOptionDescription
        };
        Options.Add(_jsonOption);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue(_resourceOption);
        var traceId = parseResult.GetValue(_traceOption);
        var spanId = parseResult.GetValue(_spanOption);
        var filters = parseResult.GetValue(_filterOption) ?? [];
        var severity = parseResult.GetValue(_severityOption);
        var outputJson = parseResult.GetValue(_jsonOption);

        // Get the parent command's options (they are recursive)
        var dashboardUrl = parseResult.GetValue<string?>("--dashboard-url");
        var apiKey = parseResult.GetValue<string?>("--api-key");

        _logger.LogDebug("Telemetry logs command executing with resource={Resource}, trace={TraceId}, span={SpanId}, filters={FilterCount}, severity={Severity}, json={Json}",
            resourceName, traceId, spanId, filters.Length, severity, outputJson);

        // Validate severity if provided
        if (!string.IsNullOrEmpty(severity) && !IsValidSeverity(severity))
        {
            InteractionService.DisplayError(TelemetryCommandStrings.LogsInvalidSeverityError);
            return ExitCodeConstants.InvalidCommand;
        }

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
                InteractionService.DisplayError(TelemetryCommandStrings.LogsNoDashboardError);
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

            // List logs with filters
            var result = await ListLogsAsync(mcpClient, resourceName, traceId, spanId, parsedFilters, severity, cancellationToken);

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
                formatter.FormatLogs(ExtractJsonFromResult(result));
            }

            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing telemetry logs command");
            InteractionService.DisplayError(ex.Message);
            return ExitCodeConstants.FailedToConnectToDashboard;
        }
    }

    private static async Task<string> ListLogsAsync(McpClient mcpClient, string? resourceName, string? traceId, string? spanId, List<ParsedFilter> parsedFilters, string? severity, CancellationToken cancellationToken)
    {
        var tool = new ListStructuredLogsTool();
        var arguments = new Dictionary<string, JsonElement>();

        if (!string.IsNullOrEmpty(resourceName))
        {
            arguments["resourceName"] = JsonDocument.Parse($"\"{EscapeJsonString(resourceName)}\"").RootElement;
        }

        if (!string.IsNullOrEmpty(severity))
        {
            arguments["severity"] = JsonDocument.Parse($"\"{EscapeJsonString(severity)}\"").RootElement;
        }

        // Build filters list combining explicit filters with trace/span filters
        var filterDtos = new List<TelemetryFilterDto>();

        // Add trace ID filter if provided
        if (!string.IsNullOrEmpty(traceId))
        {
            filterDtos.Add(new TelemetryFilterDto
            {
                Field = "trace.id",
                Condition = "equals",
                Value = traceId
            });
        }

        // Add span ID filter if provided
        if (!string.IsNullOrEmpty(spanId))
        {
            filterDtos.Add(new TelemetryFilterDto
            {
                Field = "span.id",
                Condition = "equals",
                Value = spanId
            });
        }

        // Convert pre-parsed filters to TelemetryFilterDto format
        foreach (var parsed in parsedFilters)
        {
            filterDtos.Add(parsed.ToTelemetryFilter());
        }

        // Add filters to arguments if we have any
        if (filterDtos.Count > 0)
        {
            var filtersJson = JsonSerializer.Serialize(filterDtos, TelemetryJsonContext.Default.ListTelemetryFilterDto);
            arguments["filters"] = JsonDocument.Parse(filtersJson).RootElement;
        }

        var result = await tool.CallToolAsync(mcpClient, arguments.Count > 0 ? arguments : null, cancellationToken);
        return GetTextFromResult(result);
    }

    private static bool IsValidSeverity(string severity)
    {
        var validSeverities = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        return validSeverities.Contains(severity, StringComparer.OrdinalIgnoreCase);
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
