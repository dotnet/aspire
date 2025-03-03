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

    protected virtual Dictionary<string, AspireTelemetryProperty> GetPageProperties() => [];

    protected override async Task OnInitializedAsync()
    {
        if (TelemetryService.IsTelemetryEnabled)
        {
            var request = new StartOperationRequest(
                TelemetryEventKeys.NavigateToPage,
                new AspireTelemetryScopeSettings(new Dictionary<string, AspireTelemetryProperty> {
                    { TelemetryPropertyKeys.PageUrl, new AspireTelemetryProperty(NavigationManager.Uri) }
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
            var request = new PostOperationRequest(
                TelemetryEventKeys.PageRender,
                TelemetryResult.Success,
                null,
                GetPageProperties(),
                _loadOperation?.Content?.Correlation is { } loadPageCorrelation ? [loadPageCorrelation] : null);

            return TelemetryService.PostUserTaskAsync(request);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (_loadOperation?.Content is { OperationId: { } operationId })
            {
                await TelemetryService.EndUserTaskAsync(new EndOperationRequest(operationId, TelemetryResult.Success));
            }
        }
    }
}
