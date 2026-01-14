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

internal sealed class TelemetryMetricsCommand : BaseCommand
{
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TelemetryMetricsCommand> _logger;

    private readonly Option<string?> _resourceOption;
    private readonly Option<string?> _durationOption;
    private readonly Option<bool> _jsonOption;
    private readonly Argument<string?> _instrumentArgument;

    public TelemetryMetricsCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILoggerFactory loggerFactory,
        ILogger<TelemetryMetricsCommand> logger)
        : base("metrics", TelemetryCommandStrings.MetricsDescription, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _loggerFactory = loggerFactory;
        _logger = logger;

        _resourceOption = new Option<string?>("--resource", "-r")
        {
            Description = TelemetryCommandStrings.MetricsResourceOptionDescription,
            Required = true
        };
        Options.Add(_resourceOption);

        _durationOption = new Option<string?>("--duration")
        {
            Description = TelemetryCommandStrings.MetricsDurationOptionDescription,
            DefaultValueFactory = _ => "5m"
        };
        Options.Add(_durationOption);

        _jsonOption = new Option<bool>("--json", "-j")
        {
            Description = TelemetryCommandStrings.MetricsJsonOptionDescription
        };
        Options.Add(_jsonOption);

        _instrumentArgument = new Argument<string?>("meter/instrument")
        {
            Description = TelemetryCommandStrings.MetricsInstrumentArgumentDescription,
            Arity = ArgumentArity.ZeroOrOne
        };
        Arguments.Add(_instrumentArgument);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue(_resourceOption);
        var duration = parseResult.GetValue(_durationOption) ?? "5m";
        var outputJson = parseResult.GetValue(_jsonOption);
        var instrumentPath = parseResult.GetValue(_instrumentArgument);

        // Get the parent command's options (they are recursive)
        var dashboardUrl = parseResult.GetValue<string?>("--dashboard-url");
        var apiKey = parseResult.GetValue<string?>("--api-key");

        _logger.LogDebug("Telemetry metrics command executing with resource={Resource}, duration={Duration}, json={Json}, instrument={Instrument}",
            resourceName, duration, outputJson, instrumentPath);

        // Resource is required for metrics
        if (string.IsNullOrEmpty(resourceName))
        {
            InteractionService.DisplayError(TelemetryCommandStrings.MetricsNoResourceError);
            return ExitCodeConstants.InvalidArguments;
        }

        // Validate duration format
        if (!IsValidDuration(duration))
        {
            InteractionService.DisplayError(TelemetryCommandStrings.MetricsInvalidDurationError);
            return ExitCodeConstants.InvalidArguments;
        }

        try
        {
            // Get Dashboard connection
            var (endpointUrl, apiToken) = GetDashboardConnection(dashboardUrl, apiKey);

            if (string.IsNullOrEmpty(endpointUrl))
            {
                InteractionService.DisplayError(TelemetryCommandStrings.MetricsNoDashboardError);
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
            if (!string.IsNullOrEmpty(instrumentPath))
            {
                // Get specific metric data
                var (meterName, instrumentName) = ParseInstrumentPath(instrumentPath);
                if (string.IsNullOrEmpty(meterName) || string.IsNullOrEmpty(instrumentName))
                {
                    InteractionService.DisplayError(TelemetryCommandStrings.MetricsInvalidInstrumentFormatError);
                    return ExitCodeConstants.InvalidArguments;
                }

                result = await GetMetricDataAsync(mcpClient, resourceName, meterName, instrumentName, duration, cancellationToken);
            }
            else
            {
                // List all metrics for the resource
                result = await ListMetricsAsync(mcpClient, resourceName, cancellationToken);
            }

            if (outputJson)
            {
                // For JSON output, try to extract just the JSON part from the result
                // Use DisplayPlainText to avoid Spectre.Console markup interpretation
                InteractionService.DisplayPlainText(ExtractJsonFromResult(result));
            }
            else
            {
                // For human-readable output, use the formatter
                var console = Spectre.Console.AnsiConsole.Console;
                var formatter = new TelemetryOutputFormatter(console);

                if (!string.IsNullOrEmpty(instrumentPath))
                {
                    formatter.FormatMetricData(ExtractJsonFromResult(result));
                }
                else
                {
                    formatter.FormatMetricsList(ExtractJsonFromResult(result));
                }
            }

            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing telemetry metrics command");
            InteractionService.DisplayError(ex.Message);
            return ExitCodeConstants.FailedToConnectToDashboard;
        }
    }

    private static async Task<string> ListMetricsAsync(McpClient mcpClient, string resourceName, CancellationToken cancellationToken)
    {
        var tool = new ListMetricsTool();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["resourceName"] = JsonDocument.Parse($"\"{EscapeJsonString(resourceName)}\"").RootElement
        };

        var result = await tool.CallToolAsync(mcpClient, arguments, cancellationToken);
        return GetTextFromResult(result);
    }

    private static async Task<string> GetMetricDataAsync(McpClient mcpClient, string resourceName, string meterName, string instrumentName, string duration, CancellationToken cancellationToken)
    {
        var tool = new GetMetricDataTool();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["resourceName"] = JsonDocument.Parse($"\"{EscapeJsonString(resourceName)}\"").RootElement,
            ["meterName"] = JsonDocument.Parse($"\"{EscapeJsonString(meterName)}\"").RootElement,
            ["instrumentName"] = JsonDocument.Parse($"\"{EscapeJsonString(instrumentName)}\"").RootElement,
            ["duration"] = JsonDocument.Parse($"\"{EscapeJsonString(duration)}\"").RootElement
        };

        var result = await tool.CallToolAsync(mcpClient, arguments, cancellationToken);
        return GetTextFromResult(result);
    }

    private static (string? MeterName, string? InstrumentName) ParseInstrumentPath(string path)
    {
        // Expected format: "meter/instrument"
        var slashIndex = path.IndexOf('/');
        if (slashIndex < 1 || slashIndex >= path.Length - 1)
        {
            return (null, null);
        }

        var meterName = path[..slashIndex];
        var instrumentName = path[(slashIndex + 1)..];

        return (meterName, instrumentName);
    }

    private static bool IsValidDuration(string duration)
    {
        var validDurations = new[] { "1m", "5m", "15m", "30m", "1h", "3h", "6h", "12h" };
        return validDurations.Contains(duration, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetTextFromResult(CallToolResult result)
        => TelemetryCommandHelper.GetTextFromResult(result);

    private static string ExtractJsonFromResult(string result)
        => TelemetryCommandHelper.ExtractJsonFromResult(result);

    private static string EscapeJsonString(string value)
        => TelemetryCommandHelper.EscapeJsonString(value);

    private (string? EndpointUrl, string? ApiToken) GetDashboardConnection(string? dashboardUrl, string? apiKey)
    {
        return TelemetryCommandHelper.GetDashboardConnection(dashboardUrl, apiKey, _auxiliaryBackchannelMonitor, _logger);
    }
}
