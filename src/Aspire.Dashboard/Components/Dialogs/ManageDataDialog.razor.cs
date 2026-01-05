// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Dialogs;

/// <summary>
/// Represents a row in the manage data grid, containing resource information and nested data rows.
/// </summary>
public sealed class ResourceDataRow
{
    /// <summary>
    /// The ResourceViewModel from the dashboard client. May be null for telemetry-only resources.
    /// </summary>
    public ResourceViewModel? Resource { get; init; }

    /// <summary>
    /// The OtlpResource from telemetry. May be null if no telemetry data exists yet.
    /// </summary>
    public OtlpResource? OtlpResource { get; init; }

    /// <summary>
    /// The display name for this resource row.
    /// </summary>
    public required string DisplayName { get; init; }

    public bool IsExpanded { get; set; }
    public bool Selected { get; set; }
    public List<DataRow> Data { get; set; } = [];

    /// <summary>
    /// Gets whether this resource is telemetry-only (no corresponding ResourceViewModel).
    /// </summary>
    public bool IsTelemetryOnly => Resource is null && OtlpResource is not null;
}

/// <summary>
/// Represents a nested data row within a resource row.
/// </summary>
public sealed class DataRow
{
    public required string Name { get; init; }
}

/// <summary>
/// Represents an item in the manage data grid, which can be either a resource row or a nested data row.
/// </summary>
public sealed class ManageDataGridItem
{
    public ResourceDataRow? ResourceRow { get; init; }
    public DataRow? NestedRow { get; init; }
    public ResourceViewModel? ParentResource { get; init; }
    public int Depth { get; init; }

    public bool IsResourceRow => ResourceRow is not null;
    public bool IsNestedRow => NestedRow is not null;
}

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

    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly Dictionary<string, ResourceDataRow> _resourceDataRows = new(StringComparers.ResourceName);
    private readonly HashSet<string> _expandedResourceNames = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _cts = new();
    private Task? _resourceSubscriptionTask;
    private FluentDataGrid<ManageDataGridItem>? _dataGrid;
    private bool _isExporting;

    protected override async Task OnInitializedAsync()
    {
        if (DashboardClient.IsEnabled)
        {
            await SubscribeResourcesAsync();
        }
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

        var otlpResource = TelemetryRepository.GetResourceByCompositeName(resource.Name);
        if (otlpResource is not null)
        {
            PopulateDataRows(data, otlpResource.ResourceKey);
        }

        return new ResourceDataRow
        {
            Resource = resource,
            OtlpResource = otlpResource,
            DisplayName = resource.Name,
            Data = data
        };
    }

    private ResourceDataRow CreateTelemetryOnlyResourceDataRow(OtlpResource otlpResource)
    {
        var data = new List<DataRow>();
        PopulateDataRows(data, otlpResource.ResourceKey);

        return new ResourceDataRow
        {
            Resource = null,
            OtlpResource = otlpResource,
            DisplayName = otlpResource.ResourceKey.GetCompositeName(),
            Data = data
        };
    }

    private void PopulateDataRows(List<DataRow> data, ResourceKey resourceKey)
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
            data.Add(new DataRow { Name = "Logs" });
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
            data.Add(new DataRow { Name = "Traces" });
        }

        // Check for metrics (instruments)
        var instruments = TelemetryRepository.GetInstrumentsSummaries(resourceKey);
        if (instruments.Count > 0)
        {
            data.Add(new DataRow { Name = "Metrics" });
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

    private async Task RemoveAllAsync()
    {
        TelemetryRepository.ClearAllSignals();

        await ConsoleLogsManager.UpdateFiltersAsync(new ConsoleLogsFilters { FilterAllLogsDate = TimeProvider.GetUtcNow().UtcDateTime });
    }

    private async Task ExportAllAsync()
    {
        if (_isExporting)
        {
            return;
        }

        _isExporting = true;
        StateHasChanged();

        try
        {
            using var memoryStream = await TelemetryExportService.ExportAllAsync(CancellationToken.None);
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

    public async ValueTask DisposeAsync()
    {
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
