// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Aspire.Hosting.ConsoleLogs;
using Humanizer;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model.Assistant;

internal static class AIHelpers
{
    public const int TracesLimit = 200;
    public const int StructuredLogsLimit = 200;
    public const int ConsoleLogsLimit = 500;

    // There is currently a 64K token limit in VS.
    // Limit the result from individual token calls to a smaller number so multiple results can live inside the context.
    public const int MaximumListTokenLength = 8192;

    // This value is chosen to balance:
    // - Providing enough data to the model for it to provide accurate answers.
    // - Providing too much data and exceeding length limits.
    public const int MaximumStringLength = 2048;

    // Always pass English translations to AI
    private static readonly IStringLocalizer<Columns> s_columnsLoc = new InvariantStringLocalizer<Columns>();
    private static readonly IStringLocalizer<Commands> s_commandsLoc = new InvariantStringLocalizer<Commands>();

    public static readonly TimeSpan ResponseMessageTimeout = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan CompleteMessageTimeout = TimeSpan.FromMinutes(4);

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    internal static object GetTraceDto(OtlpTrace trace, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers, PromptContext context, DashboardOptions options, bool includeDashboardUrl = false)
    {
        var spanData = trace.Spans.Select(s => new
        {
            span_id = OtlpHelpers.ToShortenedId(s.SpanId),
            parent_span_id = s.ParentSpanId is { } id ? OtlpHelpers.ToShortenedId(id) : null,
            kind = s.Kind.ToString(),
            name = context.AddValue(s.Name, id => $@"Duplicate of ""name"" for span {OtlpHelpers.ToShortenedId(id)}", s.SpanId),
            status = s.Status != OtlpSpanStatusCode.Unset ? s.Status.ToString() : null,
            status_message = context.AddValue(s.StatusMessage, id => $@"Duplicate of ""status_message"" for span {OtlpHelpers.ToShortenedId(id)}", s.SpanId),
            source = s.Source.ResourceKey.GetCompositeName(),
            destination = GetDestination(s, outgoingPeerResolvers),
            duration_ms = ConvertToMilliseconds(s.Duration),
            attributes = s.Attributes
                .ToDictionary(a => a.Key, a => context.AddValue(MapOtelAttributeValue(a), id => $@"Duplicate of attribute ""{id.Key}"" for span {OtlpHelpers.ToShortenedId(id.SpanId)}", (s.SpanId, a.Key))),
            links = s.Links.Select(l => new { trace_id = OtlpHelpers.ToShortenedId(l.TraceId), span_id = OtlpHelpers.ToShortenedId(l.SpanId) }).ToList(),
            back_links = s.BackLinks.Select(l => new { source_trace_id = OtlpHelpers.ToShortenedId(l.SourceTraceId), source_span_id = OtlpHelpers.ToShortenedId(l.SourceSpanId) }).ToList()
        }).ToList();

        var traceId = OtlpHelpers.ToShortenedId(trace.TraceId);
        var traceData = new Dictionary<string, object?>
        {
            ["trace_id"] = traceId,
            ["duration_ms"] = ConvertToMilliseconds(trace.Duration),
            ["title"] = trace.RootOrFirstSpan.Name,
            ["spans"] = spanData,
            ["has_error"] = trace.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error),
            ["timestamp"] = trace.TimeStamp,
        };

        if (includeDashboardUrl)
        {
            traceData["dashboard_link"] = GetDashboardLink(options, DashboardUrls.TraceDetailUrl(traceId), traceId);
        }

