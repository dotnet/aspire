// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Otlp.Model.Serialization;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Service for exporting telemetry and console logs data.
/// </summary>
public sealed class TelemetryExportService
{
    private readonly TelemetryRepository _telemetryRepository;
    private readonly ConsoleLogsFetcher _consoleLogsFetcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryExportService"/> class.
    /// </summary>
    /// <param name="telemetryRepository">The telemetry repository.</param>
    /// <param name="consoleLogsFetcher">The console log fetcher.</param>
    public TelemetryExportService(TelemetryRepository telemetryRepository, ConsoleLogsFetcher consoleLogsFetcher)
    {
        _telemetryRepository = telemetryRepository;
        _consoleLogsFetcher = consoleLogsFetcher;
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

    private async Task ExportConsoleLogsAsync(ZipArchive archive, HashSet<string> resourceNames, CancellationToken cancellationToken)
    {
        if (!_consoleLogsFetcher.IsEnabled)
        {
            return;
        }

        var allLogEntries = await _consoleLogsFetcher.FetchLogEntriesAsync(resourceNames, cancellationToken).ConfigureAwait(false);

        // Write results to archive sequentially (ZipArchive is not thread-safe)
        foreach (var (resourceName, logEntries) in allLogEntries)
        {
            var entry = archive.CreateEntry($"consolelogs/{SanitizeFileName(resourceName)}.txt");
            using var entryStream = entry.Open();
            LogEntrySerializer.WriteLogEntriesToStream(logEntries, entryStream);
        }
    }

    private void ExportStructuredLogs(ZipArchive archive, List<OtlpResource> resources)
    {
        foreach (var resource in resources)
        {
            var logs = _telemetryRepository.GetLogs(GetLogsContext.ForResourceKey(resource.ResourceKey));

            if (logs.Items.Count == 0)
            {
                continue;
            }

            var resourceName = OtlpResource.GetResourceName(resource, resources);
            var logsJson = ConvertLogsToOtlpJson(logs.Items);
            WriteJsonToArchive(archive, $"structuredlogs/{SanitizeFileName(resourceName)}.json", logsJson);
        }
    }

    private void ExportTraces(ZipArchive archive, List<OtlpResource> resources)
    {
        foreach (var resource in resources)
        {
            var tracesResponse = _telemetryRepository.GetTraces(GetTracesRequest.ForResourceKey(resource.ResourceKey));

            if (tracesResponse.PagedResult.Items.Count == 0)
            {
                continue;
            }

            var resourceName = OtlpResource.GetResourceName(resource, resources);
            var tracesJson = ConvertTracesToOtlpJson(tracesResponse.PagedResult.Items);
            WriteJsonToArchive(archive, $"traces/{SanitizeFileName(resourceName)}.json", tracesJson);
        }
    }

    private void ExportMetrics(ZipArchive archive, List<OtlpResource> resources)
    {
        foreach (var resource in resources)
        {
            var instrumentSummaries = _telemetryRepository.GetInstrumentsSummaries(resource.ResourceKey);

            if (instrumentSummaries.Count == 0)
            {
                continue;
            }

            // Get full instrument data with values for each instrument
            var instrumentsData = new List<OtlpInstrumentData>();
            foreach (var summary in instrumentSummaries)
            {
                var instrumentData = _telemetryRepository.GetInstrument(new GetInstrumentRequest
                {
                    ResourceKey = resource.ResourceKey,
                    MeterName = summary.Parent.Name,
                    InstrumentName = summary.Name,
                    StartTime = DateTime.MinValue,
                    EndTime = DateTime.MaxValue
                });

                if (instrumentData is not null)
                {
                    instrumentsData.Add(instrumentData);
                }
            }

            if (instrumentsData.Count == 0)
            {
                continue;
            }

            var resourceName = OtlpResource.GetResourceName(resource, resources);
            var metricsJson = ConvertMetricsToOtlpJson(resource, instrumentsData);
            WriteJsonToArchive(archive, $"metrics/{SanitizeFileName(resourceName)}.json", metricsJson);
        }
    }

    internal static OtlpTelemetryDataJson ConvertLogsToOtlpJson(List<OtlpLogEntry> logs)
    {
        // Group logs by resource and scope
        var resourceLogs = logs
            .GroupBy(l => l.ResourceView.ResourceKey)
            .Select(resourceGroup =>
            {
                var firstLog = resourceGroup.First();
                return new OtlpResourceLogsJson
                {
                    Resource = ConvertResourceView(firstLog.ResourceView),
                    ScopeLogs = resourceGroup
                        .GroupBy(l => l.Scope)
                        .Select(scopeGroup => new OtlpScopeLogsJson
                        {
                            Scope = ConvertScope(scopeGroup.Key),
                            LogRecords = scopeGroup.Select(ConvertLogEntry).ToArray()
                        }).ToArray()
                };
            }).ToArray();

        return new OtlpTelemetryDataJson
        {
            ResourceLogs = resourceLogs
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

    internal static OtlpTelemetryDataJson ConvertTracesToOtlpJson(IReadOnlyList<OtlpTrace> traces)
    {
        // Group spans by resource and scope
        var allSpans = traces.SelectMany(t => t.Spans).ToList();
        var resourceSpans = allSpans
            .GroupBy(s => s.Source.ResourceKey)
            .Select(resourceGroup =>
            {
                var firstSpan = resourceGroup.First();
                return new OtlpResourceSpansJson
                {
                    Resource = ConvertResourceView(firstSpan.Source),
                    ScopeSpans = resourceGroup
                        .GroupBy(s => s.Scope)
                        .Select(scopeGroup => new OtlpScopeSpansJson
                        {
                            Scope = ConvertScope(scopeGroup.Key),
                            Spans = scopeGroup.Select(ConvertSpan).ToArray()
                        }).ToArray()
                };
            }).ToArray();

        return new OtlpTelemetryDataJson
        {
            ResourceSpans = resourceSpans
        };
    }

    internal static string ConvertSpanToJson(OtlpSpan span, List<OtlpLogEntry>? logs = null)
    {
        var data = new OtlpTelemetryDataJson
        {
            ResourceSpans =
            [
                new OtlpResourceSpansJson
                {
                    Resource = ConvertResourceView(span.Source),
                    ScopeSpans =
                    [
                        new OtlpScopeSpansJson
                        {
                            Scope = ConvertScope(span.Scope),
                            Spans = [ConvertSpan(span)]
                        }
                    ]
                }
            ],
            ResourceLogs = ConvertLogsToResourceLogs(logs)
        };
        return JsonSerializer.Serialize(data, OtlpJsonSerializerContext.IndentedOptions);
    }

    internal static string ConvertTraceToJson(OtlpTrace trace, List<OtlpLogEntry>? logs = null)
    {
        // Group spans by resource and scope
        var spansByResourceAndScope = trace.Spans
            .GroupBy(s => s.Source.ResourceKey)
            .Select(resourceGroup =>
            {
                var firstSpan = resourceGroup.First();
                return new OtlpResourceSpansJson
                {
                    Resource = ConvertResourceView(firstSpan.Source),
                    ScopeSpans = resourceGroup
                        .GroupBy(s => s.Scope)
                        .Select(scopeGroup => new OtlpScopeSpansJson
                        {
                            Scope = ConvertScope(scopeGroup.Key),
                            Spans = scopeGroup.Select(ConvertSpan).ToArray()
                        }).ToArray()
                };
            }).ToArray();

        var data = new OtlpTelemetryDataJson
        {
            ResourceSpans = spansByResourceAndScope,
            ResourceLogs = ConvertLogsToResourceLogs(logs)
        };
        return JsonSerializer.Serialize(data, OtlpJsonSerializerContext.IndentedOptions);
    }

    internal static string ConvertLogEntryToJson(OtlpLogEntry logEntry)
    {
        var data = new OtlpTelemetryDataJson
        {
            ResourceLogs =
            [
                new OtlpResourceLogsJson
                {
                    Resource = ConvertResourceView(logEntry.ResourceView),
                    ScopeLogs =
                    [
                        new OtlpScopeLogsJson
                        {
                            Scope = ConvertScope(logEntry.Scope),
                            LogRecords = [ConvertLogEntry(logEntry)]
                        }
                    ]
                }
            ]
        };
        return JsonSerializer.Serialize(data, OtlpJsonSerializerContext.IndentedOptions);
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

    private static OtlpResourceLogsJson[]? ConvertLogsToResourceLogs(List<OtlpLogEntry>? logs)
    {
        if (logs is null || logs.Count == 0)
        {
            return null;
        }

        // Group logs by resource and scope
        return logs
            .GroupBy(l => l.ResourceView.ResourceKey)
            .Select(resourceGroup =>
            {
                var firstLog = resourceGroup.First();
                return new OtlpResourceLogsJson
                {
                    Resource = ConvertResourceView(firstLog.ResourceView),
                    ScopeLogs = resourceGroup
                        .GroupBy(l => l.Scope)
                        .Select(scopeGroup => new OtlpScopeLogsJson
                        {
                            Scope = ConvertScope(scopeGroup.Key),
                            LogRecords = scopeGroup.Select(ConvertLogEntry).ToArray()
                        }).ToArray()
                };
            }).ToArray();
    }

    internal static OtlpTelemetryDataJson ConvertMetricsToOtlpJson(OtlpResource resource, List<OtlpInstrumentData> instruments)
    {
        // Group instruments by scope
        var instrumentsByScope = instruments.GroupBy(i => i.Summary.Parent);

        var scopeMetrics = instrumentsByScope.Select(scopeGroup => new OtlpScopeMetricsJson
        {
            Scope = ConvertScope(scopeGroup.Key),
            Metrics = scopeGroup.Select(ConvertInstrument).ToArray()
        }).ToArray();

        return new OtlpTelemetryDataJson
        {
            ResourceMetrics =
            [
                new OtlpResourceMetricsJson
                {
                    Resource = ConvertResourceView(resource.GetViews()[0]),
                    ScopeMetrics = scopeMetrics
                }
            ]
        };
    }

    private static OtlpMetricJson ConvertInstrument(OtlpInstrumentData instrumentData)
    {
        var summary = instrumentData.Summary;
        var metric = new OtlpMetricJson
        {
            Name = summary.Name,
            Description = summary.Description,
            Unit = summary.Unit
        };

        // Convert dimensions to data points based on metric type
        switch (summary.Type)
        {
            case OtlpInstrumentType.Gauge:
                metric.Gauge = new OtlpGaugeJson
                {
                    DataPoints = ConvertNumberDataPoints(instrumentData.Dimensions)
                };
                break;
            case OtlpInstrumentType.Sum:
                metric.Sum = new OtlpSumJson
                {
                    DataPoints = ConvertNumberDataPoints(instrumentData.Dimensions),
                    AggregationTemporality = (int)summary.AggregationTemporality
                };
                break;
            case OtlpInstrumentType.Histogram:
                metric.Histogram = new OtlpHistogramJson
                {
                    DataPoints = ConvertHistogramDataPoints(instrumentData.Dimensions),
                    AggregationTemporality = (int)summary.AggregationTemporality
                };
                break;
        }

        return metric;
    }

    private static OtlpNumberDataPointJson[] ConvertNumberDataPoints(List<DimensionScope> dimensions)
    {
        var dataPoints = new List<OtlpNumberDataPointJson>();

        foreach (var dimension in dimensions)
        {
            foreach (var value in dimension.Values)
            {
                var dataPoint = new OtlpNumberDataPointJson
                {
                    Attributes = ConvertAttributes(dimension.Attributes),
                    StartTimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(value.Start),
                    TimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(value.End),
                    Exemplars = value.HasExemplars ? ConvertExemplars(value.Exemplars) : null
                };

                // Set the value based on the metric value type
                if (value is MetricValue<long> longValue)
                {
                    dataPoint.AsInt = longValue.Value;
                }
                else if (value is MetricValue<double> doubleValue)
                {
                    dataPoint.AsDouble = doubleValue.Value;
                }

                dataPoints.Add(dataPoint);
            }
        }

        return dataPoints.ToArray();
    }

    private static OtlpHistogramDataPointJson[] ConvertHistogramDataPoints(List<DimensionScope> dimensions)
    {
        var dataPoints = new List<OtlpHistogramDataPointJson>();

        foreach (var dimension in dimensions)
        {
            foreach (var value in dimension.Values)
            {
                if (value is not HistogramValue histogramValue)
                {
                    continue;
                }

                var dataPoint = new OtlpHistogramDataPointJson
                {
                    Attributes = ConvertAttributes(dimension.Attributes),
                    StartTimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(value.Start),
                    TimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(value.End),
                    Count = histogramValue.Count,
                    Sum = histogramValue.Sum,
                    BucketCounts = histogramValue.Values.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray(),
                    ExplicitBounds = histogramValue.ExplicitBounds,
                    Exemplars = value.HasExemplars ? ConvertExemplars(value.Exemplars) : null
                };

                dataPoints.Add(dataPoint);
            }
        }

        return dataPoints.ToArray();
    }

    private static OtlpExemplarJson[] ConvertExemplars(List<MetricsExemplar> exemplars)
    {
        return exemplars.Select(e => new OtlpExemplarJson
        {
            TimeUnixNano = OtlpHelpers.DateTimeToUnixNanoseconds(e.Start),
            AsDouble = e.Value,
            SpanId = e.SpanId,
            TraceId = e.TraceId,
            FilteredAttributes = ConvertAttributes(e.Attributes)
        }).ToArray();
    }

    private static OtlpResourceJson ConvertResourceView(OtlpResourceView resourceView)
    {
        var attributes = new List<OtlpKeyValueJson>
        {
            new OtlpKeyValueJson
            {
                Key = OtlpResource.SERVICE_NAME,
                Value = new OtlpAnyValueJson { StringValue = resourceView.Resource.ResourceName }
            },
            new OtlpKeyValueJson
            {
                Key = OtlpResource.SERVICE_INSTANCE_ID,
                Value = new OtlpAnyValueJson { StringValue = resourceView.Resource.InstanceId }
            }
        };

        // Include additional properties from the resource view
        foreach (var property in resourceView.Properties)
        {
            attributes.Add(new OtlpKeyValueJson
            {
                Key = property.Key,
                Value = new OtlpAnyValueJson { StringValue = property.Value }
            });
        }

        return new OtlpResourceJson
        {
            Attributes = attributes.ToArray()
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
