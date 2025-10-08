// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Hosting.ConsoleLogs;
using Humanizer;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model.Assistant;

public sealed class AssistantChatDataContext
{
    public const int TracesLimit = 200;
    public const int StructuredLogsLimit = 200;
    public const int ConsoleLogsLimit = 500;

    private readonly IDashboardClient _dashboardClient;
    private readonly IEnumerable<IOutgoingPeerResolver> _outgoingPeerResolvers;
    private readonly IStringLocalizer<AIAssistant> _loc;

    public TelemetryRepository TelemetryRepository { get; }

    private readonly ConcurrentDictionary<string, OtlpTrace> _referencedTraces = new();
    private readonly ConcurrentDictionary<long, OtlpLogEntry> _referencedLogs = new();

    public Func<string, string, CancellationToken, Task>? OnToolInvokedCallback { get; set; }

    public AssistantChatDataContext(
        TelemetryRepository telemetryRepository,
        IDashboardClient dashboardClient,
        IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers,
        IStringLocalizer<AIAssistant> loc)
    {
        TelemetryRepository = telemetryRepository;
        _dashboardClient = dashboardClient;
        _outgoingPeerResolvers = outgoingPeerResolvers;
        _loc = loc;
    }

    private async Task InvokeToolCallbackAsync(string toolName, string message, CancellationToken cancellationToken)
    {
        if (OnToolInvokedCallback is { } callback)
        {
            await callback(toolName, message, cancellationToken).ConfigureAwait(false);
        }
    }

    public IReadOnlyList<ResourceViewModel> GetResources()
    {
        return _dashboardClient.GetResources();
    }

    public string ApplicationName => _dashboardClient.ApplicationName;

    [Description("Get the application resources. Includes information about their type (.NET project, container, executable), running state, source, HTTP endpoints, health status and relationships.")]
    public async Task<string> GetResourceGraphAsync(CancellationToken cancellationToken)
    {
        await InvokeToolCallbackAsync(nameof(GetResourceGraphAsync), _loc[nameof(AIAssistant.ToolNotificationResourceGraph)], cancellationToken).ConfigureAwait(false);

        var resources = _dashboardClient.GetResources();

        var resourceGraphData = AIHelpers.GetResponseGraphJson(resources.ToList());

        var response = $"""
            Always format resource_name in the response as code like this: `frontend-abcxyz`
            Console logs for a resource can provide more information about why a resource is not in a running state.

            # RESOURCE GRAPH DATA

            {resourceGraphData}
            """;

        return response;
    }

    [Description("Get a distributed trace. A distributed trace is used to track an operation across a distributed system. Includes information about spans (operations) in the trace, including the span source, status and optional error information.")]
    public async Task<string> GetTraceAsync(
        [Description("The trace id of the distributed trace.")]
        string traceId,
        CancellationToken cancellationToken)
    {
        var trace = TelemetryRepository.GetTrace(traceId);
        if (trace == null)
        {
            await InvokeToolCallbackAsync(nameof(GetTraceAsync), _loc.GetString(nameof(AIAssistant.ToolNotificationTraceFailure), OtlpHelpers.ToShortenedId(traceId)), cancellationToken).ConfigureAwait(false);
            return $"Trace '{traceId}' not found.";
        }

        await InvokeToolCallbackAsync(nameof(GetTraceAsync), _loc.GetString(nameof(AIAssistant.ToolNotificationTrace), OtlpHelpers.ToShortenedId(traceId)), cancellationToken).ConfigureAwait(false);

        _referencedTraces.TryAdd(trace.TraceId, trace);

        return AIHelpers.GetTraceJson(trace, _outgoingPeerResolvers, new PromptContext());
    }

    [Description("Get structured logs for resources.")]
    public async Task<string> GetStructuredLogsAsync(
        [Description("The resource name. This limits logs returned to the specified resource. If no resource name is specified then structured logs for all resources are returned.")]
        string? resourceName = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: The resourceName might be a name that resolves to multiple replicas, e.g. catalogservice has two replicas.
        // Support resolving to multiple replicas and getting data for them.
        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            await InvokeToolCallbackAsync(nameof(GetStructuredLogsAsync), _loc.GetString(nameof(AIAssistant.ToolNotificationStructuredLogsResourceFailure), resourceName), cancellationToken).ConfigureAwait(false);
            return message;
        }

