// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class StructuredLogs
{
    private static readonly SelectViewModel<string> s_allApplication = new SelectViewModel<string> { Id = null, Name = "(All)" };

    private TotalItemsFooter _totalItemsFooter = default!;
    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<string>> _applicationViewModels = default!;
    private List<SelectViewModel<LogLevel?>> _logLevels = default!;
    private SelectViewModel<string> _selectedApplication = s_allApplication;
    private SelectViewModel<LogLevel?> _selectedLogLevel = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _logsSubscription;
    private bool _applicationChanged;
    private CancellationTokenSource? _filterCts;
    private string _filter = string.Empty;

    [Parameter]
    public string? ApplicationInstanceId { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required StructuredLogsViewModel ViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? TraceId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? SpanId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "level")]
    public string? LogLevelText { get; set; }

    public IEnumerable<LogEntryPropertyViewModel>? SelectedLogEntryProperties { get; set; }
    private OtlpLogEntry? _selectedLogEntry;

    private ValueTask<GridItemsProviderResult<OtlpLogEntry>> GetData(GridItemsProviderRequest<OtlpLogEntry> request)
    {
        ViewModel.StartIndex = request.StartIndex;
        ViewModel.Count = request.Count;

        var logs = ViewModel.GetLogs();

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to put the count inside a control and explicitly update and refresh the control.
        _totalItemsFooter.SetTotalItemCount(logs.TotalItemCount);

        TelemetryRepository.MarkViewedErrorLogs(ViewModel.ApplicationServiceId);

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
            ViewModel.AddFilter(new LogFilter { Field = "SpanId", Condition = FilterCondition.Equals, Value = SpanId  });
        }

        _logLevels = new List<SelectViewModel<LogLevel?>>
        {
            new SelectViewModel<LogLevel?> { Id = null, Name = $"({Loc[Dashboard.Resources.StructuredLogs.StructuredLogsAllTypes]})" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Trace, Name = "Trace" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Debug, Name = "Debug" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Information, Name = "Information" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Warning, Name = "Warning" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Error, Name = "Error" },
            new SelectViewModel<LogLevel?> { Id = LogLevel.Critical, Name = "Critical" },
        };
        _selectedLogLevel = _logLevels[0];

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
        _selectedApplication = _applicationViewModels.SingleOrDefault(e => e.Id == ApplicationInstanceId) ?? s_allApplication;
        ViewModel.ApplicationServiceId = _selectedApplication.Id;

        if (LogLevelText != null && Enum.TryParse<LogLevel>(LogLevelText, ignoreCase: true, out var logLevel))
        {
            _selectedLogLevel = _logLevels.SingleOrDefault(e => e.Id == logLevel) ?? _logLevels[0];
        }
        else
        {
            _selectedLogLevel = _logLevels[0];
        }
        ViewModel.LogLevel = _selectedLogLevel.Id;

        UpdateSubscription();
    }

    private void UpdateApplications()
    {
        _applications = TelemetryRepository.GetApplications();
        _applicationViewModels = SelectViewModelFactory.CreateApplicationsSelectViewModel(_applications);
        _applicationViewModels.Insert(0, s_allApplication);
    }

    private Task HandleSelectedApplicationChangedAsync()
    {
        NavigateTo(_selectedApplication.Id, _selectedLogLevel.Id);
        _applicationChanged = true;

        return Task.CompletedTask;
    }

    private Task HandleSelectedLogLevelChangedAsync()
    {
        NavigateTo(_selectedApplication.Id, _selectedLogLevel.Id);
        _applicationChanged = true;

        return Task.CompletedTask;
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_logsSubscription is null || _logsSubscription.ApplicationId != _selectedApplication.Id)
        {
            _logsSubscription?.Dispose();
            _logsSubscription = TelemetryRepository.OnNewLogs(_selectedApplication.Id, SubscriptionType.Read, async () =>
            {
                ViewModel.ClearData();
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void OnShowProperties(OtlpLogEntry entry)
    {
        if (_selectedLogEntry == entry)
        {
            ClearSelectedLogEntry();
        }
        else
        {
            _selectedLogEntry = entry;
            SelectedLogEntryProperties = entry.AllProperties()
                                              .Select(kvp => new LogEntryPropertyViewModel { Name = kvp.Key, Value = kvp.Value })
                                              .ToList();
        }
    }

    private void ClearSelectedLogEntry()
    {
        _selectedLogEntry = null;
        SelectedLogEntryProperties = null;
    }

    private async Task OpenFilterAsync(LogFilter? entry)
    {
        var logPropertyKeys = TelemetryRepository.GetLogPropertyKeys(_selectedApplication.Id);

        var title = entry is not null ? Loc[Dashboard.Resources.StructuredLogs.StructuredLogsEditFilter] : Loc[Dashboard.Resources.StructuredLogs.StructuredLogsAddFilter];
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

    private Task HandleFilterDialog(DialogResult result)
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
        }

        return Task.CompletedTask;
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

    private void NavigateTo(string? applicationId, LogLevel? level)
    {
        string url;
        if (applicationId != null)
        {
            url = $"/StructuredLogs/{applicationId}";
        }
        else
        {
            url = $"/StructuredLogs";
        }

        if (level != null)
        {
            url += $"?level={level.Value.ToString().ToLowerInvariant()}";
        }

        NavigationManager.NavigateTo(url);
    }

    private string GetRowClass(OtlpLogEntry entry)
    {
        if (entry == _selectedLogEntry)
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
}
