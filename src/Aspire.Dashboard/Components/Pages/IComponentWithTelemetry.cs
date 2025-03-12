// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;

namespace Aspire.Dashboard.Components.Pages;

/// <summary>
/// Interface for components that would like to opt-in to telemetry events, capturing
/// 1) initialization
/// 2) component properties on each render
/// 3) component disposal
/// </summary>
public interface IComponentWithTelemetry
{
    public DashboardTelemetryService TelemetryService { get; }
    public ComponentTelemetryContext TelemetryContext { get; }

    public void UpdateTelemetryProperties()
    {
    }
}

public static class IComponentWithTelemetryExtensions
{
    public static async Task InitializeComponentTelemetryAsync(this IComponentWithTelemetry component)
    {
        await component.TelemetryService.InitializeAsync();

        component.TelemetryContext.InitializeCorrelation = component.TelemetryService.PostUserTask(
            TelemetryEventKeys.ComponentInitialize,
            TelemetryResult.Success,
            properties: new Dictionary<string, AspireTelemetryProperty>
            {
                // Component properties
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(component.TelemetryContext.ComponentType) }
            });
    }

    public static void DisposeComponentTelemetry(this IComponentWithTelemetry component)
    {
        component.TelemetryService.PostOperation(
            TelemetryEventKeys.ComponentDispose,
            TelemetryResult.Success,
            properties: new Dictionary<string, AspireTelemetryProperty>
            {
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(component.TelemetryContext.ComponentType) }
            },
            correlatedWith: component.TelemetryContext.InitializeCorrelation?.Properties);
    }
}

public class ComponentTelemetryContext(string componentType)
{
    private readonly Dictionary<string, AspireTelemetryProperty> _properties = [];

    public string ComponentType { get; } = componentType;
    public OperationContext? InitializeCorrelation { get; set; }

    public bool UpdateTelemetryProperties(DashboardTelemetryService telemetryService, ReadOnlySpan<ComponentTelemetryProperty> modifiedProperties)
    {
        var anyChange = false;

        foreach (var (name, value) in modifiedProperties)
        {
            if (!_properties.TryGetValue(name, out var existingValue) || !existingValue.Value.Equals(value.Value))
            {
                _properties[name] = value;
                anyChange = true;
            }
        }

        if (anyChange)
        {
            PostProperties(telemetryService);
        }

        return anyChange;
    }

    private void PostProperties(DashboardTelemetryService telemetryService)
    {
        _properties[TelemetryPropertyKeys.DashboardComponentId] = new AspireTelemetryProperty(ComponentType);

        telemetryService.PostOperation(
            TelemetryEventKeys.ParametersSet,
            TelemetryResult.Success,
            properties: _properties,
            correlatedWith: InitializeCorrelation?.Properties);
    }
}

public record struct ComponentTelemetryProperty(string Name, AspireTelemetryProperty Value);
