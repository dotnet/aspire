// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.ManageData;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class ManageDataDialog : IDialogContentComponent, IAsyncDisposable
{
    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required ConsoleLogsManager ConsoleLogsManager { get; init; }

    [Inject]
    public required TelemetryExportService TelemetryExportService { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Dialogs> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly Dictionary<string, ResourceDataRow> _resourceDataRows = new(StringComparers.ResourceName);
    private readonly HashSet<string> _expandedResourceNames = new(StringComparers.ResourceName);
    private readonly HashSet<(string ResourceName, AspireDataType DataType)> _selectedRows = [];
    private readonly CancellationTokenSource _cts = new();
    private readonly Icon _iconUnselectedMultiple = new Icons.Regular.Size20.CheckboxUnchecked().WithColor(Color.FillInverse);
    private readonly Icon _iconSelectedMultiple = new Icons.Filled.Size20.CheckboxChecked();
    private readonly Icon _iconIndeterminate = new Icons.Filled.Size20.CheckboxIndeterminate();
    private Task? _resourceSubscriptionTask;
    private FluentDataGrid<ManageDataGridItem>? _dataGrid;
    private bool _isExporting;
    private Subscription? _logsSubscription;
    private Subscription? _tracesSubscription;
    private Subscription? _metricsSubscription;
    private Subscription? _resourcesSubscription;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to telemetry changes
        _resourcesSubscription = TelemetryRepository.OnNewResources(OnTelemetryChangedAsync);
        _logsSubscription = TelemetryRepository.OnNewLogs(resourceKey: null, SubscriptionType.Other, OnTelemetryChangedAsync);
        _tracesSubscription = TelemetryRepository.OnNewTraces(resourceKey: null, SubscriptionType.Other, OnTelemetryChangedAsync);
        _metricsSubscription = TelemetryRepository.OnNewMetrics(resourceKey: null, SubscriptionType.Other, OnTelemetryChangedAsync);

        if (DashboardClient.IsEnabled)
        {
            await SubscribeResourcesAsync();
        }
    }

    private async Task OnTelemetryChangedAsync()
    {
        await InvokeAsync(async () =>
        {
            // Recreate all data rows from _resourceByName with updated telemetry counts
            foreach (var (resourceName, resource) in _resourceByName)
            {
                if (_resourceDataRows.TryGetValue(resourceName, out _))
                {
                    var newRow = CreateResourceDataRow(resource);
                    _resourceDataRows[resourceName] = newRow;
                }
            }

            if (_dataGrid is not null)
            {
                await _dataGrid.SafeRefreshDataAsync();
            }

            StateHasChanged();
        });
    }

    private async Task SubscribeResourcesAsync()
    {
        var (snapshot, subscription) = await DashboardClient.SubscribeResourcesAsync(_cts.Token);

        // Apply snapshot.
        foreach (var resource in snapshot)
        {
            _resourceByName[resource.Name] = resource;
            _resourceDataRows[resource.Name] = CreateResourceDataRow(resource);

            // Select all data types for new resources by default
            SelectAllDataTypesForResource(resource.Name, _resourceDataRows[resource.Name].Data);
        }

        // Listen for updates and apply.
        _resourceSubscriptionTask = Task.Run(async () =>
        {
            await foreach (var changes in subscription.WithCancellation(_cts.Token).ConfigureAwait(false))
            {
                foreach (var (changeType, resource) in changes)
                {
                    if (changeType == ResourceViewModelChangeType.Upsert)
                    {
                        _resourceByName[resource.Name] = resource;
                        var isNewResource = !_resourceDataRows.ContainsKey(resource.Name);
                        _resourceDataRows[resource.Name] = CreateResourceDataRow(resource);

                        if (isNewResource)
                        {
                            // Select all data types for new resources by default
                            SelectAllDataTypesForResource(resource.Name, _resourceDataRows[resource.Name].Data);
                        }
                    }
                    else if (changeType == ResourceViewModelChangeType.Delete)
                    {
                        _resourceByName.TryRemove(resource.Name, out _);
                        _resourceDataRows.Remove(resource.Name);
                        _expandedResourceNames.Remove(resource.Name);
                        RemoveAllSelectionsForResource(resource.Name);
                    }
                }

                await InvokeAsync(async () =>
                {
                    if (_dataGrid is not null)
                    {
                        await _dataGrid.SafeRefreshDataAsync();
                    }
                    StateHasChanged();
                });
            }
        });
    }

    private ResourceDataRow CreateResourceDataRow(ResourceViewModel resource)
    {
        var data = new List<DataRow>();

        // Add console logs for resources with ResourceViewModel (no count available)
        data.Add(new DataRow { DataType = AspireDataType.ConsoleLogs, DataCount = null, Icon = new Icons.Regular.Size16.SlideText(), Url = DashboardUrls.ConsoleLogsUrl(resource: resource.Name) });

        var otlpResource = TelemetryRepository.GetResourceByCompositeName(resource.Name);
        if (otlpResource is not null)
        {
            PopulateDataRows(data, otlpResource.ResourceKey, resource.Name);
        }

        return new ResourceDataRow
        {
            Resource = resource,
            OtlpResource = otlpResource,
            Name = resource.Name,
            Data = data
        };
    }

    private ResourceDataRow CreateTelemetryOnlyResourceDataRow(OtlpResource otlpResource)
    {
        var data = new List<DataRow>();
        var resourceName = otlpResource.ResourceKey.GetCompositeName();
        PopulateDataRows(data, otlpResource.ResourceKey, resourceName);

        return new ResourceDataRow
        {
            Resource = null,
            OtlpResource = otlpResource,
            Name = otlpResource.ResourceKey.GetCompositeName(),
            Data = data
        };
    }

    private void PopulateDataRows(List<DataRow> data, ResourceKey resourceKey, string resourceName)
    {
        // Check for logs
        var logsResult = TelemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = 0,
            Filters = []
        });
        if (logsResult.TotalItemCount > 0)
        {
            data.Add(new DataRow { DataType = AspireDataType.StructuredLogs, DataCount = logsResult.TotalItemCount, Icon = new Icons.Regular.Size16.SlideTextSparkle(), Url = DashboardUrls.StructuredLogsUrl(resource: resourceName) });
        }

        // Check for traces
        var tracesResult = TelemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = 0,
            FilterText = "",
            Filters = []
        });
        if (tracesResult.PagedResult.TotalItemCount > 0)
        {
            data.Add(new DataRow { DataType = AspireDataType.Traces, DataCount = tracesResult.PagedResult.TotalItemCount, Icon = new Icons.Regular.Size16.GanttChart(), Url = DashboardUrls.TracesUrl(resource: resourceName) });
        }

        // Check for metrics (instruments)
        var instruments = TelemetryRepository.GetInstrumentsSummaries(resourceKey);
        if (instruments.Count > 0)
        {
            data.Add(new DataRow { DataType = AspireDataType.Metrics, DataCount = instruments.Count, Icon = new Icons.Regular.Size16.ChartMultiple(), Url = DashboardUrls.MetricsUrl(resource: resourceName) });
        }
    }

    private IQueryable<ManageDataGridItem> GetGridItems()
    {
        // Merge telemetry-only resources into the data rows
        MergeTelemetryOnlyResources();

        var items = new List<ManageDataGridItem>();

        // Sort by display name (works for both ResourceViewModel resources and telemetry-only resources)
        foreach (var resourceRow in _resourceDataRows.Values.OrderBy(r => r.Name, StringComparers.ResourceName))
        {
            // Add the resource row
            items.Add(new ManageDataGridItem
            {
                ResourceRow = resourceRow,
                Depth = 0
            });

            // If expanded, add nested data rows
            if (_expandedResourceNames.Contains(resourceRow.Name))
            {
                foreach (var dataRow in resourceRow.Data)
                {
                    items.Add(new ManageDataGridItem
                    {
                        NestedRow = dataRow,
                        ParentResourceName = resourceRow.Name,
                        Depth = 1
                    });
                }
            }
        }

        return items.AsQueryable();
    }

    private void MergeTelemetryOnlyResources()
    {
        // Get all telemetry resources
        var telemetryResources = TelemetryRepository.GetResources();

        foreach (var otlpResource in telemetryResources)
        {
            var compositeName = otlpResource.ResourceKey.GetCompositeName();

            // Check if this resource already exists in our dictionary
            if (!_resourceDataRows.ContainsKey(compositeName))
            {
                // This is a telemetry-only resource, add it
                var row = CreateTelemetryOnlyResourceDataRow(otlpResource);
                _resourceDataRows[compositeName] = row;

                // Select all data types for new telemetry-only resources by default
                SelectAllDataTypesForResource(compositeName, row.Data);
            }
        }
    }

    private void OnRowClicked(FluentDataGridRow<ManageDataGridItem> row)
    {
        var item = row.Item;
        if (item is null)
        {
            return;
        }

        if (item.IsResourceRow && item.ResourceRow is not null)
        {
            OnSelectRowClicked(item.ResourceRow);
        }
        else if (item.IsNestedRow && item.NestedRow is not null && item.ParentResourceName is not null)
        {
            OnSelectDataRowClicked(item.ParentResourceName, item.NestedRow.DataType);
        }
    }

    private void OnToggleExpand(ResourceDataRow resourceRow)
    {
        if (_expandedResourceNames.Contains(resourceRow.Name))
        {
            _expandedResourceNames.Remove(resourceRow.Name);
        }
        else
        {
            _expandedResourceNames.Add(resourceRow.Name);
        }

        StateHasChanged();
    }

    private string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourceByName);

    private string GetDataTypeDisplayName(AspireDataType dataType) => dataType switch
    {
        AspireDataType.ConsoleLogs => Loc[nameof(Resources.Dialogs.ManageDataConsoleLogs)],
        AspireDataType.StructuredLogs => Loc[nameof(Resources.Dialogs.ManageDataStructuredLogs)],
        AspireDataType.Traces => Loc[nameof(Resources.Dialogs.ManageDataTraces)],
        AspireDataType.Metrics => Loc[nameof(Resources.Dialogs.ManageDataMetrics)],
        _ => dataType.ToString()
    };

    private static string GetItemKey(ManageDataGridItem item)
    {
        if (item.ResourceRow is not null)
        {
            return item.ResourceRow.Name;
        }
        if (item.NestedRow is not null && item.ParentResourceName is not null)
        {
            return $"{item.ParentResourceName}_{item.NestedRow.DataType}";
        }
        return string.Empty;
    }

    private void OnSelectAllClicked()
    {
        // If any are unselected (including data rows), select all. Otherwise deselect all.
        var shouldSelectAll = !AreAllSelected();

        if (shouldSelectAll)
        {
            foreach (var row in _resourceDataRows.Values)
            {
                SelectAllDataTypesForResource(row.Name, row.Data);
            }
        }
        else
        {
            _selectedRows.Clear();
        }
    }

    private void OnSelectRowClicked(ResourceDataRow row)
    {
        // If any child data rows are unselected, select all. Otherwise deselect all.
        var shouldSelect = !AreAllDataRowsSelected(row);

        if (shouldSelect)
        {
            SelectAllDataTypesForResource(row.Name, row.Data);
        }
        else
        {
            RemoveAllSelectionsForResource(row.Name);
        }
    }

    private void OnSelectDataRowClicked(string resourceName, AspireDataType dataType)
    {
        var key = (resourceName, dataType);
        if (!_selectedRows.Remove(key))
        {
            _selectedRows.Add(key);
        }
    }

    private bool IsResourceExpanded(string resourceName) => _expandedResourceNames.Contains(resourceName);

    private bool IsDataRowSelected(string resourceName, AspireDataType dataType) => _selectedRows.Contains((resourceName, dataType));

    /// <summary>
    /// Returns true if all resource rows and their data rows are selected.
    /// </summary>
    private bool AreAllSelected()
    {
        foreach (var row in _resourceDataRows.Values)
        {
            foreach (var dataRow in row.Data)
            {
                if (!_selectedRows.Contains((row.Name, dataRow.DataType)))
                {
                    return false;
                }
            }
        }
        return _resourceDataRows.Count > 0;
    }

    /// <summary>
    /// Returns true if no resource rows or data rows are selected.
    /// </summary>
    private bool AreNoneSelected()
    {
        return _selectedRows.Count == 0;
    }

    /// <summary>
    /// Returns true if all data rows for a resource are selected.
    /// </summary>
    private bool AreAllDataRowsSelected(ResourceDataRow row)
    {
        foreach (var dataRow in row.Data)
        {
            if (!_selectedRows.Contains((row.Name, dataRow.DataType)))
            {
                return false;
            }
        }
        return row.Data.Count > 0;
    }

    /// <summary>
    /// Returns true if no data rows for a resource are selected.
    /// </summary>
    private bool AreNoDataRowsSelected(ResourceDataRow row)
    {
        foreach (var dataRow in row.Data)
        {
            if (_selectedRows.Contains((row.Name, dataRow.DataType)))
            {
                return false;
            }
        }
        return true;
    }

    private Icon GetHeaderCheckboxIcon()
    {
        if (AreAllSelected())
        {
            return _iconSelectedMultiple;
        }
        if (AreNoneSelected())
        {
            return _iconUnselectedMultiple;
        }
        return _iconIndeterminate;
    }

    private Icon GetResourceCheckboxIcon(ResourceDataRow row)
    {
        if (AreAllDataRowsSelected(row))
        {
            return _iconSelectedMultiple;
        }
        if (AreNoDataRowsSelected(row))
        {
            return _iconUnselectedMultiple;
        }
        return _iconIndeterminate;
    }

    private void NavigateToDataPage(DataRow dataRow)
    {
        NavigationManager.NavigateTo(dataRow.Url);
    }

    private async Task RemoveSelectedAsync()
    {
        var selectedResources = GetSelectedResourcesAndDataTypes();

        // Clear telemetry signals via repository
        TelemetryRepository.ClearSelectedSignals(selectedResources);

        // Handle console logs filtering separately (not stored in TelemetryRepository)
        var consoleLogResourcesToFilter = selectedResources
            .Where(kvp => kvp.Value.Contains(AspireDataType.ConsoleLogs))
            .Select(kvp => kvp.Key)
            .ToList();

        if (consoleLogResourcesToFilter.Count > 0)
        {
            var filterDate = TimeProvider.GetUtcNow().UtcDateTime;
            var filters = new ConsoleLogsFilters
            {
                FilterResourceLogsDates = consoleLogResourcesToFilter.ToDictionary(r => r, _ => filterDate, StringComparers.ResourceName)
            };
            await ConsoleLogsManager.UpdateFiltersAsync(filters);
        }
    }

    private async Task ExportSelectedAsync()
    {
        if (_isExporting)
        {
            return;
        }

        _isExporting = true;
        StateHasChanged();

        try
        {
            var selectedResources = GetSelectedResourcesAndDataTypes();
            using var memoryStream = await TelemetryExportService.ExportSelectedAsync(selectedResources, _cts.Token);
            var fileName = $"aspire-telemetry-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";

            using var streamRef = new DotNetStreamReference(memoryStream, leaveOpen: false);
            await JS.InvokeVoidAsync("downloadStreamAsFile", fileName, streamRef);
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private Dictionary<string, HashSet<AspireDataType>> GetSelectedResourcesAndDataTypes()
    {
        var result = new Dictionary<string, HashSet<AspireDataType>>(StringComparers.ResourceName);

        foreach (var (resourceName, dataType) in _selectedRows)
        {
            if (!result.TryGetValue(resourceName, out var dataTypes))
            {
                dataTypes = [];
                result[resourceName] = dataTypes;
            }
            dataTypes.Add(dataType);
        }

        return result;
    }

    private void SelectAllDataTypesForResource(string resourceName, List<DataRow> dataRows)
    {
        foreach (var dataRow in dataRows)
        {
            _selectedRows.Add((resourceName, dataRow.DataType));
        }
    }

    private void RemoveAllSelectionsForResource(string resourceName)
    {
        _selectedRows.RemoveWhere(r => StringComparers.ResourceName.Equals(r.ResourceName, resourceName));
    }

    public async ValueTask DisposeAsync()
    {
        _resourcesSubscription?.Dispose();
        _logsSubscription?.Dispose();
        _tracesSubscription?.Dispose();
        _metricsSubscription?.Dispose();

        await _cts.CancelAsync();

        if (_resourceSubscriptionTask is not null)
        {
            try
            {
                await _resourceSubscriptionTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }

        _cts.Dispose();
    }
}
