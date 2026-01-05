// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Components.Controls.Chart;
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
using CoreIcons = Microsoft.FluentUI.AspNetCore.Components.Icons;
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
    public required IconResolver IconResolver { get; init; }

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

    [Inject]
    public required IStringLocalizer<Resources.Resources> ResourcesLoc { get; init; }

    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly Dictionary<string, ResourceDataRow> _resourceDataRows = new(StringComparers.ResourceName);
    private readonly HashSet<string> _expandedResourceNames = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _cts = new();
    private readonly Icon _iconUnselectedMultiple = new CoreIcons.Regular.Size20.CheckboxUnchecked().WithColor(Color.FillInverse);
    private readonly Icon _iconSelectedMultiple = new CoreIcons.Filled.Size20.CheckboxChecked();
    private readonly Icon _iconIndeterminate = new CoreIcons.Filled.Size20.CheckboxIndeterminate();
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
                if (_resourceDataRows.TryGetValue(resourceName, out var existingRow))
                {
                    var wasExpanded = existingRow.IsExpanded;
                    var wasSelected = existingRow.Selected;
                    var previousDataSelections = existingRow.Data.ToDictionary(d => d.DataType, d => d.Selected);

                    var newRow = CreateResourceDataRow(resource);

                    // Preserve expanded and selected state
                    newRow.IsExpanded = wasExpanded;
                    newRow.Selected = wasSelected;

                    // Preserve individual data row selections
                    foreach (var dataRow in newRow.Data)
                    {
                        if (previousDataSelections.TryGetValue(dataRow.DataType, out var wasDataSelected))
                        {
                            dataRow.Selected = wasDataSelected;
                        }
                    }

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
                        if (_resourceDataRows.TryGetValue(resource.Name, out var existingRow))
                        {
                            // Preserve expanded state
                            var wasExpanded = existingRow.IsExpanded;
                            _resourceDataRows[resource.Name] = CreateResourceDataRow(resource);
                            _resourceDataRows[resource.Name].IsExpanded = wasExpanded;
                        }
                        else
                        {
                            _resourceDataRows[resource.Name] = CreateResourceDataRow(resource);
                        }
                    }
                    else if (changeType == ResourceViewModelChangeType.Delete)
                    {
                        _resourceByName.TryRemove(resource.Name, out _);
                        _resourceDataRows.Remove(resource.Name);
                        _expandedResourceNames.Remove(resource.Name);
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
        data.Add(new DataRow { DisplayName = "Console logs", DataType = AspireDataType.ConsoleLogs, DataCount = null, Selected = true, Icon = new Icons.Regular.Size16.SlideText(), Url = DashboardUrls.ConsoleLogsUrl(resource: resource.Name) });

        var otlpResource = TelemetryRepository.GetResourceByCompositeName(resource.Name);
        if (otlpResource is not null)
        {
            PopulateDataRows(data, otlpResource.ResourceKey, resource.Name);
        }

        return new ResourceDataRow
        {
            Resource = resource,
            OtlpResource = otlpResource,
            DisplayName = resource.Name,
            Data = data,
            Selected = true
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
            DisplayName = otlpResource.ResourceKey.GetCompositeName(),
            Data = data,
            Selected = true
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
            data.Add(new DataRow { DisplayName = "Logs", DataType = AspireDataType.StructuredLogs, DataCount = logsResult.TotalItemCount, Selected = true, Icon = new Icons.Regular.Size16.SlideTextSparkle(), Url = DashboardUrls.StructuredLogsUrl(resource: resourceName) });
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
            data.Add(new DataRow { DisplayName = "Traces", DataType = AspireDataType.Traces, DataCount = tracesResult.PagedResult.TotalItemCount, Selected = true, Icon = new Icons.Regular.Size16.GanttChart(), Url = DashboardUrls.TracesUrl(resource: resourceName) });
        }

        // Check for metrics (instruments)
        var instruments = TelemetryRepository.GetInstrumentsSummaries(resourceKey);
        if (instruments.Count > 0)
        {
            data.Add(new DataRow { DisplayName = "Metrics", DataType = AspireDataType.Metrics, DataCount = instruments.Count, Selected = true, Icon = new Icons.Regular.Size16.ChartMultiple(), Url = DashboardUrls.MetricsUrl(resource: resourceName) });
        }
    }

    private IQueryable<ManageDataGridItem> GetGridItems()
    {
        // Merge telemetry-only resources into the data rows
        MergeTelemetryOnlyResources();

        var items = new List<ManageDataGridItem>();

        // Sort by display name (works for both ResourceViewModel resources and telemetry-only resources)
        foreach (var resourceRow in _resourceDataRows.Values.OrderBy(r => r.DisplayName, StringComparers.ResourceName))
        {
            // Add the resource row
            items.Add(new ManageDataGridItem
            {
                ResourceRow = resourceRow,
                Depth = 0
            });

            // If expanded, add nested data rows
            if (resourceRow.IsExpanded)
            {
                foreach (var dataRow in resourceRow.Data)
                {
                    items.Add(new ManageDataGridItem
                    {
                        NestedRow = dataRow,
                        ParentResource = resourceRow.Resource,
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
                if (_expandedResourceNames.Contains(compositeName))
                {
                    row.IsExpanded = true;
                }
                _resourceDataRows[compositeName] = row;
            }
        }
    }

    private static void OnRowClicked(FluentDataGridRow<ManageDataGridItem> row)
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
        else if (item.IsNestedRow && item.NestedRow is not null)
        {
            OnSelectDataRowClicked(item.NestedRow);
        }
    }

    private void OnToggleExpand(ResourceDataRow resourceRow)
    {
        resourceRow.IsExpanded = !resourceRow.IsExpanded;

        if (resourceRow.IsExpanded)
        {
            _expandedResourceNames.Add(resourceRow.DisplayName);
        }
        else
        {
            _expandedResourceNames.Remove(resourceRow.DisplayName);
        }

        StateHasChanged();
    }

    public async Task OnViewDetailsAsync(ChartExemplar exemplar)
    {
        var available = await TraceLinkHelpers.WaitForSpanToBeAvailableAsync(
            traceId: exemplar.TraceId,
            spanId: exemplar.SpanId,
            getSpan: TelemetryRepository.GetSpan,
            DialogService,
            InvokeAsync,
            Loc,
            _cts.Token).ConfigureAwait(false);

        if (available)
        {
            NavigationManager.NavigateTo(DashboardUrls.TraceDetailUrl(exemplar.TraceId, spanId: exemplar.SpanId));
        }
    }

    private string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourceByName);

    private static string GetItemKey(ManageDataGridItem item)
    {
        if (item.ResourceRow is not null)
        {
            return item.ResourceRow.DisplayName;
        }
        if (item.NestedRow is not null)
        {
            return item.NestedRow.DisplayName;
        }
        return string.Empty;
    }

    private static string GetRowClass(ManageDataGridItem item)
    {
        return item.IsNestedRow ? "nested-data-row" : "resource-data-row";
    }

    private void OnSelectAllClicked()
    {
        // If any are unselected (including data rows), select all. Otherwise deselect all.
        var shouldSelectAll = !AreAllSelected();
        foreach (var row in _resourceDataRows.Values)
        {
            row.Selected = shouldSelectAll;
            foreach (var dataRow in row.Data)
            {
                dataRow.Selected = shouldSelectAll;
            }
        }
    }

    private static void OnSelectRowClicked(ResourceDataRow row)
    {
        // If any child data rows are unselected, select all. Otherwise deselect all.
        var shouldSelect = !AreAllDataRowsSelected(row);
        row.Selected = shouldSelect;
        foreach (var dataRow in row.Data)
        {
            dataRow.Selected = shouldSelect;
        }
    }

    private static void OnSelectDataRowClicked(DataRow row)
    {
        row.Selected = !row.Selected;
    }

    /// <summary>
    /// Returns true if all resource rows and their data rows are selected.
    /// </summary>
    private bool AreAllSelected()
    {
        foreach (var row in _resourceDataRows.Values)
        {
            if (!row.Selected)
            {
                return false;
            }
            foreach (var dataRow in row.Data)
            {
                if (!dataRow.Selected)
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
        foreach (var row in _resourceDataRows.Values)
        {
            if (row.Selected)
            {
                return false;
            }
            foreach (var dataRow in row.Data)
            {
                if (dataRow.Selected)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Returns true if all data rows for a resource are selected.
    /// </summary>
    private static bool AreAllDataRowsSelected(ResourceDataRow row)
    {
        if (!row.Selected)
        {
            return false;
        }
        foreach (var dataRow in row.Data)
        {
            if (!dataRow.Selected)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns true if no data rows for a resource are selected.
    /// </summary>
    private static bool AreNoDataRowsSelected(ResourceDataRow row)
    {
        foreach (var dataRow in row.Data)
        {
            if (dataRow.Selected)
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

    private Icon GetDefaultResourceIcon()
    {
        return IconResolver.ResolveIconName("Apps", IconSize.Size16, IconVariant.Filled) ?? new Icons.Filled.Size16.Apps();
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
            using var memoryStream = await TelemetryExportService.ExportSelectedAsync(selectedResources, CancellationToken.None);
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

        foreach (var resourceRow in _resourceDataRows.Values)
        {
            var selectedDataTypes = new HashSet<AspireDataType>();

            foreach (var dataRow in resourceRow.Data)
            {
                if (dataRow.Selected)
                {
                    selectedDataTypes.Add(dataRow.DataType);
                }
            }

            if (selectedDataTypes.Count > 0)
            {
                result[resourceRow.DisplayName] = selectedDataTypes;
            }
        }

        return result;
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
