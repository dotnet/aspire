// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using static Aspire.Dashboard.Components.Pages.Traces;

namespace Aspire.Dashboard.Components.Pages;

public partial class Traces : IPageWithSessionAndUrlState<TracesPageViewModel, TracesPageState>
{
    private const string TimestampColumn = nameof(TimestampColumn);
    private const string NameColumn = nameof(NameColumn);
    private const string SpansColumn = nameof(SpansColumn);
    private const string DurationColumn = nameof(DurationColumn);
    private const string DetailsColumn = nameof(DetailsColumn);
    private IList<GridColumn> _gridColumns = null!;
    private SelectViewModel<ResourceTypeDetails> _allApplication = null!;

    private TotalItemsFooter _totalItemsFooter = default!;
    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<ResourceTypeDetails>> _applicationViewModels = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _tracesSubscription;
    private bool _applicationChanged;
    private CancellationTokenSource? _filterCts;
    private string _filter = string.Empty;
    private AspirePageContentLayout? _contentLayout;
    private GridColumnManager _manager = null!;

    public string SessionStorageKey => BrowserStorageKeys.TracesPageState;
    public string BasePath => DashboardUrls.TracesBasePath;
    public TracesPageViewModel PageViewModel { get; set; } = null!;

    [Parameter]
    public string? ApplicationName { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required TracesViewModel TracesViewModel { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required IOptions<DashboardOptions> DashboardOptions { get; init; }

    [Inject]
    public required IMessageService MessageService { get; init; }

    [Inject]
    public required ILogger<Traces> Logger { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required ISessionStorage SessionStorage { get; set; }

    [Inject]
    public required DimensionManager DimensionManager { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

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

    private async ValueTask<GridItemsProviderResult<OtlpTrace>> GetData(GridItemsProviderRequest<OtlpTrace> request)
    {
        TracesViewModel.StartIndex = request.StartIndex;
        TracesViewModel.Count = request.Count;
        var traces = TracesViewModel.GetTraces();

        if (DashboardOptions.Value.TelemetryLimits.MaxTraceCount == traces.TotalItemCount && !TelemetryRepository.HasDisplayedMaxTraceLimitMessage)
        {
            await MessageService.ShowMessageBarAsync(options =>
            {
                options.Title = Loc[nameof(Dashboard.Resources.Traces.MessageExceededLimitTitle)];
                options.Body = string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.MessageExceededLimitBody)], DashboardOptions.Value.TelemetryLimits.MaxTraceCount);
                options.Intent = MessageIntent.Info;
                options.Section = "MessagesTop";
                options.AllowDismiss = true;
            });
            TelemetryRepository.HasDisplayedMaxTraceLimitMessage = true;
        }

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to put the count inside a control and explicitly update and refresh the control.
        _totalItemsFooter.SetTotalItemCount(traces.TotalItemCount);

        return GridItemsProviderResult.From(traces.Items, traces.TotalItemCount);
    }

    protected override Task OnInitializedAsync()
    {
        _gridColumns = [
            new GridColumn(Name: TimestampColumn, DesktopWidth: "0.8fr", MobileWidth: "0.8fr"),
            new GridColumn(Name: NameColumn, DesktopWidth: "2fr", MobileWidth: "2fr"),
            new GridColumn(Name: SpansColumn, DesktopWidth: "3fr"),
            new GridColumn(Name: DurationColumn, DesktopWidth: "0.8fr"),
            new GridColumn(Name: DetailsColumn, DesktopWidth: "0.5fr", MobileWidth: "1fr")
        ];

        _allApplication = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = ControlsStringsLoc[name: nameof(ControlsStrings.All)] };
        PageViewModel = new TracesPageViewModel { SelectedApplication = _allApplication };

        UpdateApplications();
        _applicationsSubscription = TelemetryRepository.OnNewApplications(callback: () => InvokeAsync(workItem: () =>
        {
            UpdateApplications();
            StateHasChanged();
        }));

        return Task.CompletedTask;
    }

    private void DimensionManager_OnViewportSizeChanged(object sender, ViewportSizeChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (await this.InitializeViewModelAsync())
        {
            return;
        }

        TracesViewModel.ApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();
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
        _applicationChanged = true;
        return this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: true);
    }

    private void UpdateSubscription()
    {
        var selectedApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();

        // Subscribe to updates.
        if (_tracesSubscription is null || _tracesSubscription.ApplicationKey != selectedApplicationKey)
        {
            _tracesSubscription?.Dispose();
            _tracesSubscription = TelemetryRepository.OnNewTraces(selectedApplicationKey, SubscriptionType.Read, async () =>
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

    private async Task HandleAfterFilterBindAsync()
    {
        if (!string.IsNullOrEmpty(_filter))
        {
            return;
        }

        if (_filterCts is not null)
        {
            await _filterCts.CancelAsync();
        }

        TracesViewModel.FilterText = string.Empty;
        await InvokeAsync(StateHasChanged);
    }

    private string GetResourceName(OtlpApplication app) => OtlpApplication.GetResourceName(app, _applications);
    private string GetResourceName(OtlpApplicationView app) => OtlpApplication.GetResourceName(app, _applications);

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
            DimensionManager.OnViewportInformationChanged += OnBrowserResize;
        }
    }

    private void OnBrowserResize(object? o, EventArgs args)
    {
        InvokeAsync(async () =>
        {
            await JS.InvokeVoidAsync("resetContinuousScrollPosition");
            await JS.InvokeVoidAsync("initializeContinuousScroll");
        });
    }

    public void Dispose()
    {
        _applicationsSubscription?.Dispose();
        _tracesSubscription?.Dispose();
        DimensionManager.OnViewportInformationChanged -= OnBrowserResize;
    }

    public void UpdateViewModelFromQuery(TracesPageViewModel viewModel)
    {
        viewModel.SelectedApplication = _applicationViewModels.GetApplication(Logger, ApplicationName, canSelectGrouping: true, _allApplication);
        TracesViewModel.ApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();
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
