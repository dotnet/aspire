// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Traces : IPageWithSessionAndUrlState<PageViewModelWithFilter, PageStateWithFilter>
{
    private SelectViewModel<ResourceTypeDetails> _allApplication = null!;

    private TotalItemsFooter _totalItemsFooter = default!;
    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<ResourceTypeDetails>> _applicationViewModels = default!;
    private SelectViewModel<ResourceTypeDetails> _selectedApplication = null!;
    private Subscription? _applicationsSubscription;
    private Subscription? _tracesSubscription;
    private bool _applicationChanged;
    private AspirePageContentLayout? _contentLayout;

    private CancellationTokenSource? _filterCts;

    [Parameter]
    public string? ApplicationName { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Filter { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel TracesViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; set; }

    [Inject]
    public required ProtectedSessionStorage SessionStorage { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    public string BasePath => "/traces";
    public string SessionStorageKey => "Traces_PageState";

    public PageViewModelWithFilter PageViewModel { get; set; } = null!;

    private string GetNameTooltip(OtlpTrace trace)
    {
        var tooltip = string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesFullName)], trace.FullName);
        tooltip += Environment.NewLine + string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesTraceId)], trace.TraceId);

        return tooltip;
    }

    private string GetSpansTooltip(IGrouping<OtlpApplication, OtlpSpan> applicationSpans)
    {
        var count = applicationSpans.Count();
        var errorCount = applicationSpans.Count(s => s.Status == OtlpSpanStatusCode.Error);

        var tooltip = string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesResourceSpans)], GetResourceName(applicationSpans.Key));
        tooltip += Environment.NewLine + string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesTotalTraces)], count);
        if (errorCount > 0)
        {
            tooltip += Environment.NewLine + string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesTotalErroredTraces)], errorCount);
        }

        return tooltip;
    }

    private ValueTask<GridItemsProviderResult<OtlpTrace>> GetData(GridItemsProviderRequest<OtlpTrace> request)
    {
        TracesViewModel.StartIndex = request.StartIndex;
        TracesViewModel.Count = request.Count;

        var traces = TracesViewModel.GetTraces();

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to put the count inside a control and explicitly update and refresh the control.
        _totalItemsFooter.SetTotalItemCount(traces.TotalItemCount);

        return ValueTask.FromResult(GridItemsProviderResult.From(traces.Items, traces.TotalItemCount));
    }

    protected override Task OnInitializedAsync()
    {
        _allApplication = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = $"({ControlsStringsLoc[nameof(ControlsStrings.All)]})" };
        _selectedApplication = _allApplication;

        PageViewModel = new PageViewModelWithFilter
        {
            Filter = Filter ?? string.Empty
        };

        UpdateApplications();
        _applicationsSubscription = TelemetryRepository.OnNewApplications(() => InvokeAsync(() =>
        {
            UpdateApplications();
            StateHasChanged();
        }));

        return Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        _selectedApplication = _applicationViewModels.GetApplication(ApplicationName, _allApplication);
        TracesViewModel.ApplicationServiceId = _selectedApplication.Id?.InstanceId;

        await this.InitializeViewModelAsync();
        UpdateSubscription();
    }

    private void UpdateApplications()
    {
        _applications = TelemetryRepository.GetApplications();
        _applicationViewModels = ApplicationsSelectHelpers.CreateApplications(_applications);
        _applicationViewModels.Insert(0, _allApplication);
        UpdateSubscription();
    }

    private Task HandleSelectedApplicationChanged()
    {
        NavigationManager.NavigateTo(DashboardUrls.TracesUrl(resource: _selectedApplication.Name, filter: Filter));
        _applicationChanged = true;

        return Task.CompletedTask;
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_tracesSubscription is null || _tracesSubscription.ApplicationId != _selectedApplication.Id?.InstanceId)
        {
            _tracesSubscription?.Dispose();
            _tracesSubscription = TelemetryRepository.OnNewTraces(_selectedApplication.Id?.InstanceId, SubscriptionType.Read, async () =>
            {
                TracesViewModel.ClearData();
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void HandleFilter(ChangeEventArgs args)
    {
        if (args.Value is string newFilter)
        {
            PageViewModel.Filter = newFilter;
            _filterCts?.Cancel();

            // Debouncing logic. Apply the filter after a delay.
            var cts = _filterCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                await Task.Delay(400, cts.Token);
                TracesViewModel.FilterText = newFilter;
                await InvokeAsync(StateHasChanged);
                await this.AfterViewModelChangedAsync(_contentLayout, true);
            });
        }
    }

    private async Task HandleAfterFilterBindAsync()
    {
        if (!string.IsNullOrEmpty(Filter))
        {
            return;
        }

        if (_filterCts is not null)
        {
            await _filterCts.CancelAsync();
        }

        PageViewModel.Filter = string.Empty;
        TracesViewModel.FilterText = string.Empty;
        await InvokeAsync(StateHasChanged);
        await this.AfterViewModelChangedAsync(_contentLayout, true);
    }

    private string GetResourceName(OtlpApplication app) => OtlpApplication.GetResourceName(app, _applications);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_applicationChanged)
        {
            await JS.InvokeVoidAsync("resetContinuousScrollPosition");
            _applicationChanged = false;
        }
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initializeContinuousScroll");
        }
    }

    public void Dispose()
    {
        _applicationsSubscription?.Dispose();
        _tracesSubscription?.Dispose();
    }

    public void UpdateViewModelFromQuery(PageViewModelWithFilter viewModel)
    {
        viewModel.Filter = Filter ?? string.Empty;
    }

    public string GetUrlFromSerializableViewModel(PageStateWithFilter serializable)
    {
        return DashboardUrls.TracesUrl(filter: serializable.Filter);
    }

    public PageStateWithFilter ConvertViewModelToSerializable()
    {
        return new PageStateWithFilter
        {
            Filter = PageViewModel.Filter
        };
    }
}
