// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.Telemetry;

namespace Aspire.Dashboard.Components.Pages;

public abstract class TelemetryPageComponentBase : ComponentBase
{
    private ITelemetryResponse<StartOperationResponse>? _loadOperation;

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IAspireTelemetryService TelemetryService { get; init; }

    protected abstract string PageId { get; }

    protected virtual Dictionary<string, AspireTelemetryProperty> GetPageProperties() => [];

    protected override async Task OnInitializedAsync()
    {
        await TelemetryService.InitializeAsync();

        if (TelemetryService.IsTelemetryEnabled)
        {
            var request = new StartOperationRequest(
                TelemetryEventKeys.NavigateToPage,
                new AspireTelemetryScopeSettings(new Dictionary<string, AspireTelemetryProperty> {
                    { TelemetryPropertyKeys.DashboardPageId, new AspireTelemetryProperty(PageId) }
                }));
            _loadOperation = await TelemetryService.StartOperationAsync(request);
        }
    }

    protected override void OnParametersSet()
    {
        _ = Task.Run(PostPageRenderTelemetryAsync);

        return;

        Task PostPageRenderTelemetryAsync()
        {
            var properties = GetPageProperties();
            properties[TelemetryPropertyKeys.DashboardPageId] = new AspireTelemetryProperty(PageId);

            var request = new PostOperationRequest(
                TelemetryEventKeys.PageRender,
                TelemetryResult.Success,
                null,
                properties,
                _loadOperation?.Content?.Correlation is { } loadPageCorrelation ? [loadPageCorrelation] : null);

            return TelemetryService.PostUserTaskAsync(request);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        /*if (_loadOperation?.Content is { OperationId: { } operationId })
        {
            _loadOperation = null;
            _ = TelemetryService.EndUserTaskAsync(new EndOperationRequest(operationId, TelemetryResult.Success));
        }*/
    }
}
