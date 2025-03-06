// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Telemetry;

public interface IAspireTelemetryService
{
    /// <summary>
    /// Call before using any telemetry methods. This will initialize the telemetry service and ensure that <see cref="IsTelemetrySupported"/> and <see cref="IsTelemetryEnabled"/> are set
    /// by making a request to the debug session, if one exists.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Whether the telemetry service has been initialized. This will be true if <see cref="InitializeAsync"/> has completed.
    /// </summary>
    bool IsTelemetryInitialized { get; }

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

public enum TelemetryResult
{
    /// <summary>Used for unknown or unavailable result.</summary>
    None,
    /// <summary>A result without any failure from product or user.</summary>
    Success,
    /// <summary>
    /// A result to indicate the action/operation failed because of product issue (not user faults)
    /// Consider using FaultEvent to provide more details about the failure.
    /// </summary>
    Failure,
    /// <summary>
    /// A result to indicate the action/operation failed because of user fault (e.g., invalid input).
    /// Consider using FaultEvent to provide more details.
    /// </summary>
    UserFault,
    /// <summary>
    /// A result to indicate the action/operation is cancelled by user.
    /// </summary>
    UserCancel,
}

public enum DataModelEventType
{
    /// <summary>User task event</summary>
    UserTask,
    /// <summary>Trace event</summary>
    Trace,
    /// <summary>Operation event</summary>
    Operation,
    /// <summary>Fault event</summary>
    Fault,
    /// <summary>Asset event</summary>
    Asset,
}

public enum TelemetrySeverity
{
    /// <summary>indicates telemetry event with verbose information.</summary>
    Low = -10, // 0xFFFFFFF6
    /// <summary>indicates a regular telemetry event.</summary>
    Normal = 0,
    /// <summary>
    /// indicates telemetry event with high value or require attention (e.g., fault).
    /// </summary>
    High = 10, // 0x0000000A
}

public enum FaultSeverity
{
    /// <summary>
    /// Uncategorized faults have no severity assigned by the developer. Developers should NOT use this severity in any new instrumentation.
    /// The majority of uncategorized faults are being assigned the uncategorized value by default in legacy code.
    /// Teams with high volumes of uncategorized fault data may be asked to make changes to add real severity to their faults.
    /// </summary>
    Uncategorized,
    /// <summary>
    /// Diagnostics faults represent faults which are likely informational in nature. The fault may have no clear tangible impact, it may
    /// be considered "by design" but still undesirable, or the fault only matters in relation to other faults. The fault information is
    /// nonetheless useful to investigate or root-cause an issue, or to inform future investments or changes to design, but the fault
    /// is not itself an indicator of an issue warranting attention.
    /// </summary>
    Diagnostic,
    /// <summary>
    /// General faults are the most common type of fault - the impact or significance of the fault may not be known during instrumentation.
    /// Further investigation may be required to understand the nature of the fault and, if possible, assign a more useful severity.
    /// </summary>
    General,
    /// <summary>
    /// Critical faults are faults which represent likely bugs or notable user impact. If this kind of fault is seen, there is a high
    /// likelihood that there is some kind of bug ultimately causing the issue.
    /// </summary>
    Critical,
    /// <summary>
    /// Crash faults are faults which definitively represent a bug or notable user impact because they represent a fatal crash. While
    /// Watson or other systems may collect a crash dump, crash faults are likely to include other contextual diagnostic information.
    /// </summary>
    Crash,
}
