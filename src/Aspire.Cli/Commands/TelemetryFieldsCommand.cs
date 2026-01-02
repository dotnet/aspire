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

internal sealed class TelemetryFieldsCommand : BaseCommand
{
    private readonly IAuxiliaryBackchannelMonitor _auxiliaryBackchannelMonitor;
    private readonly CliExecutionContext _executionContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TelemetryFieldsCommand> _logger;

    private readonly Option<string?> _typeOption;
    private readonly Option<string?> _resourceOption;
    private readonly Option<bool> _jsonOption;
    private readonly Argument<string?> _fieldNameArgument;

    public TelemetryFieldsCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILoggerFactory loggerFactory,
        ILogger<TelemetryFieldsCommand> logger)
        : base("fields", TelemetryCommandStrings.FieldsDescription, features, updateNotifier, executionContext, interactionService)
    {
        _auxiliaryBackchannelMonitor = auxiliaryBackchannelMonitor;
        _executionContext = executionContext;
        _loggerFactory = loggerFactory;
        _logger = logger;

        _typeOption = new Option<string?>("--type", "-t")
        {
            Description = TelemetryCommandStrings.FieldsTypeOptionDescription
        };
        Options.Add(_typeOption);

        _resourceOption = new Option<string?>("--resource", "-r")
        {
            Description = TelemetryCommandStrings.FieldsResourceOptionDescription
        };
        Options.Add(_resourceOption);

        _jsonOption = new Option<bool>("--json", "-j")
        {
            Description = TelemetryCommandStrings.FieldsJsonOptionDescription
        };
        Options.Add(_jsonOption);

        _fieldNameArgument = new Argument<string?>("field-name")
        {
            Description = TelemetryCommandStrings.FieldsFieldNameArgumentDescription,
            Arity = ArgumentArity.ZeroOrOne
        };
        Arguments.Add(_fieldNameArgument);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var type = parseResult.GetValue(_typeOption);
        var resourceName = parseResult.GetValue(_resourceOption);
        var outputJson = parseResult.GetValue(_jsonOption);
        var fieldName = parseResult.GetValue(_fieldNameArgument);

        // Get the parent command's options (they are recursive)
        var dashboardUrl = parseResult.GetValue<string?>("--dashboard-url");
        var apiKey = parseResult.GetValue<string?>("--api-key");

        _logger.LogDebug("Telemetry fields command executing with type={Type}, resource={Resource}, json={Json}, fieldName={FieldName}", 
            type, resourceName, outputJson, fieldName);

        try
        {
            // Get Dashboard connection
            var (endpointUrl, apiToken) = GetDashboardConnection(dashboardUrl, apiKey);

            if (string.IsNullOrEmpty(endpointUrl))
            {
                InteractionService.DisplayError(TelemetryCommandStrings.FieldsNoDashboardError);
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
            if (string.IsNullOrEmpty(fieldName))
            {
                // List all fields
                result = await ListFieldsAsync(mcpClient, type, resourceName, cancellationToken);
            }
            else
            {
                // Get values for specific field
                result = await GetFieldValuesAsync(mcpClient, fieldName, type, resourceName, cancellationToken);
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
                
                if (string.IsNullOrEmpty(fieldName))
                {
                    formatter.FormatFields(ExtractJsonFromResult(result));
                }
                else
                {
                    formatter.FormatFieldValues(ExtractJsonFromResult(result));
                }
            }

            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing telemetry fields command");
            InteractionService.DisplayError(ex.Message);
            return ExitCodeConstants.FailedToConnectToDashboard;
        }
    }

    private static async Task<string> ListFieldsAsync(McpClient mcpClient, string? type, string? resourceName, CancellationToken cancellationToken)
    {
        var tool = new ListTelemetryFieldsTool();
        var arguments = new Dictionary<string, JsonElement>();

        if (!string.IsNullOrEmpty(type))
        {
            arguments["type"] = JsonDocument.Parse($"\"{type}\"").RootElement;
        }

        if (!string.IsNullOrEmpty(resourceName))
        {
            arguments["resourceName"] = JsonDocument.Parse($"\"{resourceName}\"").RootElement;
        }

        var result = await tool.CallToolAsync(mcpClient, arguments.Count > 0 ? arguments : null, cancellationToken);
        return GetTextFromResult(result);
    }

    private static async Task<string> GetFieldValuesAsync(McpClient mcpClient, string fieldName, string? type, string? resourceName, CancellationToken cancellationToken)
    {
        var tool = new GetTelemetryFieldValuesTool();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["fieldName"] = JsonDocument.Parse($"\"{fieldName}\"").RootElement
        };

        if (!string.IsNullOrEmpty(type))
        {
            arguments["type"] = JsonDocument.Parse($"\"{type}\"").RootElement;
        }

        if (!string.IsNullOrEmpty(resourceName))
        {
            arguments["resourceName"] = JsonDocument.Parse($"\"{resourceName}\"").RootElement;
        }

        var result = await tool.CallToolAsync(mcpClient, arguments, cancellationToken);
        return GetTextFromResult(result);
    }

    private static string GetTextFromResult(CallToolResult result)
        => TelemetryCommandHelper.GetTextFromResult(result);

    private static string ExtractJsonFromResult(string result)
        => TelemetryCommandHelper.ExtractJsonFromResult(result);

    private (string? EndpointUrl, string? ApiToken) GetDashboardConnection(string? dashboardUrl, string? apiKey)
    {
        return TelemetryCommandHelper.GetDashboardConnection(dashboardUrl, apiKey, _auxiliaryBackchannelMonitor, _logger);
    }
}
