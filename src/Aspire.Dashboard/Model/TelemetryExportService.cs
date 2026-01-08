// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.Serialization;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Service for exporting telemetry and console logs data.
/// </summary>
public sealed class TelemetryExportService
{
    private readonly TelemetryRepository _telemetryRepository;
    private readonly IDashboardClient _dashboardClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryExportService"/> class.
    /// </summary>
    /// <param name="telemetryRepository">The telemetry repository.</param>
    /// <param name="dashboardClient">The dashboard client.</param>
    public TelemetryExportService(TelemetryRepository telemetryRepository, IDashboardClient dashboardClient)
    {
        _telemetryRepository = telemetryRepository;
        _dashboardClient = dashboardClient;
    }

    /// <summary>
    /// Exports selected telemetry and console logs as a zip archive stream.
    /// </summary>
    /// <param name="selectedResources">Dictionary mapping resource names to the data types to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A memory stream containing the zip archive.</returns>
    public async Task<MemoryStream> ExportSelectedAsync(Dictionary<string, HashSet<AspireDataType>> selectedResources, CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var allOtlpResources = _telemetryRepository.GetResources();

            // Filter to selected resources with matching data types
            var consoleLogResources = selectedResources
                .Where(kvp => kvp.Value.Contains(AspireDataType.ConsoleLogs))
                .Select(kvp => kvp.Key)
                .ToHashSet(StringComparers.ResourceName);

            var structuredLogResources = allOtlpResources
                .Where(r => selectedResources.TryGetValue(r.ResourceKey.GetCompositeName(), out var types) && types.Contains(AspireDataType.StructuredLogs))
                .ToList();

            var traceResources = allOtlpResources
                .Where(r => selectedResources.TryGetValue(r.ResourceKey.GetCompositeName(), out var types) && types.Contains(AspireDataType.Traces))
                .ToList();

            var metricsResources = allOtlpResources
                .Where(r => selectedResources.TryGetValue(r.ResourceKey.GetCompositeName(), out var types) && types.Contains(AspireDataType.Metrics))
                .ToList();

            // Export console logs for selected resources
            if (consoleLogResources.Count > 0)
            {
                await ExportConsoleLogsAsync(archive, consoleLogResources, cancellationToken).ConfigureAwait(false);
            }

            // Export structured logs (OTLP JSON)
            if (structuredLogResources.Count > 0)
            {
                ExportStructuredLogs(archive, structuredLogResources);
            }

            // Export traces (OTLP JSON)
            if (traceResources.Count > 0)
            {
                ExportTraces(archive, traceResources);
            }

            // Export metrics (OTLP JSON)
            if (metricsResources.Count > 0)
            {
                ExportMetrics(archive, metricsResources);
            }
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private async Task ExportConsoleLogsAsync(ZipArchive archive, HashSet<string>? resourceFilter, CancellationToken cancellationToken)
    {
        if (!_dashboardClient.IsEnabled)
        {
            return;
        }

        var resources = _dashboardClient.GetResources();

        // Filter resources if a filter is provided
        if (resourceFilter is not null)
        {
            resources = resources.Where(r => resourceFilter.Contains(r.Name)).ToList();
        }

        // Fetch console logs for all resources in parallel
        var logTasks = resources.Select(async resource =>
        {
            var logs = new StringBuilder();

            await foreach (var logBatch in _dashboardClient.GetConsoleLogs(resource.Name, cancellationToken).ConfigureAwait(false))
            {
                foreach (var logLine in logBatch)
                {
                    logs.AppendLine(logLine.Content);
                }
            }

            return (Resource: resource, Logs: logs);
        });

        var results = await Task.WhenAll(logTasks).ConfigureAwait(false);

        // Write results to archive sequentially (ZipArchive is not thread-safe)
        foreach (var (resource, logs) in results)
        {
            if (logs.Length > 0)
            {
                var resourceName = ResourceViewModel.GetResourceName(resource, _dashboardClient.GetResources());
                var entry = archive.CreateEntry($"consolelogs/{SanitizeFileName(resourceName)}.txt");
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(logs.ToString()).ConfigureAwait(false);
            }
        }
    }

    private void ExportStructuredLogs(ZipArchive archive, List<OtlpResource> resources)
    {
        foreach (var resource in resources)
        {
            var logs = _telemetryRepository.GetLogs(new GetLogsContext
            {
                ResourceKey = resource.ResourceKey,
                StartIndex = 0,
                Count = int.MaxValue,
                Filters = []
            });

            if (logs.Items.Count == 0)
            {
                continue;
            }

            var resourceName = OtlpResource.GetResourceName(resource, resources);
            var logsJson = ConvertLogsToOtlpJson(resource, logs.Items);
            WriteJsonToArchive(archive, $"structuredlogs/{SanitizeFileName(resourceName)}.json", logsJson);
        }
    }

    private void ExportTraces(ZipArchive archive, List<OtlpResource> resources)
    {
        foreach (var resource in resources)
        {
            var tracesResponse = _telemetryRepository.GetTraces(new GetTracesRequest
            {
                ResourceKey = resource.ResourceKey,
                StartIndex = 0,
                Count = int.MaxValue,
                FilterText = string.Empty,
                Filters = []
            });

            if (tracesResponse.PagedResult.Items.Count == 0)
            {
                continue;
            }

            var resourceName = OtlpResource.GetResourceName(resource, resources);
            var tracesJson = ConvertTracesToOtlpJson(resource, tracesResponse.PagedResult.Items);
            WriteJsonToArchive(archive, $"traces/{SanitizeFileName(resourceName)}.json", tracesJson);
        }
    }

    private void ExportMetrics(ZipArchive archive, List<OtlpResource> resources)
    {
        foreach (var resource in resources)
        {
            var instruments = _telemetryRepository.GetInstrumentsSummaries(resource.ResourceKey);

            if (instruments.Count == 0)
            {
                continue;
            }

            var resourceName = OtlpResource.GetResourceName(resource, resources);
            var metricsJson = ConvertMetricsToOtlpJson(resource, instruments);
            WriteJsonToArchive(archive, $"metrics/{SanitizeFileName(resourceName)}.json", metricsJson);
        }
    }

    internal static OtlpLogsDataJson ConvertLogsToOtlpJson(OtlpResource resource, IReadOnlyList<OtlpLogEntry> logs)
    {
        // Group logs by scope
        var logsByScope = logs.GroupBy(l => l.Scope);

        var scopeLogs = logsByScope.Select(scopeGroup => new OtlpScopeLogsJson
        {
            Scope = ConvertScope(scopeGroup.Key),
            LogRecords = scopeGroup.Select(ConvertLogEntry).ToArray()
        }).ToArray();

        return new OtlpLogsDataJson
        {
            ResourceLogs =
            [
                new OtlpResourceLogsJson
                {
                    Resource = ConvertResource(resource),
                    ScopeLogs = scopeLogs
                }
            ]
        };
    }

    private static OtlpLogRecordJson ConvertLogEntry(OtlpLogEntry log)
    {
        return new OtlpLogRecordJson
        {
            TimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(log.TimeStamp),
            SeverityNumber = log.SeverityNumber,
            SeverityText = log.Severity.ToString(),
            Body = new OtlpAnyValueJson { StringValue = log.Message },
            Attributes = ConvertAttributes(log.Attributes),
            TraceId = string.IsNullOrEmpty(log.TraceId) ? null : log.TraceId,
            SpanId = string.IsNullOrEmpty(log.SpanId) ? null : log.SpanId,
            Flags = log.Flags,
            EventName = log.EventName
        };
    }

    internal static OtlpTracesDataJson ConvertTracesToOtlpJson(OtlpResource resource, IReadOnlyList<OtlpTrace> traces)
    {
        // Group spans by scope
        var allSpans = traces.SelectMany(t => t.Spans).ToList();
        var spansByScope = allSpans.GroupBy(s => s.Scope);

        var scopeSpans = spansByScope.Select(scopeGroup => new OtlpScopeSpansJson
        {
            Scope = ConvertScope(scopeGroup.Key),
            Spans = scopeGroup.Select(ConvertSpan).ToArray()
        }).ToArray();

        return new OtlpTracesDataJson
        {
            ResourceSpans =
            [
                new OtlpResourceSpansJson
                {
                    Resource = ConvertResource(resource),
                    ScopeSpans = scopeSpans
                }
            ]
        };
    }

    private static OtlpSpanJson ConvertSpan(OtlpSpan span)
    {
        return new OtlpSpanJson
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            ParentSpanId = string.IsNullOrEmpty(span.ParentSpanId) ? null : span.ParentSpanId,
            Name = span.Name,
            Kind = (int)span.Kind,
            StartTimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(span.StartTime),
            EndTimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(span.EndTime),
            Attributes = ConvertAttributes(span.Attributes),
            Status = ConvertSpanStatus(span.Status, span.StatusMessage),
            Events = span.Events.Count > 0 ? span.Events.Select(ConvertSpanEvent).ToArray() : null,
            Links = span.Links.Count > 0 ? span.Links.Select(ConvertSpanLink).ToArray() : null,
            TraceState = span.State
        };
    }