        return traceData;
    }

    private static string MapOtelAttributeValue(KeyValuePair<string, string> attribute)
    {
        switch (attribute.Key)
        {
            case "http.response.status_code":
                {
                    if (int.TryParse(attribute.Value, CultureInfo.InvariantCulture, out var value))
                    {
                        return OtelAttributeHelpers.GetHttpStatusName(value);
                    }
                    goto default;
                }
            case "rpc.grpc.status_code":
                {
                    if (int.TryParse(attribute.Value, CultureInfo.InvariantCulture, out var value))
                    {
                        return OtelAttributeHelpers.GetGrpcStatusName(value);
                    }
                    goto default;
                }
            default:
                return attribute.Value;
        }
    }

    private static int ConvertToMilliseconds(TimeSpan duration)
    {
        return (int)Math.Round(duration.TotalMilliseconds, 0, MidpointRounding.AwayFromZero);
    }

    public static (string json, string limitMessage) GetTracesJson(List<OtlpTrace> traces, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers, DashboardOptions options, bool includeDashboardUrl = false)
    {
        var promptContext = new PromptContext();
        var (trimmedItems, limitMessage) = GetLimitFromEndWithSummary(
            traces,
            TracesLimit,
            "trace",
            trace => GetTraceDto(trace, outgoingPeerResolvers, promptContext, options, includeDashboardUrl),
            EstimateSerializedJsonTokenSize);
        var tracesData = SerializeJson(trimmedItems);

        return (tracesData, limitMessage);
    }

    internal static string GetTraceJson(OtlpTrace trace, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers, PromptContext context, DashboardOptions options, bool includeDashboardUrl = false)
    {
        var dto = GetTraceDto(trace, outgoingPeerResolvers, context, options, includeDashboardUrl);

        var json = SerializeJson(dto);
        return json;
    }

    private static string? GetDestination(OtlpSpan s, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        return ResolveUninstrumentedPeerName(s, outgoingPeerResolvers);
    }

    private static string? ResolveUninstrumentedPeerName(OtlpSpan span, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        // Attempt to resolve uninstrumented peer to a friendly name from the span.
        foreach (var resolver in outgoingPeerResolvers)
        {
            if (resolver.TryResolvePeer(span.Attributes, out var name, out _))
            {
                return name;
            }
        }

        // Fallback to the peer address.
        return span.Attributes.GetPeerAddress();
    }

    internal static string GetResponseGraphJson(List<ResourceViewModel> resources, DashboardOptions options, bool includeDashboardUrl = false)
    {
        var data = resources.Where(resource => !resource.IsResourceHidden(false)).Select(resource =>
        {
            var resourceObj = new Dictionary<string, object?>
            {
                ["resource_name"] = resource.Name,
                ["type"] = resource.ResourceType,
                ["state"] = resource.State,
                ["state_description"] = ResourceStateViewModel.GetResourceStateTooltip(resource, s_columnsLoc),
                ["relationships"] = GetResourceRelationships(resources, resource),
                ["endpoint_urls"] = resource.Urls.Where(u => !u.IsInternal).Select(u => new
                {
                    name = u.EndpointName,
                    url = u.Url,
                    display_name = !string.IsNullOrEmpty(u.DisplayProperties.DisplayName) ? u.DisplayProperties.DisplayName : null,
                }).ToList(),
                ["health"] = new
                {
                    resource_health_status = GetResourceHealthStatus(resource),
                    health_reports = resource.HealthReports.Select(report => new
                    {
                        name = report.Name,
                        health_status = GetReportHealthStatus(resource, report),
                        exception = report.ExceptionText
                    }).ToList()
                },
                ["source"] = ResourceSourceViewModel.GetSourceViewModel(resource)?.Value,
                ["commands"] = resource.Commands.Where(cmd => cmd.State == CommandViewModelState.Enabled).Select(cmd => new
                {
                    name = cmd.Name,
                    description = cmd.GetDisplayDescription(s_commandsLoc)
                }).ToList()
            };

            if (includeDashboardUrl)
            {
                resourceObj["dashboard_link"] = GetDashboardLink(options, DashboardUrls.ResourcesUrl(resource: resource.Name), resource.Name);
            }

            return resourceObj;
        }).ToList();

        var resourceGraphData = SerializeJson(data);
        return resourceGraphData;

        static List<object> GetResourceRelationships(List<ResourceViewModel> allResources, ResourceViewModel resourceViewModel)
        {
            var relationships = new List<object>();

            foreach (var relationship in resourceViewModel.Relationships)
            {
                var matches = allResources
                    .Where(r => string.Equals(r.DisplayName, relationship.ResourceName, StringComparisons.ResourceName))
                    .Where(r => r.KnownState != KnownResourceState.Hidden)
                    .ToList();

                foreach (var match in matches)
                {
                    relationships.Add(new
                    {
                        resource_name = match.Name,
                        Types = relationship.Type
                    });
                }
            }

            return relationships;
        }

        static string? GetResourceHealthStatus(ResourceViewModel resource)
        {
            if (resource.HealthReports.Length == 0)
            {
                return "No health reports specified";
            }

            if (resource.HealthStatus == null && !resource.IsRunningState())
            {
                return $"Health reports aren't evaluated until the resource is in a {KnownResourceState.Running} state";
            }

            return resource.HealthStatus?.ToString();
        }

        static string? GetReportHealthStatus(ResourceViewModel resource, HealthReportViewModel report)
        {
            if (report.HealthStatus == null && !resource.IsRunningState())
            {
                return $"Health reports aren't evaluated until the resource is in a {KnownResourceState.Running} state";
            }

            return report.HealthStatus?.ToString();
        }
    }

    public static object? GetDashboardLink(DashboardOptions options, string path, string text)
    {
        var url = GetDashboardUrl(options, path);
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        return new
        {
            url = url,
            text = text
        };
    }

    public static string? GetDashboardUrl(DashboardOptions options, string path)
    {
        var frontendEndpoints = options.Frontend.GetEndpointAddresses();

        var frontendUrl = options.Frontend.PublicUrl
            ?? frontendEndpoints.FirstOrDefault(e => string.Equals(e.Scheme, "https", StringComparison.Ordinal))?.ToString()
            ?? frontendEndpoints.FirstOrDefault(e => string.Equals(e.Scheme, "http", StringComparison.Ordinal))?.ToString();

        if (frontendUrl == null)
        {
            return null;
        }

        return new Uri(new Uri(frontendUrl), path).ToString();
    }

    public static int EstimateTokenCount(string text)
    {
        // This is a rough estimate of the number of tokens in the text.
        // If the exact value is needed then use a library to calculate.
        return text.Length / 4;
    }

    public static int EstimateSerializedJsonTokenSize<T>(T value)
    {
        var json = SerializeJson(value);
        return EstimateTokenCount(json);
    }

    private static string SerializeJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, s_jsonSerializerOptions);
    }

    public static (string json, string limitMessage) GetStructuredLogsJson(List<OtlpLogEntry> errorLogs, DashboardOptions options, bool includeDashboardUrl = false)
    {
        var promptContext = new PromptContext();
        var (trimmedItems, limitMessage) = GetLimitFromEndWithSummary(
            errorLogs,
            StructuredLogsLimit,
            "log entry",
            i => GetLogEntryDto(i, promptContext, options, includeDashboardUrl),
            EstimateSerializedJsonTokenSize);
        var logsData = SerializeJson(trimmedItems);

        return (logsData, limitMessage);
    }

    internal static string GetStructuredLogJson(OtlpLogEntry l, DashboardOptions options, bool includeDashboardUrl = false)
    {
        var dto = GetLogEntryDto(l, new PromptContext(), options, includeDashboardUrl);

        var json = SerializeJson(dto);
        return json;
    }

    public static object GetLogEntryDto(OtlpLogEntry l, PromptContext context, DashboardOptions options, bool includeDashboardUrl = false)
    {
        var exceptionText = OtlpLogEntry.GetExceptionText(l);

        var log = new Dictionary<string, object?>
        {
            ["log_id"] = l.InternalId,
            ["span_id"] = OtlpHelpers.ToShortenedId(l.SpanId),
            ["trace_id"] = OtlpHelpers.ToShortenedId(l.TraceId),
            ["message"] = context.AddValue(l.Message, id => $@"Duplicate of ""message"" for log entry {id.InternalId}", l),
            ["severity"] = l.Severity.ToString(),
            ["resource_name"] = l.ResourceView.Resource.ResourceKey.GetCompositeName(),
            ["attributes"] = l.Attributes
                .Where(l => l.Key is not (OtlpLogEntry.ExceptionStackTraceField or OtlpLogEntry.ExceptionMessageField or OtlpLogEntry.ExceptionTypeField))
                .ToDictionary(a => a.Key, a => context.AddValue(MapOtelAttributeValue(a), id => $@"Duplicate of attribute ""{id.Key}"" for log entry {id.InternalId}", (l.InternalId, a.Key))),
            ["exception"] = context.AddValue(exceptionText, id => $@"Duplicate of ""exception"" for log entry {id.InternalId}", l),
            ["source"] = l.Scope.Name
        };

        if (includeDashboardUrl)
        {
            log["dashboard_link"] = GetDashboardLink(options, DashboardUrls.StructuredLogsUrl(logEntryId: l.InternalId), $"log_id: {l.InternalId}");
        }

        return log;
    }

    public static string SerializeConsoleLogs(IList<string> logEntries)
    {
        var consoleLogsText = new StringBuilder();

        foreach (var logEntry in logEntries)
        {
            consoleLogsText.AppendLine(logEntry);
        }

        return consoleLogsText.ToString();
    }

    public static string SerializeLogEntry(LogEntry logEntry)
    {
        if (logEntry.RawContent is not null)
        {
            var content = logEntry.RawContent;
            if (TimestampParser.TryParseConsoleTimestamp(content, out var timestampParseResult))
            {
                content = timestampParseResult.Value.ModifiedText;
            }

            return LimitLength(AnsiParser.StripControlSequences(content));
        }
        else
        {
            return string.Empty;
        }
    }

    public static bool TryGetSingleResult<T>(IEnumerable<T> source, Func<T, bool> predicate, [NotNullWhen(true)] out T? result)
    {
        result = default;
        var found = false;

        foreach (var item in source)
        {
            if (predicate(item))
            {
                if (found)
                {
                    // Multiple results found
                    result = default;
                    return false;
                }

                result = item;
                found = true;
            }
        }

        return found;
    }

    public static bool TryGetResource(IReadOnlyList<OtlpResource> resources, string resourceName, [NotNullWhen(true)] out OtlpResource? resource)
    {
        if (TryGetSingleResult(resources, r => r.ResourceName == resourceName, out resource))
        {
            return true;
        }
        else if (TryGetSingleResult(resources, r => r.ResourceKey.ToString() == resourceName, out resource))
        {
            return true;
        }

        resource = null;
        return false;
    }

    public static bool TryGetResource(IReadOnlyList<ResourceViewModel> resources, string resourceName, [NotNullWhen(true)] out ResourceViewModel? resource)
    {
        if (TryGetSingleResult(resources, r => r.Name == resourceName, out resource))
        {
            return true;
        }
        else if (TryGetSingleResult(resources, r => r.DisplayName == resourceName, out resource))
        {
            return true;
        }

        resource = null;
        return false;
    }

    internal static async Task ExecuteStreamingCallAsync(
        IChatClient client,
        List<ChatMessage> chatMessages,
        Func<string, Task> textUpdateCallback,
        Func<IList<ChatMessage>, Task> onMessageCallback,
        int maximumResponseLength,
        AIFunction[] tools,
        CancellationTokenSource responseCts)
    {
        var chatOptions = new ChatOptions
        {
            Tools = tools
        };

        // This CTS is used to cancel the response stream if it takes too long to respond.
        // The timeout is reset each time a response update is received.
        var messageCts = new CancellationTokenSource();
        messageCts.Token.Register(responseCts.Cancel);
        if (!Debugger.IsAttached)
        {
            messageCts.CancelAfter(ResponseMessageTimeout);
        }

        var response = client.GetStreamingResponseAsync(chatMessages, chatOptions, responseCts.Token);

        var responseLength = 0;
        await foreach (var update in response.WithCancellation(responseCts.Token).ConfigureAwait(false))
        {
            if (!Debugger.IsAttached)
            {
                // Reset the timeout for the next update.
                messageCts.CancelAfter(ResponseMessageTimeout);
            }

            var newMessages = GetMessages(update, filter: c => c is not TextContent);
            if (newMessages.Count > 0)
            {
                await onMessageCallback(newMessages).ConfigureAwait(false);
            }

            foreach (var item in update.Contents.OfType<TextContent>())
            {
                if (!string.IsNullOrEmpty(item.Text))
                {
                    responseLength += item.Text.Length;

                    if (responseLength > maximumResponseLength)
                    {
                        throw new InvalidOperationException("Response exceeds maximum length.");
                    }

                    await textUpdateCallback(item.Text).ConfigureAwait(false);
                }
            }
        }
    }

    public static IList<ChatMessage> GetMessages(ChatResponseUpdate update, Func<AIContent, bool>? filter = null)
    {
        var contentsList = filter is null ? update.Contents : update.Contents.Where(filter).ToList();
        if (contentsList.Count > 0)
        {
            var list = new List<ChatMessage>();

            list.Add(new ChatMessage(update.Role ?? ChatRole.Assistant, contentsList)
            {
                AuthorName = update.AuthorName,
                RawRepresentation = update.RawRepresentation,
                AdditionalProperties = update.AdditionalProperties,
            });

            return list;
        }

        return [];
    }

    public static bool IsMissingValue([NotNullWhen(false)] string? value)
    {
        // Models sometimes pass an string value of "null" instead of null.
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase);
    }

    public static string LimitLength(string value)
    {
        if (value.Length <= MaximumStringLength)
        {
            return value;
        }

        return
            $"""
            {value.AsSpan(0, MaximumStringLength)}...[TRUNCATED]
            """;
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
}
