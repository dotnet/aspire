// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Telemetry;

namespace Aspire.Dashboard.Telemetry;

public interface IAspireTelemetryService
{
    Task InitializeAsync();

    bool IsTelemetrySupported { get; }
    bool IsTelemetryEnabled { get; }
    Task<bool> SetTelemetryEnabledAsync(bool enabled);

    Task<ITelemetryResponse<StartOperationResponse>?> StartOperationAsync(StartOperationRequest request);
    Task<ITelemetryResponse?> EndOperationAsync(EndOperationRequest request);
    Task<ITelemetryResponse<StartOperationResponse>?> StartUserTaskAsync(StartOperationRequest request);
    Task<ITelemetryResponse?> EndUserTaskAsync(EndOperationRequest request);
    Task PerformUserTaskAsync(StartOperationRequest request, Func<Task<OperationResult>> func);
    Task PerformOperationAsync(StartOperationRequest request, Func<Task<OperationResult>> func);

    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostOperationAsync(PostOperationRequest request);
    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostUserTaskAsync(PostOperationRequest request);
    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostFaultAsync(PostFaultRequest request);
    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostAssetAsync(PostAssetRequest request);
    Task<ITelemetryResponse?> PostPropertyAsync(PostPropertyRequest request);
    Task<ITelemetryResponse?> PostRecurringPropertyAsync(PostPropertyRequest request);
    Task<ITelemetryResponse?> PostCommandLineFlagsAsync(PostCommandLineFlagsRequest request);
}

public interface ITelemetryResponse
{
    HttpStatusCode StatusCode { get; }
}

public interface ITelemetryResponse<out T> : ITelemetryResponse
{
    public T? Content { get; }
}

public class TelemetryResponse(HttpStatusCode statusCode) : ITelemetryResponse
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class TelemetryResponse<T>(HttpStatusCode statusCode, T? result) : ITelemetryResponse<T>
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public T? Content { get; } = result;
}

public record TelemetryEnabledResponse(bool IsEnabled);
public record StartOperationRequest(string EventName, AspireTelemetryScopeSettings? Settings = null);
public record StartOperationResponse(string OperationId, TelemetryEventCorrelation Correlation);
public record EndOperationRequest(string Id, TelemetryResult Result, string? ErrorMessage = null);
public record PostOperationRequest(string EventName, TelemetryResult Result, string? ResultSummary = null, Dictionary<string, AspireTelemetryProperty>? Properties = null, TelemetryEventCorrelation[]? CorrelatedWith = null);
public record PostFaultRequest(string EventName, string Description, FaultSeverity Severity, Dictionary<string, AspireTelemetryProperty>? Properties = null, TelemetryEventCorrelation[]? CorrelatedWith = null);
public record PostAssetRequest(string EventName, string AssetId, int AssetEventVersion, Dictionary<string, AspireTelemetryProperty>? AdditionalProperties, TelemetryEventCorrelation[]? CorrelatedWith = null);
public record PostPropertyRequest(string PropertyName, AspireTelemetryProperty PropertyValue);
public record PostCommandLineFlagsRequest(List<string> FlagPrefixes, Dictionary<string, AspireTelemetryProperty> AdditionalProperties);

public record AspireTelemetryScopeSettings(
    Dictionary<string, AspireTelemetryProperty> StartEventProperties,
    TelemetrySeverity Severity = TelemetrySeverity.Normal,
    bool IsOptOutFriendly = false,
    TelemetryEventCorrelation[]? Correlations = null,
    bool PostStartEvent = true
);

public class TelemetryEventCorrelation
{
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }

    [JsonPropertyName("eventType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataModelEventType EventType { get; set; }
}

public record OperationResult(TelemetryResult Result, string? ErrorMessage = null);

public record AspireTelemetryProperty(object Value, bool IsPii = false);
