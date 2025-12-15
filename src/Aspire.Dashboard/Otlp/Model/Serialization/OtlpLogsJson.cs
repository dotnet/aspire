// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Otlp.Model.Serialization;

/// <summary>
/// Represents the logs data in OTLP JSON format.
/// </summary>
internal sealed class OtlpLogsDataJson
{
    /// <summary>
    /// An array of ResourceLogs.
    /// </summary>
    [JsonPropertyName("resourceLogs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpResourceLogsJson[]? ResourceLogs { get; set; }
}

/// <summary>
/// Represents a collection of ScopeLogs from a Resource.
/// </summary>
internal sealed class OtlpResourceLogsJson
{
    /// <summary>
    /// The resource for the logs in this message.
    /// </summary>
    [JsonPropertyName("resource")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpResourceJson? Resource { get; set; }

    /// <summary>
    /// A list of ScopeLogs that originate from a resource.
    /// </summary>
    [JsonPropertyName("scopeLogs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpScopeLogsJson[]? ScopeLogs { get; set; }

    /// <summary>
    /// The Schema URL, if known.
    /// </summary>
    [JsonPropertyName("schemaUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SchemaUrl { get; set; }
}

/// <summary>
/// Represents a collection of Logs produced by a Scope.
/// </summary>
internal sealed class OtlpScopeLogsJson
{
    /// <summary>
    /// The instrumentation scope information for the logs in this message.
    /// </summary>
    [JsonPropertyName("scope")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpInstrumentationScopeJson? Scope { get; set; }

    /// <summary>
    /// A list of log records.
    /// </summary>
    [JsonPropertyName("logRecords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpLogRecordJson[]? LogRecords { get; set; }

    /// <summary>
    /// The Schema URL, if known.
    /// </summary>
    [JsonPropertyName("schemaUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SchemaUrl { get; set; }
}

/// <summary>
/// Represents a log record according to OpenTelemetry Log Data Model.
/// </summary>
internal sealed class OtlpLogRecordJson
{
    /// <summary>
    /// Time when the event occurred. Serialized as string per protojson spec for fixed64.
    /// </summary>
    [JsonPropertyName("timeUnixNano")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public ulong? TimeUnixNano { get; set; }

    /// <summary>
    /// Time when the event was observed. Serialized as string per protojson spec for fixed64.
    /// </summary>
    [JsonPropertyName("observedTimeUnixNano")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public ulong? ObservedTimeUnixNano { get; set; }

    /// <summary>
    /// Numerical value of the severity.
    /// </summary>
    [JsonPropertyName("severityNumber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(JsonStringEnumConverter<OtlpSeverityNumberJson>))]
    public OtlpSeverityNumberJson? SeverityNumber { get; set; }

    /// <summary>
    /// The severity text (also known as log level).
    /// </summary>
    [JsonPropertyName("severityText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SeverityText { get; set; }

    /// <summary>
    /// A value containing the body of the log record.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpAnyValueJson? Body { get; set; }

    /// <summary>
    /// Additional attributes that describe the specific event occurrence.
    /// </summary>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpKeyValueJson[]? Attributes { get; set; }

    /// <summary>
    /// The number of dropped attributes.
    /// </summary>
    [JsonPropertyName("droppedAttributesCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint DroppedAttributesCount { get; set; }

    /// <summary>
    /// Flags, a bit field. Serialized as string per protojson spec for fixed32.
    /// </summary>
    [JsonPropertyName("flags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public uint? Flags { get; set; }

    /// <summary>
    /// A unique identifier for a trace. Serialized as base64.
    /// </summary>
    [JsonPropertyName("traceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }

    /// <summary>
    /// A unique identifier for a span within a trace. Serialized as base64.
    /// </summary>
    [JsonPropertyName("spanId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SpanId { get; set; }

    /// <summary>
    /// A unique identifier of event category/type.
    /// </summary>
    [JsonPropertyName("eventName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EventName { get; set; }
}

/// <summary>
/// Represents SeverityNumber enumeration values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OtlpSeverityNumberJson>))]
internal enum OtlpSeverityNumberJson
{
    /// <summary>
    /// Unspecified severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_UNSPECIFIED")]
    SeverityNumberUnspecified = 0,

    /// <summary>
    /// Trace severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_TRACE")]
    SeverityNumberTrace = 1,

    /// <summary>
    /// Trace2 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_TRACE2")]
    SeverityNumberTrace2 = 2,

    /// <summary>
    /// Trace3 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_TRACE3")]
    SeverityNumberTrace3 = 3,

    /// <summary>
    /// Trace4 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_TRACE4")]
    SeverityNumberTrace4 = 4,

    /// <summary>
    /// Debug severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_DEBUG")]
    SeverityNumberDebug = 5,

    /// <summary>
    /// Debug2 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_DEBUG2")]
    SeverityNumberDebug2 = 6,

    /// <summary>
    /// Debug3 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_DEBUG3")]
    SeverityNumberDebug3 = 7,

    /// <summary>
    /// Debug4 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_DEBUG4")]
    SeverityNumberDebug4 = 8,

    /// <summary>
    /// Info severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_INFO")]
    SeverityNumberInfo = 9,

    /// <summary>
    /// Info2 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_INFO2")]
    SeverityNumberInfo2 = 10,

    /// <summary>
    /// Info3 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_INFO3")]
    SeverityNumberInfo3 = 11,

    /// <summary>
    /// Info4 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_INFO4")]
    SeverityNumberInfo4 = 12,

    /// <summary>
    /// Warn severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_WARN")]
    SeverityNumberWarn = 13,

    /// <summary>
    /// Warn2 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_WARN2")]
    SeverityNumberWarn2 = 14,

    /// <summary>
    /// Warn3 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_WARN3")]
    SeverityNumberWarn3 = 15,

    /// <summary>
    /// Warn4 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_WARN4")]
    SeverityNumberWarn4 = 16,

    /// <summary>
    /// Error severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_ERROR")]
    SeverityNumberError = 17,

    /// <summary>
    /// Error2 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_ERROR2")]
    SeverityNumberError2 = 18,

    /// <summary>
    /// Error3 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_ERROR3")]
    SeverityNumberError3 = 19,

    /// <summary>
    /// Error4 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_ERROR4")]
    SeverityNumberError4 = 20,

    /// <summary>
    /// Fatal severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_FATAL")]
    SeverityNumberFatal = 21,

    /// <summary>
    /// Fatal2 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_FATAL2")]
    SeverityNumberFatal2 = 22,

    /// <summary>
    /// Fatal3 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_FATAL3")]
    SeverityNumberFatal3 = 23,

    /// <summary>
    /// Fatal4 severity.
    /// </summary>
    [JsonStringEnumMemberName("SEVERITY_NUMBER_FATAL4")]
    SeverityNumberFatal4 = 24,
}

/// <summary>
/// Represents the export logs service request.
/// </summary>
internal sealed class OtlpExportLogsServiceRequestJson
{
    /// <summary>
    /// An array of ResourceLogs.
    /// </summary>
    [JsonPropertyName("resourceLogs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpResourceLogsJson[]? ResourceLogs { get; set; }
}

/// <summary>
/// Represents the export logs service response.
/// </summary>
internal sealed class OtlpExportLogsServiceResponseJson
{
    /// <summary>
    /// The details of a partially successful export request.
    /// </summary>
    [JsonPropertyName("partialSuccess")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpExportLogsPartialSuccessJson? PartialSuccess { get; set; }
}

/// <summary>
/// Represents partial success information for logs export.
/// </summary>
internal sealed class OtlpExportLogsPartialSuccessJson
{
    /// <summary>
    /// The number of rejected log records. Serialized as string per protojson spec for int64.
    /// </summary>
    [JsonPropertyName("rejectedLogRecords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long RejectedLogRecords { get; set; }

    /// <summary>
    /// A developer-facing human-readable error message.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; set; }
}
