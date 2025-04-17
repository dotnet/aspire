// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public class ComponentTelemetryContext(string componentType) : IDisposable
{
    private DashboardTelemetryService? _telemetryService;
    private OperationContext? _initializeCorrelation;

    // Internal for testing
    internal Dictionary<string, AspireTelemetryProperty> Properties { get; } = [];

    private DashboardTelemetryService TelemetryService => _telemetryService ?? throw new ArgumentNullException(nameof(_telemetryService), "InitializeAsync has not been called");

    public async Task InitializeAsync(DashboardTelemetryService telemetryService, IJSRuntime js)
    {
        Properties[TelemetryPropertyKeys.DashboardComponentId] = new AspireTelemetryProperty(componentType);
        Properties[TelemetryPropertyKeys.UserAgent] = new AspireTelemetryProperty(await telemetryService.GetUserAgentAsync(js));

        _telemetryService = telemetryService;
        await telemetryService.InitializeAsync();

        _initializeCorrelation = telemetryService.PostUserTask(
            TelemetryEventKeys.ComponentInitialize,
            TelemetryResult.Success,
            properties: new Dictionary<string, AspireTelemetryProperty>
            {
                // Component properties
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(componentType) }
            });
    }

    public bool UpdateTelemetryProperties(IEnumerable<ComponentTelemetryProperty> modifiedProperties)
    {
        var anyChange = false;

        foreach (var (name, value) in modifiedProperties)
        {
            if (!Properties.TryGetValue(name, out var existingValue) || !existingValue.Value.Equals(value.Value))
            {
                Properties[name] = value;
                anyChange = true;
            }
        }

        if (anyChange)
        {
            PostProperties();
        }

        return anyChange;
    }

    private void PostProperties()
    {
        TelemetryService.PostOperation(
            TelemetryEventKeys.ParametersSet,
            TelemetryResult.Success,
            properties: Properties,
            correlatedWith: _initializeCorrelation?.Properties);
    }

    public void Dispose()
    {
        TelemetryService.PostOperation(
            TelemetryEventKeys.ComponentDispose,
            TelemetryResult.Success,
            properties: new Dictionary<string, AspireTelemetryProperty>
            {
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(componentType) }
            },
            correlatedWith: _initializeCorrelation?.Properties);
    }
}
