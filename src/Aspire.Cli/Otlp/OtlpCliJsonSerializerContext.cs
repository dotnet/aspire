// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Otlp.Serialization;

namespace Aspire.Cli.Otlp;

/// <summary>
/// Represents the telemetry data returned by the Dashboard API.
/// Contains logs, traces, and/or metrics data.
/// </summary>
internal sealed class TelemetryDataJson
{
    [JsonPropertyName("resourceSpans")]
    public OtlpResourceSpansJson[]? ResourceSpans { get; set; }

    [JsonPropertyName("resourceLogs")]
    public OtlpResourceLogsJson[]? ResourceLogs { get; set; }
}

/// <summary>
/// Represents the API response wrapper for telemetry data.
/// </summary>
internal sealed class TelemetryApiResponse
{
    [JsonPropertyName("data")]
    public TelemetryDataJson? Data { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("returnedCount")]
    public int ReturnedCount { get; set; }
}

/// <summary>
/// Information about a resource that has telemetry data.
/// </summary>
internal sealed class ResourceInfoJson
{
    /// <summary>
    /// The base resource name (e.g., "catalogservice").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// The instance ID if this is a replica (e.g., "abc123"), or null if single instance.
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// The full display name including instance ID (e.g., "catalogservice-abc123" or "catalogservice").
    /// Use this when querying the telemetry API.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Whether this resource has structured logs.
    /// </summary>
    [JsonPropertyName("hasLogs")]
    public bool HasLogs { get; set; }

    /// <summary>
    /// Whether this resource has traces/spans.
    /// </summary>
    [JsonPropertyName("hasTraces")]
    public bool HasTraces { get; set; }

    /// <summary>
    /// Whether this resource has metrics.
    /// </summary>
    [JsonPropertyName("hasMetrics")]
    public bool HasMetrics { get; set; }
}

/// <summary>
/// Source-generated JSON serializer context for OTLP types used by CLI telemetry commands.
/// Provides AOT-compatible serialization for logs and trace types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(TelemetryApiResponse))]
[JsonSerializable(typeof(TelemetryDataJson))]
[JsonSerializable(typeof(ResourceInfoJson))]
[JsonSerializable(typeof(ResourceInfoJson[]))]
[JsonSerializable(typeof(OtlpAnyValueJson))]
[JsonSerializable(typeof(OtlpArrayValueJson))]
[JsonSerializable(typeof(OtlpKeyValueListJson))]
[JsonSerializable(typeof(OtlpKeyValueJson))]
[JsonSerializable(typeof(OtlpInstrumentationScopeJson))]
[JsonSerializable(typeof(OtlpEntityRefJson))]
[JsonSerializable(typeof(OtlpResourceJson))]
[JsonSerializable(typeof(OtlpResourceSpansJson))]
[JsonSerializable(typeof(OtlpScopeSpansJson))]
[JsonSerializable(typeof(OtlpSpanJson))]
[JsonSerializable(typeof(OtlpSpanEventJson))]
[JsonSerializable(typeof(OtlpSpanLinkJson))]
[JsonSerializable(typeof(OtlpSpanStatusJson))]
[JsonSerializable(typeof(OtlpExportTraceServiceRequestJson))]
[JsonSerializable(typeof(OtlpResourceLogsJson))]
[JsonSerializable(typeof(OtlpScopeLogsJson))]
[JsonSerializable(typeof(OtlpLogRecordJson))]
[JsonSerializable(typeof(OtlpExportLogsServiceRequestJson))]
internal sealed partial class OtlpCliJsonSerializerContext : JsonSerializerContext
{
}
