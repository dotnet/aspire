// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Traces
{
    private SelectViewModel<string> _allApplication = null!;

    private TotalItemsFooter _totalItemsFooter = default!;
    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<string>> _applicationViewModels = default!;
    private SelectViewModel<string> _selectedApplication = null!;
    private Subscription? _applicationsSubscription;
    private Subscription? _tracesSubscription;
    private bool _applicationChanged;
    private CancellationTokenSource? _filterCts;
    private string _filter = string.Empty;

    [Parameter]
    public string? ApplicationInstanceId { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel ViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    private string GetRowStyle(OtlpTrace trace)
    {
        var percentage = 0.0;
        if (ViewModel.MaxDuration != TimeSpan.Zero)
        {
            percentage = trace.Duration / ViewModel.MaxDuration * 100.0;
        }

        return string.Create(CultureInfo.InvariantCulture, $"background: linear-gradient(to right, var(--neutral-fill-input-alt-active) {percentage:0.##}%, transparent {percentage:0.##}%);");
    }

    private string GetTooltip(IGrouping<OtlpApplication, OtlpSpan> applicationSpans)
    {
        var count = applicationSpans.Count();
        var errorCount = applicationSpans.Count(s => s.Status == OtlpSpanStatusCode.Error);

        var tooltip = string.Format(CultureInfo.InvariantCulture, Loc[Dashboard.Resources.Traces.TracesResourceSpans], GetResourceName(applicationSpans.Key));
        tooltip += Environment.NewLine + string.Format(CultureInfo.InvariantCulture, Loc[Dashboard.Resources.Traces.TracesTotalTraces], count);
        if (errorCount > 0)
        {
            tooltip += Environment.NewLine + string.Format(CultureInfo.InvariantCulture, Loc[Dashboard.Resources.Traces.TracesTotalErroredTraces], errorCount);
        }

        return tooltip;
    }

    private ValueTask<GridItemsProviderResult<OtlpTrace>> GetData(GridItemsProviderRequest<OtlpTrace> request)
    {
        ViewModel.StartIndex = request.StartIndex;
        ViewModel.Count = request.Count;

        var traces = ViewModel.GetTraces();

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to put the count inside a control and explicitly update and refresh the control.
        _totalItemsFooter.SetTotalItemCount(traces.TotalItemCount);

        return ValueTask.FromResult(GridItemsProviderResult.From(traces.Items, traces.TotalItemCount));
    }

    protected override Task OnInitializedAsync()
    {
        _allApplication  = new SelectViewModel<string> { Id = null, Name = $"({ControlsStringsLoc[ControlsStrings.All]})" };
        _selectedApplication = _allApplication;

        UpdateApplications();
        _applicationsSubscription = TelemetryRepository.OnNewApplications(() => InvokeAsync(() =>
        {
            UpdateApplications();
            StateHasChanged();
        }));

        return Task.CompletedTask;
    }

    protected override void OnParametersSet()
    {
        _selectedApplication = _applicationViewModels.SingleOrDefault(e => e.Id == ApplicationInstanceId) ?? _allApplication;
        ViewModel.ApplicationServiceId = _selectedApplication.Id;
        UpdateSubscription();
    }

    private void UpdateApplications()
    {
        _applications = TelemetryRepository.GetApplications();
        _applicationViewModels = SelectViewModelFactory.CreateApplicationsSelectViewModel(_applications);
        _applicationViewModels.Insert(0, _allApplication);
        UpdateSubscription();
    }

    private Task HandleSelectedApplicationChangedAsync()
    {
        NavigationManager.NavigateTo($"/Traces/{_selectedApplication.Id}");
        _applicationChanged = true;

        return Task.CompletedTask;
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_tracesSubscription is null || _tracesSubscription.ApplicationId != _selectedApplication.Id)
        {
            _tracesSubscription?.Dispose();
            _tracesSubscription = TelemetryRepository.OnNewTraces(_selectedApplication.Id, SubscriptionType.Read, async () =>
            {
                ViewModel.ClearData();
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void HandleFilter(ChangeEventArgs args)
    {
        if (args.Value is string newFilter)
        {
            _filter = newFilter;
            _filterCts?.Cancel();

            // Debouncing logic. Apply the filter after a delay.
            var cts = _filterCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                await Task.Delay(400, cts.Token);
                ViewModel.FilterText = newFilter;
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void HandleClear()
    {
        _filterCts?.Cancel();
        ViewModel.FilterText = string.Empty;
        StateHasChanged();
    }

    private string GetResourceName(OtlpApplication app) => OtlpApplication.GetResourceName(app, _applications);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_applicationChanged)
        {
            await JS.InvokeVoidAsync("resetContinuousScrollPosition");
            _applicationChanged = false;
        }
        await JS.InvokeVoidAsync("initializeContinuousScroll");
    }

    public void Dispose()
    {
        _applicationsSubscription?.Dispose();
        _tracesSubscription?.Dispose();
    }
}