    private static OtlpSpanStatusJson? ConvertSpanStatus(OtlpSpanStatusCode status, string? statusMessage)
    {
        if (status == OtlpSpanStatusCode.Unset && string.IsNullOrEmpty(statusMessage))
        {
            return null;
        }

        return new OtlpSpanStatusJson
        {
            Code = (int)status,
            Message = statusMessage
        };
    }

    private static OtlpSpanEventJson ConvertSpanEvent(OtlpSpanEvent evt)
    {
        return new OtlpSpanEventJson
        {
            TimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(evt.Time),
            Name = evt.Name,
            Attributes = ConvertAttributes(evt.Attributes)
        };
    }

    private static OtlpSpanLinkJson ConvertSpanLink(OtlpSpanLink link)
    {
        return new OtlpSpanLinkJson
        {
            TraceId = link.TraceId,
            SpanId = link.SpanId,
            TraceState = link.TraceState,
            Attributes = ConvertAttributes(link.Attributes)
        };
    }

    internal static OtlpMetricsDataJson ConvertMetricsToOtlpJson(OtlpResource resource, List<OtlpInstrumentSummary> instruments)
    {
        // Group instruments by scope
        var instrumentsByScope = instruments.GroupBy(i => i.Parent);

        var scopeMetrics = instrumentsByScope.Select(scopeGroup => new OtlpScopeMetricsJson
        {
            Scope = ConvertScope(scopeGroup.Key),
            Metrics = scopeGroup.Select(ConvertInstrument).ToArray()
        }).ToArray();

        return new OtlpMetricsDataJson
        {
            ResourceMetrics =
            [
                new OtlpResourceMetricsJson
                {
                    Resource = ConvertResource(resource),
                    ScopeMetrics = scopeMetrics
                }
            ]
        };
    }

