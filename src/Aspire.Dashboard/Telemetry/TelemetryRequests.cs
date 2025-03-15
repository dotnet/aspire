// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public record StartOperationRequest(string EventName, AspireTelemetryScopeSettings? Settings = null);

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

public record struct AspireTelemetryProperty(object Value, AspireTelemetryPropertyType PropertyType = AspireTelemetryPropertyType.Basic);

public enum AspireTelemetryPropertyType
{
    Pii = 0,
    Basic = 1,
    Metric = 2,
    UserSetting = 3
}
