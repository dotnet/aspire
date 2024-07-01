// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class StructuredLogs : IPageWithSessionAndUrlState<StructuredLogs.StructuredLogsPageViewModel, StructuredLogs.StructuredLogsPageState>
{
    private SelectViewModel<ResourceTypeDetails> _allApplication = default!;

    private TotalItemsFooter _totalItemsFooter = default!;
    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<ResourceTypeDetails>> _applicationViewModels = default!;
    private List<SelectViewModel<LogLevel?>> _logLevels = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _logsSubscription;
    private bool _applicationChanged;
    private CancellationTokenSource? _filterCts;
    private string? _elementIdBeforeDetailsViewOpened;

    public string BasePath => DashboardUrls.StructuredLogsBasePath;
    public string SessionStorageKey => "StructuredLogs_PageState";
    public StructuredLogsPageViewModel PageViewModel { get; set; } = null!;

    [Parameter]
    public string? ApplicationName { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required StructuredLogsViewModel ViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ProtectedSessionStorage SessionStorage { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; set; }

    [Inject]
    public required ILogger<Traces> Logger { get; init; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? TraceId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? SpanId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "logLevel")]
    public string? LogLevelText { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "filters")]
    public string? SerializedLogFilters { get; set; }

    public StructureLogsDetailsViewModel? SelectedLogEntry { get; set; }

    private ValueTask<GridItemsProviderResult<OtlpLogEntry>> GetData(GridItemsProviderRequest<OtlpLogEntry> request)
    {
        ViewModel.StartIndex = request.StartIndex;
        ViewModel.Count = request.Count;

        var logs = ViewModel.GetLogs();

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to put the count inside a control and explicitly update and refresh the control.
        _totalItemsFooter.SetTotalItemCount(logs.TotalItemCount);

        TelemetryRepository.MarkViewedErrorLogs(ViewModel.ApplicationKey);

        return ValueTask.FromResult(GridItemsProviderResult.From(logs.Items, logs.TotalItemCount));
    }

    protected override Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(TraceId))
        {
            ViewModel.AddFilter(new LogFilter { Field = "TraceId", Condition = FilterCondition.Equals, Value = TraceId });
        }
        if (!string.IsNullOrEmpty(SpanId))
        {
            ViewModel.AddFilter(new LogFilter { Field = "SpanId", Condition = FilterCondition.Equals, Value = SpanId });
        }

        _allApplication = new()
        {
            Id = null,
            Name = Loc[nameof(Dashboard.Resources.ControlsStrings.All)]
        };

        _logLevels = new List<SelectViewModel<LogLevel?>>
        {
            new SelectViewModel<LogLevel?> { Id = null, Name = $"({Loc[nameof(Dashboard.Resources.ControlsStrings.All)]})" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Trace, Name = "Trace" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Debug, Name = "Debug" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Information, Name = "Information" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Warning, Name = "Warning" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Error, Name = "Error" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Critical, Name = "Critical" },
        };

        PageViewModel = new StructuredLogsPageViewModel { SelectedLogLevel = _logLevels[0], SelectedApplication = _allApplication };

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
    }

    private Task HandleSelectedApplicationChangedAsync()
    {
        _applicationChanged = true;

        return this.AfterViewModelChangedAsync();
    }

    private async Task HandleSelectedLogLevelChangedAsync()
    {
        _applicationChanged = true;

        await ClearSelectedLogEntryAsync();
        await this.AfterViewModelChangedAsync();
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_logsSubscription is null || _logsSubscription.ApplicationKey != PageViewModel.SelectedApplication.Id?.GetApplicationKey())
        {
            _logsSubscription?.Dispose();
            _logsSubscription = TelemetryRepository.OnNewLogs(PageViewModel.SelectedApplication.Id?.GetApplicationKey(), SubscriptionType.Read, async () =>
            {
                ViewModel.ClearData();
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private async Task OnShowPropertiesAsync(OtlpLogEntry entry, string buttonId)
    {
        _elementIdBeforeDetailsViewOpened = buttonId;

        if (SelectedLogEntry?.LogEntry == entry)
        {
            await ClearSelectedLogEntryAsync();
        }
        else
        {
            var logEntryViewModel = new StructureLogsDetailsViewModel
            {
                LogEntry = entry
            };

            SelectedLogEntry = logEntryViewModel;
        }
    }

    private async Task ClearSelectedLogEntryAsync(bool causedByUserAction = false)
    {
        SelectedLogEntry = null;

        if (_elementIdBeforeDetailsViewOpened is not null && causedByUserAction)
        {
            await JS.InvokeVoidAsync("focusElement", _elementIdBeforeDetailsViewOpened);
        }

        _elementIdBeforeDetailsViewOpened = null;
    }

    private async Task OpenFilterAsync(LogFilter? entry)
    {
        var logPropertyKeys = TelemetryRepository.GetLogPropertyKeys(PageViewModel.SelectedApplication.Id?.GetApplicationKey());

        var title = entry is not null ? Loc[nameof(Dashboard.Resources.StructuredLogs.StructuredLogsEditFilter)] : Loc[nameof(Dashboard.Resources.StructuredLogs.StructuredLogsAddFilter)];
        var parameters = new DialogParameters
        {
            OnDialogResult = DialogService.CreateDialogCallback(this, HandleFilterDialog),
            Title = title,
            Alignment = HorizontalAlignment.Right,
            PrimaryAction = null,
            SecondaryAction = null,
        };
        var data = new FilterDialogViewModel
        {
            Filter = entry,
            LogPropertyKeys = logPropertyKeys
        };
        await DialogService.ShowPanelAsync<FilterDialog>(data, parameters);
    }

    private async Task HandleFilterDialog(DialogResult result)
    {
        if (result.Data is FilterDialogResult filterResult && filterResult.Filter is LogFilter filter)
        {
            if (filterResult.Delete)
            {
                ViewModel.RemoveFilter(filter);
            }
            else if (filterResult.Add)
            {
                ViewModel.AddFilter(filter);
            }

            await ClearSelectedLogEntryAsync();
        }

        await this.AfterViewModelChangedAsync();
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
                await ClearSelectedLogEntryAsync();

                await Task.Delay(400, cts.Token);
                ViewModel.FilterText = newFilter;
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private async Task HandleClearAsync()
    {
        if (_filterCts is not null)
        {
            await _filterCts.CancelAsync();
        }

        ViewModel.FilterText = string.Empty;
        await ClearSelectedLogEntryAsync();
        StateHasChanged();
    }

    private string GetResourceName(OtlpApplication app) => OtlpApplication.GetResourceName(app, _applications);

    private string GetRowClass(OtlpLogEntry entry)
    {
        if (entry == SelectedLogEntry?.LogEntry)
        {
            return "selected-row";
        }
        else
        {
            return $"log-row-{entry.Severity.ToString().ToLowerInvariant()}";
        }
    }

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
        _logsSubscription?.Dispose();
        _filterCts?.Dispose();
    }

    public string GetUrlFromSerializableViewModel(StructuredLogsPageState serializable)
    {
        var filters = (serializable.Filters.Count > 0) ? LogFilterFormatter.SerializeLogFiltersToString(serializable.Filters) : null;

        var url = DashboardUrls.StructuredLogsUrl(
            resource: serializable.SelectedApplication,
            logLevel: serializable.LogLevelText,
            filters: filters);

        return url;
    }

    public StructuredLogsPageState ConvertViewModelToSerializable()
    {
        return new StructuredLogsPageState
        {
            Filter = PageViewModel.Filter,
            LogLevelText = PageViewModel.SelectedLogLevel.Id?.ToString().ToLowerInvariant(),
            SelectedApplication = PageViewModel.SelectedApplication.Id is not null ? PageViewModel.SelectedApplication.Name : null,
            Filters = ViewModel.Filters
        };
    }

    public void UpdateViewModelFromQuery(StructuredLogsPageViewModel viewModel)
    {
        PageViewModel.SelectedApplication = _applicationViewModels.GetApplication(Logger, ApplicationName, _allApplication);
        ViewModel.ApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();

        if (LogLevelText is not null && Enum.TryParse<LogLevel>(LogLevelText, ignoreCase: true, out var logLevel))
        {
            PageViewModel.SelectedLogLevel = _logLevels.SingleOrDefault(e => e.Id == logLevel) ?? _logLevels[0];
        }
        else
        {
            PageViewModel.SelectedLogLevel = _logLevels[0];
        }

        ViewModel.LogLevel = PageViewModel.SelectedLogLevel.Id;

        if (SerializedLogFilters is not null)
        {
            var filters = LogFilterFormatter.DeserializeLogFiltersFromString(SerializedLogFilters);

            if (filters.Count > 0)
            {
                ViewModel.ClearFilters();
                ViewModel.AddFilters(filters);
            }
        }

        _ = Task.Run(async () =>
        {
            await InvokeAsync(StateHasChanged);
        });
    }

    public class StructuredLogsPageViewModel
    {
        public string Filter { get; set; } = string.Empty;
        public required SelectViewModel<ResourceTypeDetails> SelectedApplication { get; set; }
        public SelectViewModel<LogLevel?> SelectedLogLevel { get; set; } = default!;
    }

    public class StructuredLogsPageState
    {
        public required string Filter { get; set; }
        public string? SelectedApplication { get; set; }
        public string? LogLevelText { get; set; }
        public required IReadOnlyCollection<LogFilter> Filters { get; set; }
    }
}
