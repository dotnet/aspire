// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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
using static Aspire.Dashboard.Components.Pages.Traces;

namespace Aspire.Dashboard.Components.Pages;

public partial class Traces : IPageWithSessionAndUrlState<TracesPageViewModel, TracesPageState>
{
    private SelectViewModel<ResourceTypeDetails> _allApplication = null!;

    private TotalItemsFooter _totalItemsFooter = default!;
    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<ResourceTypeDetails>> _applicationViewModels = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _tracesSubscription;
    private bool _applicationChanged;
    private CancellationTokenSource? _filterCts;
    private string _filter = string.Empty;

    public string SessionStorageKey => "Traces_PageState";
    public string BasePath => DashboardUrls.TracesBasePath;
    public TracesPageViewModel PageViewModel { get; set; } = null!;

    [Parameter]
    public string? ApplicationName { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel TracesViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; set; }

    [Inject]
    public required ILogger<Traces> Logger { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required ProtectedSessionStorage SessionStorage { get; set; }

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
        PageViewModel = new TracesPageViewModel { SelectedApplication = _allApplication };

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

    private Task HandleSelectedApplicationChangedAsync()
    {
        _applicationChanged = true;
        return this.AfterViewModelChangedAsync();
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_tracesSubscription is null || _tracesSubscription.ApplicationId != PageViewModel.SelectedApplication.Id?.InstanceId)
        {
            _tracesSubscription?.Dispose();
            _tracesSubscription = TelemetryRepository.OnNewTraces(PageViewModel.SelectedApplication.Id?.InstanceId, SubscriptionType.Read, async () =>
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
            _filter = newFilter;
            _filterCts?.Cancel();

            // Debouncing logic. Apply the filter after a delay.
            var cts = _filterCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                await Task.Delay(400, cts.Token);
                TracesViewModel.FilterText = newFilter;
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void HandleClear()
    {
        _filterCts?.Cancel();
        TracesViewModel.FilterText = string.Empty;
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

    public void UpdateViewModelFromQuery(TracesPageViewModel viewModel)
    {
        viewModel.SelectedApplication = _applicationViewModels.GetApplication(ApplicationName, _allApplication);
        TracesViewModel.ApplicationServiceId = PageViewModel.SelectedApplication.Id?.InstanceId;
    }

    public string GetUrlFromSerializableViewModel(TracesPageState serializable)
    {
        return DashboardUrls.TracesUrl(serializable.SelectedApplication);
    }

    public TracesPageState ConvertViewModelToSerializable()
    {
        return new TracesPageState
        {
            SelectedApplication = PageViewModel.SelectedApplication.Id is not null ? PageViewModel.SelectedApplication.Name : null,
        };
    }

    public class TracesPageViewModel
    {
        public required SelectViewModel<ResourceTypeDetails> SelectedApplication { get; set; }
    }

    public class TracesPageState
    {
        public string? SelectedApplication { get; set; }
    }
}
