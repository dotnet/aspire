// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Telemetry;

namespace Aspire.Dashboard.Telemetry;

public interface IAspireTelemetryService
{
    /// <summary>
    /// Call before using any telemetry methods. This will initialize the telemetry service and ensure that <see cref="IsTelemetrySupported"/> and <see cref="IsTelemetryEnabled"/> are set
    /// by making a request to the debug session, if one exists.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Whether telemetry is supported in the current environment. This will be false if the user is not running in a debug session or if the debug session has telemetry opted-out.
    /// In VS/VSC, this means the current instance of the IDE.
    /// </summary>
    bool IsTelemetrySupported { get; }

    /// <summary>
    /// Whether telemetry is enabled in the current environment. This will be false if:
    /// <list type="bullet">
    /// <item><see cref="IsTelemetrySupported"/> is false</item>
    /// <item>the user has previously opted-out of dashboard telemetry in the current browser (uses localStorage)</item>
    /// <item>the user opts-out of telemetry in the current circuit (by calling <see cref="SetTelemetryEnabledAsync"/>)</item>
    /// </list>
    /// </summary>
    bool IsTelemetryEnabled { get; }

    /// <summary>
    /// Sets the telemetry enabled state for the current circuit. This will not affect debug session telemetry opt-out, and will be persisted to localStorage.
    /// </summary>
    /// <param name="enabled"></param>
    /// <returns>Whether the state was successfully updated. False if <see cref="IsTelemetrySupported"/> is false.</returns>
    Task<bool> SetTelemetryEnabledAsync(bool enabled);

    /// <summary>
    /// Begin a long-running user operation. Preference this over <see cref="PostOperationAsync"/>. If an explicit user task caused this operation to start,
    /// use <see cref="StartUserTaskAsync"/> instead. Duration will be automatically calculated and the end event posted after <see cref="EndOperationAsync"/> is called.
    /// </summary>
    Task<ITelemetryResponse<StartOperationResponse>?> StartOperationAsync(StartOperationRequest request);

    /// <summary>
    /// Ends a long-running operation. This will post the end event and calculate the duration.
    /// </summary>
    Task<ITelemetryResponse?> EndOperationAsync(EndOperationRequest request);

    /// <summary>
    /// Begin a long-running user task. This will post the start event and calculate the duration.
    /// Duration will be automatically calculated and the end event posted after <see cref="EndUserTaskAsync"/> is called.
    /// </summary>
    Task<ITelemetryResponse<StartOperationResponse>?> StartUserTaskAsync(StartOperationRequest request);

    /// <summary>
    /// Ends a long-running user task. This will post the end event and calculate the duration.
    /// </summary>
    Task<ITelemetryResponse?> EndUserTaskAsync(EndOperationRequest request);

    /// <summary>
    /// Performs a short-lived operation. Preference this method over <see cref="StartOperationAsync"/> and <see cref="EndOperationAsync"/> if the operation is contained within a single method.
    /// Use <see cref="PerformUserTaskAsync"/> if the operation is a user task.
    /// </summary>
    Task PerformOperationAsync(StartOperationRequest request, Func<Task<OperationResult>> func);

    /// <summary>
    /// Performs a short-lived user task. Preference this method over <see cref="StartUserTaskAsync"/> and <see cref="EndUserTaskAsync"/> if the operation is contained within a single method.
    /// </summary>
    Task PerformUserTaskAsync(StartOperationRequest request, Func<Task<OperationResult>> func);

    /// <summary>
    /// Posts a short-lived operation. If duration needs to be calculated, use <see cref="StartOperationAsync"/> and <see cref="EndOperationAsync"/> instead.
    /// If an explicit user task caused this operation to start, use <see cref="PostUserTaskAsync"/> instead.
    /// </summary>
    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostOperationAsync(PostOperationRequest request);

    /// <summary>
    /// Posts a short-lived user task. If duration needs to be calculated, use <see cref="StartUserTaskAsync"/> and <see cref="EndUserTaskAsync"/> instead.
    /// </summary>
    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostUserTaskAsync(PostOperationRequest request);

    /// <summary>
    /// Posts a fault event.
    /// </summary>
    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostFaultAsync(PostFaultRequest request);

    /// <summary>
    /// Posts an asset event. This is used to track events that are related to a specific asset, whose correlations can be sent along with other events.
    /// Currently not used.
    /// </summary>
    Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostAssetAsync(PostAssetRequest request);

    /// <summary>
    /// Post a session property.
    /// </summary>
    Task<ITelemetryResponse?> PostPropertyAsync(PostPropertyRequest request);

    /// <summary>
    /// Post a session recurring property.
    /// </summary>
    Task<ITelemetryResponse?> PostRecurringPropertyAsync(PostPropertyRequest request);

    /// <summary>
    /// Currently not used.
    /// </summary>
    Task<ITelemetryResponse?> PostCommandLineFlagsAsync(PostCommandLineFlagsRequest request);

    /// <summary>
    /// Gets identifying properties for the telemetry session.
    /// </summary>
    Dictionary<string, AspireTelemetryProperty> GetDefaultProperties();
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

public record AspireTelemetryProperty(object Value, AspireTelemetryPropertyType PropertyType = AspireTelemetryPropertyType.Basic);

public enum AspireTelemetryPropertyType
{
    Pii = 0,
    Basic = 1,
    Metric = 2,
    UserSetting = 3
}
