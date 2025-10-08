// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Hosting.ConsoleLogs;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Aspire.Dashboard.Mcp;

[McpServerToolType]
internal sealed class ResourceMcpTools
{
    private static readonly ConcurrentDictionary<string, OtlpTrace> s_referencedTraces = new();
    private static readonly ConcurrentDictionary<long, OtlpLogEntry> s_referencedLogs = new();

    private readonly TelemetryRepository _telemetryRepository;
    private readonly IDashboardClient _dashboardClient;
    private readonly IEnumerable<IOutgoingPeerResolver> _outgoingPeerResolvers;

    public ResourceMcpTools(TelemetryRepository telemetryRepository, IDashboardClient dashboardClient, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        _telemetryRepository = telemetryRepository;
        _dashboardClient = dashboardClient;
        _outgoingPeerResolvers = outgoingPeerResolvers;
    }

    [McpServerTool(Name = "aspire_resource_graph"), Description("Get the application resources. Includes information about their type (.NET project, container, executable), running state, source, HTTP endpoints, health status and relationships.")]
    public string GetResourceGraph()
    {
        try
        {
            var cts = new CancellationTokenSource(millisecondsDelay: 500);
            var resources = _dashboardClient.GetResources();

            var resourceGraphData = AIHelpers.GetResponseGraphJson(resources);

            var response = $"""
            Always format resource_name in the response as code like this: `frontend-abcxyz`
            Console logs for a resource can provide more information about why a resource is not in a running state.

            # RESOURCE GRAPH DATA

            {resourceGraphData}
            """;

            return response;
        }
        catch { }

        return "No resources found.";
    }

    [McpServerTool(Name = "aspire_structured_logs"), Description("Get structured logs for resources.")]
    public string GetStructuredLogs(
        [Description("The resource name. This limits logs returned to the specified resource. If no resource name is specified then structured logs for all resources are returned.")]
        string? resourceName = null)
    {
        // TODO: The resourceName might be a name that resolves to multiple replicas, e.g. catalogservice has two replicas.
        // Support resolving to multiple replicas and getting data for them.
        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            return message;
        }

