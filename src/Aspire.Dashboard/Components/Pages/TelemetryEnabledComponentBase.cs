// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

/// <summary>
/// A base class for components that would like to opt-in to automatic telemetry events, capturing
/// 1) initial render time
/// 2) component properties on each render
///
/// <remarks>If overriding <see cref="OnInitializedAsync"/>, <see cref="OnParametersSetAsync"/>/<see cref="OnParametersSet"/>, or <see cref="OnAfterRenderAsync"/>/<see cref="OnAfterRender"/>,
/// inheritors <b>must</b> the accompanying base methods for telemetry to work properly.</remarks>
/// </summary>
public abstract class TelemetryEnabledComponentBase : ComponentBase
{
    private ITelemetryResponse<StartOperationResponse>? _loadOperation;

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IAspireTelemetryService TelemetryService { get; init; }

    protected abstract string ComponentId { get; }

    protected virtual Dictionary<string, AspireTelemetryProperty> GetTelemetryProperties() => [];

    // Use OnInitializedAsync instead so we ensure telemetry is initialized.
    protected sealed override void OnInitialized()
    {
    }

    protected override async Task OnInitializedAsync()
    {
        await TelemetryService.InitializeAsync();

        if (TelemetryService.IsTelemetryEnabled)
        {
            var request = new StartOperationRequest(
                TelemetryEventKeys.InitializeComponent,
                new AspireTelemetryScopeSettings(new Dictionary<string, AspireTelemetryProperty> {
                    // Component properties
                    { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(ComponentId) }
                }));
            _loadOperation = await TelemetryService.StartUserTaskAsync(request);
        }
    }

    protected override Task OnParametersSetAsync()
    {
        OnParametersSet();
        return Task.CompletedTask;
    }

    protected override void OnParametersSet()
    {
        PostParametersSetTelemetryAsync();

        return;

        void PostParametersSetTelemetryAsync()
        {
            var properties = GetTelemetryProperties();
            properties[TelemetryPropertyKeys.DashboardComponentId] = new AspireTelemetryProperty(ComponentId);

            var request = new PostOperationRequest(
                TelemetryEventKeys.ParametersSet,
                TelemetryResult.Success,
                null,
                properties,
                _loadOperation?.Content?.Correlation is { } loadPageCorrelation ? [loadPageCorrelation] : null);

            TelemetryService.PostUserTaskAsync(request);
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        OnAfterRender(firstRender);
        return Task.CompletedTask;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (_loadOperation?.Content is { OperationId: { } operationId })
        {
            _loadOperation = null;
            TelemetryService.EndUserTaskAsync(new EndOperationRequest(operationId, TelemetryResult.Success));
        }
    }
}
