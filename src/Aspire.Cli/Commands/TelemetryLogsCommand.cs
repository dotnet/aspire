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
        var limit = parseResult.GetValue(_limitOption);
        var outputJson = parseResult.GetValue(_jsonOption);

        // Get the parent command's options (they are recursive)
        var dashboardUrl = parseResult.GetValue<string?>("--dashboard-url");
        var apiKey = parseResult.GetValue<string?>("--api-key");

        _logger.LogDebug("Telemetry logs command executing with resource={Resource}, trace={TraceId}, span={SpanId}, filters={FilterCount}, severity={Severity}, limit={Limit}, json={Json}",
            resourceName, traceId, spanId, filters.Length, severity, limit, outputJson);

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

            // Validate filter fields against available fields (only if we have filters)
            if (parsedFilters.Count > 0)
            {
                var validationError = await ValidateFilterFieldsAsync(mcpClient, parsedFilters, cancellationToken);
                if (validationError != null)
                {
                    InteractionService.DisplayError(validationError);
                    return ExitCodeConstants.InvalidArguments;
                }
            }

            // List logs with filters
            var result = await ListLogsAsync(mcpClient, resourceName, traceId, spanId, parsedFilters, severity, cancellationToken);

            // Extract JSON from the MCP result
            var jsonResult = ExtractJsonFromResult(result);

            // Apply limit
            if (limit > 0)
            {
                jsonResult = ApplyLimit(jsonResult, limit);
            }

            if (outputJson)
            {
                // For JSON output, output the (possibly limited) JSON
                // Use DisplayPlainText to avoid Spectre.Console markup interpretation
                InteractionService.DisplayPlainText(jsonResult);
            }
            else
            {
                // For human-readable output, use the formatter
                var console = Spectre.Console.AnsiConsole.Console;
                var formatter = new TelemetryOutputFormatter(console);
                formatter.FormatLogs(jsonResult);
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

    private async Task<string?> ValidateFilterFieldsAsync(McpClient mcpClient, List<ParsedFilter> parsedFilters, CancellationToken cancellationToken)
    {
        try
        {
            // Call list_telemetry_fields to get available fields
            var fieldsTool = new ListTelemetryFieldsTool();
            var fieldsArgs = new Dictionary<string, JsonElement>
            {
                ["type"] = JsonDocument.Parse("\"logs\"").RootElement
            };

            var fieldsResult = await fieldsTool.CallToolAsync(mcpClient, fieldsArgs, cancellationToken);
            var fieldsJson = GetTextFromResult(fieldsResult);

            if (string.IsNullOrEmpty(fieldsJson))
            {
                // If we can't get fields, skip validation and let the Dashboard handle it
                _logger.LogDebug("Could not retrieve available fields, skipping field validation");
                return null;
            }

            // Validate each filter field
            var filterFields = parsedFilters.Select(f => f.Field).ToList();
            var validationResults = FilterFieldValidator.ValidateFields(filterFields, fieldsJson, "logs");

            if (validationResults.Count > 0)
            {
                // Return first validation error with suggestions
                var firstError = validationResults[0];
                return FilterFieldValidator.FormatValidationError(firstError);
            }

            return null; // All fields valid
        }
        catch (Exception ex)
        {
            // If validation fails for any reason, log and continue without validation
            // The Dashboard will still reject invalid fields
            _logger.LogDebug(ex, "Filter field validation failed, continuing without validation");
            return null;
        }
    }

    private static bool IsValidSeverity(string severity)
    {
        var validSeverities = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        return validSeverities.Contains(severity, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetTextFromResult(CallToolResult result)
        => TelemetryCommandHelper.GetTextFromResult(result);

    private static string ExtractJsonFromResult(string result)
        => TelemetryCommandHelper.ExtractJsonFromResult(result);

    private static string ApplyLimit(string jsonResult, int limit)
    {
        if (string.IsNullOrWhiteSpace(jsonResult) || limit <= 0)
        {
            return jsonResult;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonResult);
            var root = document.RootElement;

            // If the root is an array, limit the number of elements
            if (root.ValueKind == JsonValueKind.Array)
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartArray();
                    var count = 0;
                    foreach (var item in root.EnumerateArray())
                    {
                        if (count >= limit)
                        {
                            break;
                        }
                        item.WriteTo(writer);
                        count++;
                    }
                    writer.WriteEndArray();
                }
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }

            // If it's an object with a "logs" array, limit that array
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("logs", out var logsArray) && logsArray.ValueKind == JsonValueKind.Array)
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name == "logs")
                        {
                            writer.WritePropertyName("logs");
                            writer.WriteStartArray();
                            var count = 0;
                            foreach (var log in logsArray.EnumerateArray())
                            {
                                if (count >= limit)
                                {
                                    break;
                                }
                                log.WriteTo(writer);
                                count++;
                            }
                            writer.WriteEndArray();
                        }
                        else
                        {
                            prop.WriteTo(writer);
                        }
                    }
                    writer.WriteEndObject();
                }
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }

            // Return as-is if we can't identify the structure
            return jsonResult;
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return the original
            return jsonResult;
        }
    }

    private static string EscapeJsonString(string value)
        => TelemetryCommandHelper.EscapeJsonString(value);

    private (string? EndpointUrl, string? ApiToken) GetDashboardConnection(string? dashboardUrl, string? apiKey)
    {
        return TelemetryCommandHelper.GetDashboardConnection(dashboardUrl, apiKey, _auxiliaryBackchannelMonitor, _logger);
    }
}
