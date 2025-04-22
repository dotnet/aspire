// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Traces : IComponentWithTelemetry, IPageWithSessionAndUrlState<Traces.TracesPageViewModel, Traces.TracesPageState>
{
    private const string TimestampColumn = nameof(TimestampColumn);
    private const string NameColumn = nameof(NameColumn);
    private const string SpansColumn = nameof(SpansColumn);
    private const string DurationColumn = nameof(DurationColumn);
    private const string ActionsColumn = nameof(ActionsColumn);
    private IList<GridColumn> _gridColumns = null!;
    private SelectViewModel<ResourceTypeDetails> _allApplication = null!;

    private TotalItemsFooter _totalItemsFooter = default!;
    private int _totalItemsCount;
    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<ResourceTypeDetails>> _applicationViewModels = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _tracesSubscription;
    private bool _applicationChanged;
    private string _filter = string.Empty;
    private AspirePageContentLayout? _contentLayout;
    private FluentDataGrid<OtlpTrace> _dataGrid = null!;
    private GridColumnManager _manager = null!;

    private ColumnResizeLabels _resizeLabels = ColumnResizeLabels.Default;
    private ColumnSortLabels _sortLabels = ColumnSortLabels.Default;

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

    [Inject]
    public required PauseManager PauseManager { get; init; }

    [Inject]
    public required DashboardTelemetryService TelemetryService { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "filters")]
    public string? SerializedFilters { get; set; }

    private string GetNameTooltip(OtlpTrace trace)
    {
        var tooltip = string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesFullName)], trace.FullName);
        tooltip += Environment.NewLine + string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesTraceId)], trace.TraceId);

        return tooltip;
    }

    private string GetSpansTooltip(OrderedApplication applicationSpans)
    {
        var count = applicationSpans.TotalSpans;
        var errorCount = applicationSpans.ErroredSpans;

        var tooltip = string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.TracesResourceSpans)], GetResourceName(applicationSpans.Application));
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
        TracesViewModel.Count = request.Count ?? DashboardUIHelpers.DefaultDataGridResultCount;
        var traces = TracesViewModel.GetTraces();

        if (traces.IsFull && !TelemetryRepository.HasDisplayedMaxTraceLimitMessage)
        {
            TelemetryRepository.MaxTraceLimitMessage = await DashboardUIHelpers.DisplayMaxLimitMessageAsync(
                MessageService,
                Loc[nameof(Dashboard.Resources.Traces.MessageExceededLimitTitle)],
                string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Traces.MessageExceededLimitBody)], DashboardOptions.Value.TelemetryLimits.MaxTraceCount),
                () => TelemetryRepository.MaxTraceLimitMessage = null);

            TelemetryRepository.HasDisplayedMaxTraceLimitMessage = true;
        }
        else if (!traces.IsFull && TelemetryRepository.MaxTraceLimitMessage is { } message)
        {
            // Telemetry could have been cleared from the dashboard. Automatically remove full message on data update.
            message.Close();
        }

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to explicitly update and refresh the control.
        _totalItemsCount = traces.TotalItemCount;
        _totalItemsFooter.UpdateDisplayedCount(_totalItemsCount);

        return GridItemsProviderResult.From(traces.Items, traces.TotalItemCount);
    }

    protected override async Task OnInitializedAsync()
    {
        (_resizeLabels, _sortLabels) = DashboardUIHelpers.CreateGridLabels(ControlsStringsLoc);

        _gridColumns = [
            new GridColumn(Name: TimestampColumn, DesktopWidth: "0.8fr", MobileWidth: "0.8fr"),
            new GridColumn(Name: NameColumn, DesktopWidth: "2fr", MobileWidth: "2fr"),
            new GridColumn(Name: SpansColumn, DesktopWidth: "3fr"),
            new GridColumn(Name: DurationColumn, DesktopWidth: "0.8fr"),
            new GridColumn(Name: ActionsColumn, DesktopWidth: "0.5fr", MobileWidth: "1fr")
        ];

        _allApplication = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = ControlsStringsLoc[name: nameof(ControlsStrings.LabelAll)] };
        PageViewModel = new TracesPageViewModel { SelectedApplication = _allApplication };

        UpdateApplications();
        _applicationsSubscription = TelemetryRepository.OnNewApplications(callback: () => InvokeAsync(workItem: () =>
        {
            UpdateApplications();
            StateHasChanged();
        }));

        await TelemetryContext.InitializeAsync(TelemetryService, JS);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (await this.InitializeViewModelAsync())
        {
            return;
        }

        TracesViewModel.ApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();
        UpdateSubscription();
        UpdateTelemetryProperties();
    }

    private void UpdateApplications()
    {
        _applications = TelemetryRepository.GetApplications(includeUninstrumentedPeers: true);
        _applicationViewModels = ApplicationsSelectHelpers.CreateApplications(_applications);
        _applicationViewModels.Insert(0, _allApplication);
        UpdateSubscription();
    }

    private Task HandleSelectedApplicationChanged()
    {
        _applicationChanged = true;

        return this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: true);
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
                await InvokeAsync(_dataGrid.SafeRefreshDataAsync);
            });
        }
    }

    private async Task HandleAfterFilterBindAsync()
    {
        TracesViewModel.FilterText = _filter;
        await InvokeAsync(_dataGrid.SafeRefreshDataAsync);
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

    private string? PauseText => PauseManager.AreTracesPaused(out var startTime)
        ? string.Format(
            CultureInfo.CurrentCulture,
            Loc[nameof(Dashboard.Resources.StructuredLogs.PauseInProgressText)],
            FormatHelpers.FormatTimeWithOptionalDate(TimeProvider, startTime.Value, MillisecondsDisplay.Truncated))
        : null;

    public void Dispose()
    {
        _applicationsSubscription?.Dispose();
        _tracesSubscription?.Dispose();
        DimensionManager.OnViewportInformationChanged -= OnBrowserResize;
    }

    public async Task UpdateViewModelFromQueryAsync(TracesPageViewModel viewModel)
    {
        viewModel.SelectedApplication = _applicationViewModels.GetApplication(Logger, ApplicationName, canSelectGrouping: true, _allApplication);
        TracesViewModel.ApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();

        if (SerializedFilters is not null)
        {
            var filters = TelemetryFilterFormatter.DeserializeFiltersFromString(SerializedFilters);

            if (filters.Count > 0)
            {
                TracesViewModel.ClearFilters();
                foreach (var filter in filters)
                {
                    TracesViewModel.AddFilter(filter);
                }
            }
        }

        await InvokeAsync(_dataGrid.SafeRefreshDataAsync);
    }

    public string GetUrlFromSerializableViewModel(TracesPageState serializable)
    {
        var filters = (serializable.Filters.Count > 0) ? TelemetryFilterFormatter.SerializeFiltersToString(serializable.Filters) : null;

        return DashboardUrls.TracesUrl(
            resource: serializable.SelectedApplication,
            filters: filters);
    }

    public TracesPageState ConvertViewModelToSerializable()
    {
        return new TracesPageState
        {
            SelectedApplication = PageViewModel.SelectedApplication.Id is not null ? PageViewModel.SelectedApplication.Name : null,
            Filters = TracesViewModel.Filters
        };
    }

    private async Task OpenFilterAsync(TelemetryFilter? entry)
    {
        if (_contentLayout is not null)
        {
            await _contentLayout.CloseMobileToolbarAsync();
        }

        var title = entry is not null ? FilterLoc[nameof(StructuredFiltering.DialogTitleEditFilter)] : FilterLoc[nameof(StructuredFiltering.DialogTitleAddFilter)];
        var parameters = new DialogParameters
        {
            OnDialogResult = DialogService.CreateDialogCallback(this, HandleFilterDialog),
            Title = title,
            DismissTitle = DialogsLoc[nameof(Dashboard.Resources.Dialogs.DialogCloseButtonText)],
            Alignment = HorizontalAlignment.Right,
            PrimaryAction = null,
            SecondaryAction = null,
            Width = "450px"
        };
        var data = new FilterDialogViewModel
        {
            Filter = entry,
            PropertyKeys = TelemetryRepository.GetTracePropertyKeys(PageViewModel.SelectedApplication.Id?.GetApplicationKey()),
            KnownKeys = KnownTraceFields.AllFields,
            GetFieldValues = TelemetryRepository.GetTraceFieldValues
        };
        await DialogService.ShowPanelAsync<FilterDialog>(data, parameters);
    }

    private async Task HandleFilterDialog(DialogResult result)
    {
        if (result.Data is FilterDialogResult filterResult && filterResult.Filter is TelemetryFilter filter)
        {
            if (filterResult.Delete)
            {
                TracesViewModel.RemoveFilter(filter);
            }
            else if (filterResult.Add)
            {
                TracesViewModel.AddFilter(filter);
            }
            else if (filterResult.Enable)
            {
                filter.Enabled = true;
            }
            else if (filterResult.Disable)
            {
                filter.Enabled = false;
            }
        }

        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: true);
    }

    private Task ClearTraces(ApplicationKey? key)
    {
        TelemetryRepository.ClearTraces(key);
        return Task.CompletedTask;
    }

    private List<MenuButtonItem> GetFilterMenuItems()
    {
        return this.GetFilterMenuItems(
            TracesViewModel.Filters,
            clearFilters: TracesViewModel.ClearFilters,
            openFilterAsync: OpenFilterAsync,
            filterLoc: FilterLoc,
            dialogsLoc: DialogsLoc,
            contentLayout: _contentLayout);
    }

    public class TracesPageViewModel
    {
        public required SelectViewModel<ResourceTypeDetails> SelectedApplication { get; set; }
    }

    public class TracesPageState
    {
        public string? SelectedApplication { get; set; }
        public required IReadOnlyCollection<TelemetryFilter> Filters { get; set; }
    }

    // IComponentWithTelemetry impl
    public ComponentTelemetryContext TelemetryContext { get; } = new(DashboardUrls.TracesBasePath);

    public void UpdateTelemetryProperties()
    {
        TelemetryContext.UpdateTelemetryProperties([
            new ComponentTelemetryProperty(TelemetryPropertyKeys.ApplicationInstanceId, new AspireTelemetryProperty(PageViewModel.SelectedApplication.Id?.InstanceId ?? string.Empty)),
        ]);
    }
}
