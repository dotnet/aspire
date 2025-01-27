// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Resources : ComponentBase, IAsyncDisposable
{
    private const string TypeColumn = nameof(TypeColumn);
    private const string NameColumn = nameof(NameColumn);
    private const string StateColumn = nameof(StateColumn);
    private const string StartTimeColumn = nameof(StartTimeColumn);
    private const string SourceColumn = nameof(SourceColumn);
    private const string EndpointsColumn = nameof(EndpointsColumn);
    private const string ActionsColumn = nameof(ActionsColumn);

    private Subscription? _logsSubscription;
    private IList<GridColumn>? _gridColumns;
    private Dictionary<ApplicationKey, int>? _applicationUnviewedErrorCounts;

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }
    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; init; }
    [Inject]
    public required DashboardCommandExecutor DashboardCommandExecutor { get; init; }
    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }
    [Inject]
    public required IJSRuntime JS { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? VisibleTypes { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "resource")]
    public string? ResourceName { get; set; }

    private ResourceViewModel? SelectedResource { get; set; }

    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly ConcurrentDictionary<string, bool> _allResourceTypes = [];
    private readonly ConcurrentDictionary<string, bool> _visibleResourceTypes = new(StringComparers.ResourceName);
    private readonly HashSet<string> _expandedResourceNames = [];
    private string _filter = "";
    private bool _isTypeFilterVisible;
    private Task? _resourceSubscriptionTask;
    private bool _isLoading = true;
    private string? _elementIdBeforeDetailsViewOpened;
    private FluentDataGrid<ResourceGridViewModel> _dataGrid = null!;
    private GridColumnManager _manager = null!;
    private int _maxHighlightedCount;

    private ColumnResizeLabels _resizeLabels = ColumnResizeLabels.Default;
    private ColumnSortLabels _sortLabels = ColumnSortLabels.Default;

    private bool Filter(ResourceViewModel resource) => _visibleResourceTypes.ContainsKey(resource.ResourceType) && (_filter.Length == 0 || resource.MatchesFilter(_filter)) && !resource.IsHiddenState();

    private async Task OnAllResourceTypesCheckedChangedAsync(bool? areAllTypesVisible)
    {
        AreAllTypesVisible = areAllTypesVisible;
        await _dataGrid.SafeRefreshDataAsync();
    }

    private async Task OnResourceTypeVisibilityChangedAsync(string resourceType, bool isVisible)
    {
        if (isVisible)
        {
            _visibleResourceTypes[resourceType] = true;
        }
        else
        {
            _visibleResourceTypes.TryRemove(resourceType, out _);
        }

        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
    }

    private async Task HandleSearchFilterChangedAsync()
    {
        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
    }

    private bool? AreAllTypesVisible
    {
        get
        {
            static bool SetEqualsKeys(ConcurrentDictionary<string, bool> left, ConcurrentDictionary<string, bool> right)
            {
                // PERF: This is inefficient since Keys locks and copies the keys.
                var keysLeft = left.Keys;
                var keysRight = right.Keys;

                return keysLeft.Count == keysRight.Count && keysLeft.OrderBy(key => key, StringComparers.ResourceType).SequenceEqual(keysRight.OrderBy(key => key, StringComparers.ResourceType), StringComparers.ResourceType);
            }

            return SetEqualsKeys(_visibleResourceTypes, _allResourceTypes)
                ? true
                : _visibleResourceTypes.IsEmpty
                    ? false
                    : null;
        }
        set
        {
            static bool UnionWithKeys(ConcurrentDictionary<string, bool> left, ConcurrentDictionary<string, bool> right)
            {
                // .Keys locks and copies the keys so avoid it here.
                foreach (var (key, _) in right)
                {
                    left[key] = true;
                }

                return true;
            }

            if (value is true)
            {
                UnionWithKeys(_visibleResourceTypes, _allResourceTypes);
            }
            else if (value is false)
            {
                _visibleResourceTypes.Clear();
            }

            StateHasChanged();
        }
    }

    private readonly GridSort<ResourceGridViewModel> _nameSort = GridSort<ResourceGridViewModel>.ByAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);
    private readonly GridSort<ResourceGridViewModel> _stateSort = GridSort<ResourceGridViewModel>.ByAscending(p => p.Resource.State).ThenAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);
    private readonly GridSort<ResourceGridViewModel> _startTimeSort = GridSort<ResourceGridViewModel>.ByDescending(p => p.Resource.StartTimeStamp).ThenAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);
    private readonly GridSort<ResourceGridViewModel> _typeSort = GridSort<ResourceGridViewModel>.ByAscending(p => p.Resource.ResourceType).ThenAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);

    protected override async Task OnInitializedAsync()
    {
        (_resizeLabels, _sortLabels) = DashboardUIHelpers.CreateGridLabels(ControlsStringsLoc);

        _gridColumns = [
            new GridColumn(Name: NameColumn, DesktopWidth: "1.5fr", MobileWidth: "1.5fr"),
            new GridColumn(Name: StateColumn, DesktopWidth: "1.25fr", MobileWidth: "1.25fr"),
            new GridColumn(Name: StartTimeColumn, DesktopWidth: "1fr"),
            new GridColumn(Name: TypeColumn, DesktopWidth: "1fr"),
            new GridColumn(Name: SourceColumn, DesktopWidth: "2.25fr"),
            new GridColumn(Name: EndpointsColumn, DesktopWidth: "2.25fr", MobileWidth: "2fr"),
            new GridColumn(Name: ActionsColumn, DesktopWidth: "minmax(150px, 1.5fr)", MobileWidth: "1fr")
        ];
        _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();

        if (DashboardClient.IsEnabled)
        {
            await SubscribeResourcesAsync();
        }

        _logsSubscription = TelemetryRepository.OnNewLogs(null, SubscriptionType.Other, async () =>
        {
            var newApplicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();

            // Only update UI if the error counts have changed.
            if (ApplicationErrorCountsChanged(newApplicationUnviewedErrorCounts))
            {
                _applicationUnviewedErrorCounts = newApplicationUnviewedErrorCounts;
                await InvokeAsync(_dataGrid.SafeRefreshDataAsync);
            }
        });

        _isLoading = false;

        async Task SubscribeResourcesAsync()
        {
            var preselectedVisibleResourceTypes = VisibleTypes?.Split(',').ToHashSet();

            var (snapshot, subscription) = await DashboardClient.SubscribeResourcesAsync(_watchTaskCancellationTokenSource.Token);

            // Apply snapshot.
            foreach (var resource in snapshot)
            {
                var added = _resourceByName.TryAdd(resource.Name, resource);

                _allResourceTypes.TryAdd(resource.ResourceType, true);

                if (preselectedVisibleResourceTypes is null || preselectedVisibleResourceTypes.Contains(resource.ResourceType))
                {
                    _visibleResourceTypes.TryAdd(resource.ResourceType, true);
                }

                Debug.Assert(added, "Should not receive duplicate resources in initial snapshot data.");
            }

            UpdateMaxHighlightedCount();
            await _dataGrid.SafeRefreshDataAsync();

            // Listen for updates and apply.
            _resourceSubscriptionTask = Task.Run(async () =>
            {
                await foreach (var changes in subscription.WithCancellation(_watchTaskCancellationTokenSource.Token).ConfigureAwait(false))
                {
                    var selectedResourceHasChanged = false;

                    foreach (var (changeType, resource) in changes)
                    {
                        if (changeType == ResourceViewModelChangeType.Upsert)
                        {
                            _resourceByName[resource.Name] = resource;
                            if (string.Equals(SelectedResource?.Name, resource.Name, StringComparisons.ResourceName))
                            {
                                SelectedResource = resource;
                                selectedResourceHasChanged = true;
                            }

                            if (_allResourceTypes.TryAdd(resource.ResourceType, true))
                            {
                                // If someone has filtered out a resource type then don't remove filter because an update was received.
                                // Only automatically set resource type to visible if it is a new resource.
                                _visibleResourceTypes[resource.ResourceType] = true;
                            }
                        }
                        else if (changeType == ResourceViewModelChangeType.Delete)
                        {
                            var removed = _resourceByName.TryRemove(resource.Name, out _);
                            Debug.Assert(removed, "Cannot remove unknown resource.");
                        }
                    }

                    UpdateMaxHighlightedCount();
                    await InvokeAsync(async () =>
                    {
                        await _dataGrid.SafeRefreshDataAsync();
                        if (selectedResourceHasChanged)
                        {
                            // Notify page that the selected resource parameter has changed.
                            // This is required so the resource open in the details view is refreshed.
                            StateHasChanged();
                        }
                    });
                }
            });
        }
    }

    private ValueTask<GridItemsProviderResult<ResourceGridViewModel>> GetData(GridItemsProviderRequest<ResourceGridViewModel> request)
    {
        // Get filtered and ordered resources.
        var filteredResources = _resourceByName.Values
            .Where(Filter)
            .Select(r => new ResourceGridViewModel { Resource = r })
            .AsQueryable();
        filteredResources = request.ApplySorting(filteredResources);

        // Rearrange resources based on parent information.
        // This must happen after resources are ordered so nested resources are in the right order.
        // Collapsed resources are filtered out of results.
        var orderedResources = ResourceGridViewModel.OrderNestedResources(filteredResources.ToList(), r => !_expandedResourceNames.Contains(r.Name))
            .Where(r => !r.IsHidden)
            .ToList();

        // Paging visible resources.
        var query = orderedResources
            .Skip(request.StartIndex)
            .Take(request.Count ?? DashboardUIHelpers.DefaultDataGridResultCount);

        return ValueTask.FromResult(GridItemsProviderResult.From(query.ToList(), orderedResources.Count));
    }

    private void UpdateMaxHighlightedCount()
    {
        var maxHighlightedCount = 0;
        foreach (var kvp in _resourceByName)
        {
            var resourceHighlightedCount = 0;
            foreach (var command in kvp.Value.Commands)
            {
                if (command.IsHighlighted && command.State != CommandViewModelState.Hidden)
                {
                    resourceHighlightedCount++;
                }
            }
            maxHighlightedCount = Math.Max(maxHighlightedCount, resourceHighlightedCount);
        }

        // Don't attempt to display more than 2 highlighted commands. Many commands will take up too much space.
        // Extra highlighted commands are still available in the menu.
        _maxHighlightedCount = Math.Min(maxHighlightedCount, DashboardUIHelpers.MaxHighlightedCommands);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ResourceName is not null)
        {
            if (_resourceByName.TryGetValue(ResourceName, out var selectedResource))
            {
                await ShowResourceDetailsAsync(selectedResource, buttonId: null);
            }

            // Navigate to remove ?resource=xxx in the URL.
            NavigationManager.NavigateTo(DashboardUrls.ResourcesUrl(), new NavigationOptions { ReplaceHistoryEntry = true });
        }
    }

    private bool ApplicationErrorCountsChanged(Dictionary<ApplicationKey, int> newApplicationUnviewedErrorCounts)
    {
        if (_applicationUnviewedErrorCounts == null || _applicationUnviewedErrorCounts.Count != newApplicationUnviewedErrorCounts.Count)
        {
            return true;
        }

        foreach (var (application, count) in newApplicationUnviewedErrorCounts)
        {
            if (!_applicationUnviewedErrorCounts.TryGetValue(application, out var oldCount) || oldCount != count)
            {
                return true;
            }
        }

        return false;
    }

    private async Task ShowResourceDetailsAsync(ResourceViewModel resource, string? buttonId)
    {
        _elementIdBeforeDetailsViewOpened = buttonId;

        if (string.Equals(SelectedResource?.Name, resource.Name, StringComparisons.ResourceName))
        {
            await ClearSelectedResourceAsync();
        }
        else
        {
            SelectedResource = resource;

            // Ensure that the selected resource is visible in the grid. All parents must be expanded.
            var current = resource;
            while (current != null)
            {
                if (current.GetResourcePropertyValue(KnownProperties.Resource.ParentName) is { Length: > 0 } value)
                {
                    if (_resourceByName.TryGetValue(value, out current))
                    {
                        _expandedResourceNames.Add(value);
                        continue;
                    }
                }

                break;
            }

            await _dataGrid.SafeRefreshDataAsync();
        }
    }

    private async Task ClearSelectedResourceAsync(bool causedByUserAction = false)
    {
        SelectedResource = null;

        await InvokeAsync(StateHasChanged);

        if (_elementIdBeforeDetailsViewOpened is not null && causedByUserAction)
        {
            await JS.InvokeVoidAsync("focusElement", _elementIdBeforeDetailsViewOpened);
        }

        _elementIdBeforeDetailsViewOpened = null;
    }

    private string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourceByName);

    private bool HasMultipleReplicas(ResourceViewModel resource)
    {
        var count = 0;
        foreach (var (_, item) in _resourceByName)
        {
            if (item.IsHiddenState())
            {
                continue;
            }

            if (string.Equals(item.DisplayName, resource.DisplayName, StringComparisons.ResourceName))
            {
                count++;
                if (count >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetRowClass(ResourceViewModel resource)
        => string.Equals(resource.Name, SelectedResource?.Name, StringComparisons.ResourceName) ? "selected-row resource-row" : "resource-row";

    private async Task ExecuteResourceCommandAsync(ResourceViewModel resource, CommandViewModel command)
    {
        await DashboardCommandExecutor.ExecuteAsync(resource, command, GetResourceName);
    }

    private static string GetEndpointsTooltip(ResourceViewModel resource)
    {
        var displayedEndpoints = GetDisplayedEndpoints(resource);

        if (displayedEndpoints.Count == 0)
        {
            return string.Empty;
        }

        if (displayedEndpoints.Count == 1)
        {
            return displayedEndpoints[0].Text;
        }

        var maxShownEndpoints = 3;
        var tooltipBuilder = new StringBuilder(string.Join(", ", displayedEndpoints.Take(maxShownEndpoints).Select(endpoint => endpoint.Text)));

        if (displayedEndpoints.Count > maxShownEndpoints)
        {
            tooltipBuilder.Append(CultureInfo.CurrentCulture, $" + {displayedEndpoints.Count - maxShownEndpoints}");
        }

        return tooltipBuilder.ToString();
    }

    private async Task OnToggleCollapse(ResourceGridViewModel viewModel)
    {
        // View model data is recreated if data updates.
        // Persist the collapsed state in a separate list.
        if (viewModel.IsCollapsed)
        {
            viewModel.IsCollapsed = false;
            _expandedResourceNames.Add(viewModel.Resource.Name);
        }
        else
        {
            viewModel.IsCollapsed = true;
            _expandedResourceNames.Remove(viewModel.Resource.Name);
        }

        await _dataGrid.SafeRefreshDataAsync();
    }

    private static List<DisplayedEndpoint> GetDisplayedEndpoints(ResourceViewModel resource)
    {
        return ResourceEndpointHelpers.GetEndpoints(resource, includeInternalUrls: false);
    }

    public async ValueTask DisposeAsync()
    {
        _watchTaskCancellationTokenSource.Cancel();
        _watchTaskCancellationTokenSource.Dispose();
        _logsSubscription?.Dispose();

        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);
    }
}
