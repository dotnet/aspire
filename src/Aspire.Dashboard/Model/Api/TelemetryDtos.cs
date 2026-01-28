// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Model.Api;

/// <summary>
/// Response from GET /api/telemetry/traces.
/// </summary>
public sealed class TracesResponse
{
    public required TraceDto[] Traces { get; init; }
    public required int TotalCount { get; init; }
    public required int ReturnedCount { get; init; }
}

/// <summary>
/// Response from GET /api/telemetry/logs.
/// </summary>
public sealed class LogsResponse
{
    public required LogEntryDto[] Logs { get; init; }
    public required int TotalCount { get; init; }
    public required int ReturnedCount { get; init; }
}

/// <summary>
/// Represents a distributed trace with all its spans.
/// </summary>
public sealed class TraceDto
{
    public required string TraceId { get; init; }
    public required int DurationMs { get; init; }
    public required string Title { get; init; }
    public required bool HasError { get; init; }
    public required DateTime Timestamp { get; init; }
    public required SpanDto[] Spans { get; init; }
    public LinkDto? DashboardLink { get; init; }
}

/// <summary>
/// Represents a single span within a trace.
/// </summary>
public sealed class SpanDto
{
    public required string SpanId { get; init; }
    public string? ParentSpanId { get; init; }
    public required string Kind { get; init; }
    public required string Name { get; init; }
    public string? Status { get; init; }
    public string? StatusMessage { get; init; }
    public required string Source { get; init; }
    public string? Destination { get; init; }
    public required int DurationMs { get; init; }
    public required Dictionary<string, string> Attributes { get; init; }
}

/// <summary>
/// Represents a structured log entry.
/// </summary>
public sealed class LogEntryDto
{
    public required long LogId { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public required string Message { get; init; }
    public required string Severity { get; init; }
    public required string ResourceName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required Dictionary<string, string> Attributes { get; init; }
    public string? Exception { get; init; }
    public string? Source { get; init; }
    public LinkDto? DashboardLink { get; init; }
}

/// <summary>
/// A link to the Dashboard UI.
/// </summary>
public sealed class LinkDto
{
    public required string Url { get; init; }
    public required string Text { get; init; }
}

/// <summary>
/// JSON serialization context for telemetry API DTOs.
/// </summary>
[JsonSerializable(typeof(TracesResponse))]
[JsonSerializable(typeof(LogsResponse))]
[JsonSerializable(typeof(TraceDto))]
[JsonSerializable(typeof(LogEntryDto))]
[JsonSerializable(typeof(SpanDto))]
[JsonSerializable(typeof(LinkDto))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public sealed partial class TelemetryApiJsonContext : JsonSerializerContext
{
}