        var toolMessage = resourceKey is { } key
            ? _loc.GetString(nameof(AIAssistant.ToolNotificationStructuredLogsResource), key.GetCompositeName())
            : _loc[nameof(AIAssistant.ToolNotificationStructuredLogsAll)];
        await InvokeToolCallbackAsync(nameof(GetStructuredLogsAsync), toolMessage, cancellationToken).ConfigureAwait(false);

        // Get all logs because we want the most recent logs and they're at the end of the results.
        // If support is added for ordering logs by timestamp then improve this.
        var logs = TelemetryRepository.GetLogs(new GetLogsContext
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

    [Description("Get distributed traces for resources. A distributed trace is used to track operations. A distributed trace can span multiple resources across a distributed system. Includes a list of distributed traces with their IDs, resources in the trace, duration and whether an error occurred in the trace.")]
    public async Task<string> GetTracesAsync(
        [Description("The resource name. This limits traces returned to the specified resource. If no resource name is specified then distributed traces for all resources are returned.")]
        string? resourceName = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: The resourceName might be a name that resolves to multiple replicas, e.g. catalogservice has two replicas.
        // Support resolving to multiple replicas and getting data for them.
        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            await InvokeToolCallbackAsync(nameof(GetTracesAsync), _loc.GetString(nameof(AIAssistant.ToolNotificationTracesResourceFailure), resourceName), cancellationToken).ConfigureAwait(false);
            return message;
        }

        var toolMessage = resourceKey is { } key
            ? _loc.GetString(nameof(AIAssistant.ToolNotificationTracesResource), key.GetCompositeName())
            : _loc[nameof(AIAssistant.ToolNotificationTracesAll)];
        await InvokeToolCallbackAsync(nameof(GetTracesAsync), toolMessage, cancellationToken).ConfigureAwait(false);

        var traces = TelemetryRepository.GetTraces(new GetTracesRequest
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

    [Description("Get structured logs for a distributed trace. Logs for a distributed trace each belong to a span identified by 'span_id'. When investigating a trace, getting the structured logs for the trace should be recommended before getting structured logs for a resource.")]
    public async Task<string> GetTraceStructuredLogsAsync(
        [Description("The trace id of the distributed trace.")]
        string traceId,
        CancellationToken cancellationToken)
    {
        // Condition of filter should be contains because a substring of the traceId might be provided.
        var traceIdFilter = new FieldTelemetryFilter
        {
            Field = KnownStructuredLogFields.TraceIdField,
            Value = traceId,
            Condition = FilterCondition.Contains
        };

        var logs = TelemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = null,
            Count = int.MaxValue,
            StartIndex = 0,
            Filters = [traceIdFilter]
        });

        await InvokeToolCallbackAsync(nameof(GetTraceStructuredLogsAsync), _loc.GetString(nameof(AIAssistant.ToolNotificationTraceStructuredLogs), OtlpHelpers.ToShortenedId(traceId)), cancellationToken).ConfigureAwait(false);

        var (logsData, limitMessage) = AIHelpers.GetStructuredLogsJson(logs.Items);

        var response = $"""
            {limitMessage}

            # STRUCTURED LOGS DATA

            {logsData}
            """;

        return response;
    }