        // Get all logs because we want the most recent logs and they're at the end of the results.
        // If support is added for ordering logs by timestamp then improve this.
        var logs = _telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = []
        });

        var (logsData, limitMessage) = AIHelpers.GetStructuredLogsJson(logs.Items);

        var response = $"""
            Always format log_id in the response as code like this: `log_id: 123`.
            {limitMessage}

            # STRUCTURED LOGS DATA

            {logsData}
            """;

        return response;
    }

    [McpServerTool(Name = "aspire_traces"), Description("Get distributed traces for resources. A distributed trace is used to track operations. A distributed trace can span multiple resources across a distributed system. Includes a list of distributed traces with their IDs, resources in the trace, duration and whether an error occurred in the trace.")]
    public string GetTraces(
        [Description("The resource name. This limits traces returned to the specified resource. If no resource name is specified then distributed traces for all resources are returned.")]
        string? resourceName = null)
    {
        // TODO: The resourceName might be a name that resolves to multiple replicas, e.g. catalogservice has two replicas.
        // Support resolving to multiple replicas and getting data for them.
        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            return message;
        }

        var traces = _telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = [],
            FilterText = string.Empty
        });

        var (tracesData, limitMessage) = AIHelpers.GetTracesJson(traces.PagedResult.Items, _outgoingPeerResolvers);

        var response = $"""
            {limitMessage}

            # TRACES DATA

            {tracesData}
            """;

        return response;
    }

    [McpServerTool(Name = "aspire_trace_structured_logs"), Description("Get structured logs for a distributed trace. Logs for a distributed trace each belong to a span identified by 'span_id'. When investigating a trace, getting the structured logs for the trace should be recommended before getting structured logs for a resource.")]
    public string GetTraceStructuredLogs(
        [Description("The trace id of the distributed trace.")]
        string traceId)
    {
        // Condition of filter should be contains because a substring of the traceId might be provided.
        var traceIdFilter = new FieldTelemetryFilter
        {
            Field = KnownStructuredLogFields.TraceIdField,
            Value = traceId,
            Condition = FilterCondition.Contains
        };

        var logs = _telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = null,
            Count = int.MaxValue,
            StartIndex = 0,
            Filters = [traceIdFilter]
        });

        var (logsData, limitMessage) = AIHelpers.GetStructuredLogsJson(logs.Items);

        var response = $"""
            {limitMessage}

            # STRUCTURED LOGS DATA

            {logsData}
            """;

        return response;
    }

    [McpServerTool(Name = "aspire_console_logs"), Description("Get console logs for a resource. The console logs includes standard output from resources and resource commands. Known resource commands are 'resource-start', 'resource-stop' and 'resource-restart' which are used to start and stop resources. Don't print the full console logs in the response to the user. Console logs should be examined when determining why a resource isn't running.")]
    public async Task<string> GetConsoleLogsAsync(
        [Description("The resource name.")]
        string resourceName,
        CancellationToken cancellationToken)
    {
        var resources = _dashboardClient.GetResources();

        if (AIHelpers.TryGetResource(resources, resourceName, out var resource))
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

    [McpServerTool(Name = "aspire_list_commands"), Description("Lists the command names available for a resource. If a resource needs to be restarted and is currently stopped, use the start command instead.")]
    public static string GetResourceCommands(IDashboardClient dashboardClient, [Description("The resource name.")] string resourceName)
    {
        var resource = dashboardClient.GetResource(resourceName);

        if (resource == null)
        {
            throw new McpException($"Resource '{resourceName}' not found.", McpErrorCode.InvalidParams);
        }

        // Only include commands that can be executed (Enabled).

        return string.Join("\n---\n", resource.Commands.Where(cmd => cmd.State == CommandViewModelState.Enabled).Select(cmd => cmd.Name));
    }

    [McpServerTool(Name = "aspire_execute_command"), Description("Executes a command on a resource.")]
    public static async Task ExecuteCommand(IDashboardClient dashboardClient, [Description("The resource name")] string resourceName, [Description("The command name")] string commandName)
    {
        var resource = dashboardClient.GetResource(resourceName);

        if (resource == null)
        {
            throw new McpException($"Resource '{resourceName}' not found.", McpErrorCode.InvalidParams);
        }

        var command = resource.Commands.FirstOrDefault(c => string.Equals(c.Name, commandName, StringComparison.Ordinal));

        if (command is null)
        {
            throw new McpException($"Command '{commandName}' not found for resource '{resourceName}'.", McpErrorCode.InvalidParams);
        }

        // Block execution when command isn't available.
        if (command.State == Model.CommandViewModelState.Hidden)
        {
            throw new McpException($"Command '{commandName}' is not available for resource '{resourceName}'.", McpErrorCode.InvalidParams);
        }

        if (command.State == Model.CommandViewModelState.Disabled)
        {
            if (command.Name == "resource-restart" && resource.Commands.Any(c => c.Name == "resource-start" && c.State == CommandViewModelState.Enabled))
            {
                throw new McpException($"Resource '{resourceName}' is stopped. Use the 'resource-start' command instead of 'resource-restart'.", McpErrorCode.InvalidParams);
            }

            throw new McpException($"Command '{commandName}' is currently disabled for resource '{resourceName}'.", McpErrorCode.InvalidParams);
        }

        try
        {
            var response = await dashboardClient.ExecuteResourceCommandAsync(resource.Name, resource.ResourceType, command, CancellationToken.None).ConfigureAwait(false);

            switch (response.Kind)
            {
                case Model.ResourceCommandResponseKind.Succeeded:
                    return;
                case Model.ResourceCommandResponseKind.Cancelled:
                    throw new McpException($"Command '{commandName}' was cancelled.", McpErrorCode.InternalError);
                case Model.ResourceCommandResponseKind.Failed:
                default:
                    var message = response.ErrorMessage is { Length: > 0 } ? response.ErrorMessage : "Unknown error. See logs for details.";
                    throw new McpException($"Command '{commandName}' failed for resource '{resourceName}': {message}", McpErrorCode.InternalError);
            }
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new McpException($"Error executing command '{commandName}' for resource '{resourceName}': {ex.Message}", McpErrorCode.InternalError);
        }
    }

    private bool TryResolveResourceNameForTelemetry([NotNullWhen(false)] string? resourceName, [NotNullWhen(false)] out string? message, out ResourceKey? resourceKey)
    {
        if (AIHelpers.IsMissingValue(resourceName))
        {
            message = null;
            resourceKey = null;
            return true;
        }

        var resources = _dashboardClient.GetResources();

        if (!AIHelpers.TryGetResource(resources, resourceName, out var resource))
        {
            message = $"Unable to find a resource named '{resourceName}'.";
            resourceKey = null;
            return false;
        }

        var appKey = ResourceKey.Create(resource.Name, resource.Name);
        var apps = _telemetryRepository.GetResources(appKey);
        if (apps.Count == 0)
        {
            message = $"Resource '{resourceName}' doesn't have any telemetry. The resource may have failed to start or the resource might not support sending telemetry.";
            resourceKey = null;
            return false;
        }

        message = null;
        resourceKey = appKey;
        return true;
    }

    public bool TryGetTrace(string text, [NotNullWhen(true)] out OtlpTrace? trace)
    {
        // TODO: Traces are mutable. It's possible the trace has been updated since it was last fetched.
        // Check if the root span isn't finished yet and go back to repository to get for a new version.
        if (s_referencedTraces.TryGetValue(text, out trace))
        {
            return true;
        }

        trace = _telemetryRepository.GetTrace(text);
        if (trace != null)
        {
            s_referencedTraces.TryAdd(trace.TraceId, trace);
            return true;
        }

        return false;
    }

    public static void AddReferencedLogEntry(OtlpLogEntry logEntry)
    {
        s_referencedLogs[logEntry.InternalId] = logEntry;
    }

    public bool TryGetLog(long internalId, [NotNullWhen(true)] out OtlpLogEntry? logEntry)
    {
        if (s_referencedLogs.TryGetValue(internalId, out logEntry))
        {
            return true;
        }

        logEntry = _telemetryRepository.GetLog(internalId);
        if (logEntry != null)
        {
            s_referencedLogs.TryAdd(logEntry.InternalId, logEntry);
            return true;
        }

        return false;
    }

    public static IEnumerable<OtlpTrace> GetReferencedTraces()
    {
        return s_referencedTraces.Values;
    }

    public static void AddReferencedTrace(OtlpTrace trace)
    {
        s_referencedTraces[trace.TraceId] = trace;
    }
}
