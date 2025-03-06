// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;

namespace Aspire.Dashboard.Components.Tests.Shared;

public class TestTelemetryService : IDashboardTelemetryService
{
    public Task InitializeAsync(IDashboardTelemetrySender? telemetrySender = null) => Task.CompletedTask;

    public bool IsTelemetryInitialized => true;
    public bool IsTelemetryEnabled => true;

    public (Guid OperationIdToken, Guid CorrelationToken) StartOperation(string eventName, Dictionary<string, AspireTelemetryProperty> startEventProperties, TelemetrySeverity severity = TelemetrySeverity.Normal, bool isOptOutFriendly = false, bool postStartEvent = true, IEnumerable<Guid>? correlations = null)
    {
        return (Guid.NewGuid(), Guid.NewGuid());
    }

    public void EndOperation(Guid operationId, TelemetryResult result, string? errorMessage = null)
    {
    }

    public (Guid OperationIdToken, Guid CorrelationToken) StartUserTask(string eventName, Dictionary<string, AspireTelemetryProperty> startEventProperties, TelemetrySeverity severity = TelemetrySeverity.Normal, bool isOptOutFriendly = false, bool postStartEvent = true, IEnumerable<Guid>? correlations = null)
    {
        return (Guid.NewGuid(), Guid.NewGuid());
    }

    public void EndUserTask(Guid operationId, TelemetryResult result, string? errorMessage = null)
    {
    }

    public Guid PostOperation(string eventName, TelemetryResult result, string? resultSummary = null, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        return Guid.NewGuid();
    }

    public Guid PostUserTask(string eventName, TelemetryResult result, string? resultSummary = null, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        return Guid.NewGuid();
    }

    public Guid PostFault(string eventName, string description, FaultSeverity severity, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        return Guid.NewGuid();
    }

    public Guid PostAsset(string eventName, string assetId, int assetEventVersion, Dictionary<string, AspireTelemetryProperty>? additionalProperties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        return Guid.NewGuid();
    }

    public void PostProperty(string propertyName, AspireTelemetryProperty propertyValue)
    {
    }

    public void PostRecurringProperty(string propertyName, AspireTelemetryProperty propertyValue)
    {
    }

    public void PostCommandLineFlags(List<string> flagPrefixes, Dictionary<string, AspireTelemetryProperty> additionalProperties)
    {
    }

    public Dictionary<string, AspireTelemetryProperty> GetDefaultProperties() => [];
}