    private static OtlpMetricJson ConvertInstrument(OtlpInstrumentSummary instrument)
    {
        // We only export the summary information since we don't have access to the raw data points
        return new OtlpMetricJson
        {
            Name = instrument.Name,
            Description = instrument.Description,
            Unit = instrument.Unit
        };
    }

    private static OtlpResourceJson ConvertResource(OtlpResource resource)
    {
        return new OtlpResourceJson
        {
            Attributes =
            [
                new OtlpKeyValueJson
                {
                    Key = OtlpResource.SERVICE_NAME,
                    Value = new OtlpAnyValueJson { StringValue = resource.ResourceName }
                },
                new OtlpKeyValueJson
                {
                    Key = OtlpResource.SERVICE_INSTANCE_ID,
                    Value = new OtlpAnyValueJson { StringValue = resource.InstanceId }
                }
            ]
        };
    }

    private static OtlpInstrumentationScopeJson ConvertScope(OtlpScope scope)
    {
        return new OtlpInstrumentationScopeJson
        {
            Name = scope.Name,
            Version = string.IsNullOrEmpty(scope.Version) ? null : scope.Version,
            Attributes = scope.Attributes.Length > 0 ? ConvertAttributes(scope.Attributes) : null
        };
    }

    private static OtlpKeyValueJson[]? ConvertAttributes(KeyValuePair<string, string>[] attributes)
    {
        if (attributes.Length == 0)
        {
            return null;
        }

        return attributes.Select(a => new OtlpKeyValueJson
        {
            Key = a.Key,
            Value = new OtlpAnyValueJson { StringValue = a.Value }
        }).ToArray();
    }

    private static void WriteJsonToArchive<T>(ZipArchive archive, string path, T data)
    {
        var entry = archive.CreateEntry(path);
        using var entryStream = entry.Open();
        JsonSerializer.Serialize(entryStream, data, OtlpJsonSerializerContext.IndentedOptions);
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            sanitized.Append(invalidChars.Contains(c) ? '_' : c);
        }

        return sanitized.ToString();
    }
}
