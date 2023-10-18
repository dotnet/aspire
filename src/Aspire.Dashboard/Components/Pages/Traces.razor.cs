// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;
public partial class Traces
{
    private static readonly ApplicationViewModel s_allApplication = new ApplicationViewModel { Id = null, Name = "(All)" };

    private TotalItemsFooter _totalItemsFooter = default!;
    private List<ApplicationViewModel> _applications = default!;
    private ApplicationViewModel _selectedApplication = s_allApplication;
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

        return $"background: linear-gradient(to right, var(--neutral-fill-input-alt-active) {percentage:0.##}%, transparent {percentage:0.##}%);";
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
        _selectedApplication = _applications.SingleOrDefault(e => e.Id == ApplicationInstanceId) ?? s_allApplication;
        ViewModel.ApplicationServiceId = _selectedApplication.Id;
        UpdateSubscription();
    }

    private void UpdateApplications()
    {
        _applications = TelemetryRepository.GetApplications().Select(a => new ApplicationViewModel { Id = a.InstanceId, Name = a.ApplicationName }).ToList();
        _applications.Insert(0, s_allApplication);
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
            _tracesSubscription = TelemetryRepository.OnNewTraces(_selectedApplication.Id, async () =>
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
                ViewModel.FilterText = newFilter ?? string.Empty;
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void HandleClear(string value)
    {
        _filterCts?.Cancel();
        ViewModel.FilterText = string.Empty;
        StateHasChanged();
    }

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
