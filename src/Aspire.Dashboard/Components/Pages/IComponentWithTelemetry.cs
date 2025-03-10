// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

/// <summary>
/// Interface for components that would like to opt-in to telemetry events, capturing
/// 1) initialization
/// 2) component properties on each render
/// </summary>
public interface IComponentWithTelemetry
{
    public OperationContext? InitializeCorrelation { get; set; }
    public NavigationManager NavigationManager { get; }
    public DashboardTelemetryService TelemetryService { get; }

    public string ComponentId { get; }
    public Dictionary<string, AspireTelemetryProperty> GetTelemetryProperties() => [];
}

public static class IComponentWithTelemetryExtensions
{
    public static async Task InitializeComponentTelemetryAsync(this IComponentWithTelemetry component)
    {
        await component.TelemetryService.InitializeAsync();

        if (component.TelemetryService.IsTelemetryEnabled)
        {
            component.InitializeCorrelation = component.TelemetryService.PostUserTask(
                TelemetryEventKeys.InitializeComponent,
                TelemetryResult.Success,
                properties: new Dictionary<string, AspireTelemetryProperty>
                {
                    // Component properties
                    { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(component.ComponentId) }
                });
        }
    }

    public static void PostParametersSetTelemetry(this IComponentWithTelemetry component)
    {
        var properties = component.GetTelemetryProperties();
        properties[TelemetryPropertyKeys.DashboardComponentId] = new AspireTelemetryProperty(component.ComponentId);

        component.TelemetryService.PostOperation(
            TelemetryEventKeys.ParametersSet,
            TelemetryResult.Success,
            properties: properties,
            correlatedWith: component.InitializeCorrelation?.Properties);
    }
}
