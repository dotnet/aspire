// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Telemetry;

public interface IDashboardTelemetryService
{
    /// <summary>
    /// Call before using any telemetry methods. This will initialize the telemetry service and ensure that <see cref="IsTelemetryEnabled"/> is set
    /// by making a request to the debug session, if one exists.
    /// </summary>
    public Task InitializeAsync(IDashboardTelemetrySender? telemetrySender = null);

    /// <summary>
    /// Whether the telemetry service has been initialized. This will be true if <see cref="InitializeAsync"/> has completed.
    /// </summary>
    public bool IsTelemetryInitialized { get; }

    /// <summary>
    /// Whether telemetry is enabled in the current environment. This will be false if:
    /// <list type="bullet">
    /// <item>The user is not running the Aspire dashboard through a supported IDE version</item>
    /// <item>The dashboard resource contains a telemetry opt-out config entry</item>
    /// <item>The IDE instance has opted out of telemetry</item>
    /// </list>
    /// </summary>
    public bool IsTelemetryEnabled { get; }

    /// <summary>
    /// Begin a long-running user operation. Prefer this over <see cref="PostOperation"/>. If an explicit user task caused this operation to start,
    /// use <see cref="StartUserTask"/> instead. Duration will be automatically calculated and the end event posted after <see cref="EndOperation"/> is called.
    /// </summary>
    public (Guid OperationIdToken, Guid CorrelationToken) StartOperation(string eventName, Dictionary<string, AspireTelemetryProperty> startEventProperties, TelemetrySeverity severity = TelemetrySeverity.Normal, bool isOptOutFriendly = false, bool postStartEvent = true, IEnumerable<Guid>? correlations = null);

    /// <summary>
    /// Ends a long-running operation. This will post the end event and calculate the duration.
    /// </summary>
    public void EndOperation(Guid operationId, TelemetryResult result, string? errorMessage = null);

    /// <summary>
    /// Begin a long-running user task. This will post the start event and calculate the duration.
    /// Duration will be automatically calculated and the end event posted after <see cref="EndUserTask"/> is called.
    /// </summary>
    public (Guid OperationIdToken, Guid CorrelationToken) StartUserTask(string eventName, Dictionary<string, AspireTelemetryProperty> startEventProperties, TelemetrySeverity severity = TelemetrySeverity.Normal, bool isOptOutFriendly = false, bool postStartEvent = true, IEnumerable<Guid>? correlations = null);

    /// <summary>
    /// Ends a long-running user task. This will post the end event and calculate the duration.
    /// </summary>
    public void EndUserTask(Guid operationId, TelemetryResult result, string? errorMessage = null);

    /// <summary>
    /// Posts a short-lived operation. If duration needs to be calculated, use <see cref="StartOperation"/> and <see cref="EndOperation"/> instead.
    /// If an explicit user task caused this operation to start, use <see cref="PostUserTask"/> instead.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
    public Guid PostOperation(string eventName, TelemetryResult result, string? resultSummary = null, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null);

    /// <summary>
    /// Posts a short-lived user task. If duration needs to be calculated, use <see cref="StartUserTask"/> and <see cref="EndUserTask"/> instead.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
    public Guid PostUserTask(string eventName, TelemetryResult result, string? resultSummary = null, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null);

    /// <summary>
    /// Posts a fault event.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
    public Guid PostFault(string eventName, string description, FaultSeverity severity, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null);

    /// <summary>
    /// Posts an asset event. This is used to track events that are related to a specific asset, whose correlations can be sent along with other events.
    /// Currently not used.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
    public Guid PostAsset(string eventName, string assetId, int assetEventVersion, Dictionary<string, AspireTelemetryProperty>? additionalProperties = null, IEnumerable<Guid>? correlatedWith = null);

    /// <summary>
    /// Post a session property.
    /// </summary>
    public void PostProperty(string propertyName, AspireTelemetryProperty propertyValue);

    /// <summary>
    /// Post a session recurring property.
    /// </summary>
    public void PostRecurringProperty(string propertyName, AspireTelemetryProperty propertyValue);

    /// <summary>
    /// Currently not used.
    /// </summary>
    public void PostCommandLineFlags(List<string> flagPrefixes, Dictionary<string, AspireTelemetryProperty> additionalProperties);

    /// <summary>
    /// Gets identifying properties for the telemetry session.
    /// </summary>
    public Dictionary<string, AspireTelemetryProperty> GetDefaultProperties();
}

public interface ITelemetryResponse
{
    public HttpStatusCode StatusCode { get; }
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