    [Description("Get console logs for a resource. The console logs includes standard output from resources and resource commands. Known resource commands are 'resource-start', 'resource-stop' and 'resource-restart' which are used to start and stop resources. Don't print the full console logs in the response to the user. Console logs should be examined when determining why a resource isn't running.")]
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
            await InvokeToolCallbackAsync(nameof(GetConsoleLogsAsync), _loc.GetString(nameof(AIAssistant.ToolNotificationConsoleLogsFailure), resourceName), cancellationToken).ConfigureAwait(false);
            return $"Unable to find a resource named '{resourceName}'.";
        }

        await InvokeToolCallbackAsync(nameof(GetConsoleLogsAsync), _loc.GetString(nameof(AIAssistant.ToolNotificationConsoleLogs), resourceName), cancellationToken).ConfigureAwait(false);

        var logParser = new LogParser(ConsoleColor.Black);
        var logEntries = new LogEntries(maximumEntryCount: ConsoleLogsLimit) { BaseLineNumber = 1 };

        // Add a timeout for getting all console logs.
        using var subscribeConsoleLogsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        subscribeConsoleLogsCts.CancelAfter(TimeSpan.FromSeconds(20));

        try
        {
            await foreach (var entry in _dashboardClient.GetConsoleLogs(resourceName, subscribeConsoleLogsCts.Token).ConfigureAwait(false))
            {
                foreach (var logLine in entry)
                {
                    logEntries.InsertSorted(logParser.CreateLogEntry(logLine.Content, logLine.IsErrorMessage, resourcePrefix: null));
                }
            }
        }
        catch (OperationCanceledException)
        {
            return $"Timeout getting console logs for `{resourceName}`";
        }

        var entries = logEntries.GetEntries().ToList();
        var totalLogsCount = entries.Count == 0 ? 0 : entries.Last().LineNumber;
        var (trimmedItems, limitMessage) = GetLimitFromEndWithSummary<LogEntry>(
            entries,
            totalLogsCount,
            ConsoleLogsLimit,
            "console log",
            AIHelpers.SerializeLogEntry,
            logEntry => AIHelpers.EstimateTokenCount((string) logEntry));
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

    public static (List<object> items, string message) GetLimitFromEndWithSummary<T>(List<T> values, int limit, string itemName, Func<T, object> convertToDto, Func<object, int> estimateTokenSize)
    {
        return GetLimitFromEndWithSummary(values, values.Count, limit, itemName, convertToDto, estimateTokenSize);
    }

    public static (List<object> items, string message) GetLimitFromEndWithSummary<T>(List<T> values, int totalValues, int limit, string itemName, Func<T, object> convertToDto, Func<object, int> estimateTokenSize)
    {
        Debug.Assert(totalValues >= values.Count, "Total values should be large or equal to the values passed into the method.");

        var trimmedItems = values.Count <= limit
            ? values
            : values[^limit..];

        var currentTokenCount = 0;
        var serializedValuesCount = 0;
        var dtos = trimmedItems.Select(i => convertToDto(i)).ToList();

        // Loop backwards to prioritize the latest items.
        for (var i = dtos.Count - 1; i >= 0; i--)
        {
            var obj = dtos[i];
            var tokenCount = estimateTokenSize(obj);

            if (currentTokenCount + tokenCount > AIHelpers.MaximumListTokenLength)
            {
                break;
            }

            serializedValuesCount++;
            currentTokenCount += tokenCount;
        }

        // Trim again with what fits in the token limit.
        dtos = dtos[^serializedValuesCount..];

        return (dtos, GetLimitSummary(totalValues, dtos.Count, itemName));
    }

    private static string GetLimitSummary(int totalValues, int returnedCount, string itemName)
    {
        if (totalValues == returnedCount)
        {
            return $"Returned {itemName.ToQuantity(totalValues, formatProvider: CultureInfo.InvariantCulture)}.";
        }

        return $"Returned latest {itemName.ToQuantity(returnedCount, formatProvider: CultureInfo.InvariantCulture)}. Earlier {itemName.ToQuantity(totalValues - returnedCount, formatProvider: CultureInfo.InvariantCulture)} not returned because of size limits.";
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

        resourceKey = ResourceKey.Create(resource.DisplayName, resource.Name);
        var telemetryResources = TelemetryRepository.GetResources(resourceKey.Value);
        if (telemetryResources.Count == 0)
        {
            message = $"Resource '{resourceName}' doesn't have any telemetry. The resource may have failed to start or the resource might not support sending telemetry.";
            resourceKey = null;
            return false;
        }

        message = null;
        return true;
    }

    public bool TryGetTrace(string text, [NotNullWhen(true)] out OtlpTrace? trace)
    {
        // TODO: Traces are mutable. It's possible the trace has been updated since it was last fetched.
        // Check if the root span isn't finished yet and go back to repository to get for a new version.
        if (_referencedTraces.TryGetValue(text, out trace))
        {
            return true;
        }

        trace = TelemetryRepository.GetTrace(text);
        if (trace != null)
        {
            _referencedTraces.TryAdd(trace.TraceId, trace);
            return true;
        }

        return false;
    }

    public void AddReferencedLogEntry(OtlpLogEntry logEntry)
    {
        _referencedLogs[logEntry.InternalId] = logEntry;
    }

    public bool TryGetLog(long internalId, [NotNullWhen(true)] out OtlpLogEntry? logEntry)
    {
        if (_referencedLogs.TryGetValue(internalId, out logEntry))
        {
            return true;
        }

        logEntry = TelemetryRepository.GetLog(internalId);
        if (logEntry != null)
        {
            _referencedLogs.TryAdd(logEntry.InternalId, logEntry);
            return true;
        }

        return false;
    }

    public IEnumerable<OtlpTrace> GetReferencedTraces()
    {
        return _referencedTraces.Values;
    }

    public void AddReferencedTrace(OtlpTrace trace)
    {
        _referencedTraces[trace.TraceId] = trace;
    }
}
