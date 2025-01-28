// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class TreeMetricSelector
{
    [Parameter, EditorRequired]
    public required Func<Task> HandleSelectedTreeItemChangedAsync { get; set; }

    [Parameter, EditorRequired]
    public required Metrics.MetricsViewModel PageViewModel { get; set; }

    [Parameter]
    public bool IncludeLabel { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    private List<OtlpInstrumentSummary>? _instruments;

    protected override void OnInitialized()
    {
        OnResourceChanged();
    }

    public void OnResourceChanged()
    {
        // instruments may be out of sync if we have updated the application but not yet closed the filter panel
        // this is because we have not updated the URL, which would close the details panel
        // because of this, we should always compute the instruments from the repository
        var selectedInstance = PageViewModel.SelectedApplication.Id?.GetApplicationKey();

        if (selectedInstance is null)
        {
            return;
        }

        _instruments = TelemetryRepository.GetInstrumentsSummaries(selectedInstance.Value);
        StateHasChanged();
    }
}
