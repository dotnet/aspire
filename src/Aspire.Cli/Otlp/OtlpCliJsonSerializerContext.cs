// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Otlp.Serialization;

namespace Aspire.Cli.Otlp;

/// <summary>
/// Represents the API response wrapper for telemetry data.
/// </summary>
internal sealed class TelemetryApiResponse
{
    [JsonPropertyName("data")]
    public OtlpTelemetryDataJson? Data { get; set; }

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
    /// The instance ID (e.g., "abc123").
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

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

    /// <summary>
    /// Gets the full display name by combining Name and InstanceId.
    /// </summary>
    /// <returns>The full display name (e.g., "catalogservice-abc123" or "catalogservice").</returns>
    public string GetCompositeName()
    {
        if (InstanceId is null)
        {
            return Name;
        }

        return $"{Name}-{InstanceId}";
    }
}
