// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Telemetry;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

/// <summary>
/// Interface for components that would like to opt-in to telemetry events, capturing
/// 1) initialization
/// 2) component properties on each render
/// 3) component disposal
/// </summary>
public interface IComponentWithTelemetry
{
    public OperationContext? InitializeCorrelation { get; set; }
    public NavigationManager NavigationManager { get; }
    public DashboardTelemetryService TelemetryService { get; }

    public string ComponentType { get; }
    public string ComponentId { get; }
    public Dictionary<string, AspireTelemetryProperty> GetTelemetryProperties() => [];
}

public static class IComponentWithTelemetryExtensions
{
    private static readonly Dictionary<string, Dictionary<string, AspireTelemetryProperty>> s_telemetryProperties = [];

    public static async Task InitializeComponentTelemetryAsync(this IComponentWithTelemetry component)
    {
        await component.TelemetryService.InitializeAsync();

        component.InitializeCorrelation = component.TelemetryService.PostUserTask(
            TelemetryEventKeys.ComponentInitialize,
            TelemetryResult.Success,
            properties: new Dictionary<string, AspireTelemetryProperty>
            {
                // Component properties
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(component.ComponentType) }
            });
    }

    public static void PostParametersSetTelemetry(this IComponentWithTelemetry component)
    {
        var properties = component.GetTelemetryProperties();
        properties[TelemetryPropertyKeys.DashboardComponentId] = new AspireTelemetryProperty(component.ComponentType);

        // return if the properties are the same as the previous ones
        if (s_telemetryProperties.TryGetValue(component.ComponentId, out var previousProperties)
            && previousProperties.Equivalent(properties))
        {
            return;
        }

        component.TelemetryService.PostOperation(
            TelemetryEventKeys.ParametersSet,
            TelemetryResult.Success,
            properties: properties,
            correlatedWith: component.InitializeCorrelation?.Properties);

        s_telemetryProperties[component.ComponentId] = properties;
    }

    public static void DisposeComponentTelemetry(this IComponentWithTelemetry component)
    {
        component.TelemetryService.PostOperation(
            TelemetryEventKeys.ComponentDispose,
            TelemetryResult.Success,
            properties: new Dictionary<string, AspireTelemetryProperty>
            {
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(component.ComponentType) }
            },
            correlatedWith: component.InitializeCorrelation?.Properties);
    }
}
