// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.ResourceGraph;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Pages;

public partial class Resources : ComponentBase, IAsyncDisposable, IPageWithSessionAndUrlState<Resources.ResourcesViewModel, Resources.ResourcesPageState>
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
    private bool _hideResourceGraph;
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
    [Inject]
    public required ISessionStorage SessionStorage { get; init; }
    [Inject]
    public required IOptionsMonitor<DashboardOptions> DashboardOptions { get; init; }

    public string BasePath => DashboardUrls.ResourcesBasePath;
    public string SessionStorageKey => "Resources_PageState";
    public ResourcesViewModel PageViewModel { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "view")]
    public string? ViewKindName { get; set; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? VisibleTypes { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? VisibleStates { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? VisibleHealthStates { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "resource")]
    public string? ResourceName { get; set; }

    private ResourceViewModel? SelectedResource { get; set; }

    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly HashSet<string> _collapsedResourceNames = new(StringComparers.ResourceName);
    private string _filter = "";
    private bool _isFilterPopupVisible;
    private Task? _resourceSubscriptionTask;
    private bool _isLoading = true;
    private string? _elementIdBeforeDetailsViewOpened;
    private FluentDataGrid<ResourceGridViewModel> _dataGrid = null!;
    private GridColumnManager _manager = null!;
    private int _maxHighlightedCount;
    private readonly List<MenuButtonItem> _resourcesMenuItems = new();
    private DotNetObjectReference<ResourcesInterop>? _resourcesInteropReference;
    private IJSObjectReference? _jsModule;
    private AspirePageContentLayout? _contentLayout;

    private ColumnResizeLabels _resizeLabels = ColumnResizeLabels.Default;
    private ColumnSortLabels _sortLabels = ColumnSortLabels.Default;

    // Filters in the resource popup
    // Internal for tests
    internal ConcurrentDictionary<string, bool> ResourceTypesToVisibility { get; } = new(StringComparers.ResourceName);
    internal ConcurrentDictionary<string, bool> ResourceStatesToVisibility { get; } = new(StringComparers.ResourceState);
    internal ConcurrentDictionary<string, bool> ResourceHealthStatusesToVisibility { get; } = new(StringComparer.Ordinal);

    private bool Filter(ResourceViewModel resource)
    {
        return IsKeyValueTrue(resource.ResourceType, ResourceTypesToVisibility)
               && IsKeyValueTrue(resource.State ?? string.Empty, ResourceStatesToVisibility)
               && IsKeyValueTrue(resource.HealthStatus?.Humanize() ?? string.Empty, ResourceHealthStatusesToVisibility)
               && (_filter.Length == 0 || resource.MatchesFilter(_filter))
               && !resource.IsHiddenState();

        static bool IsKeyValueTrue(string key, IDictionary<string, bool> dictionary) => dictionary.TryGetValue(key, out var value) && value;
    }

    private async Task OnAllFilterVisibilityCheckedChangedAsync()
    {
        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
        UpdateMenuButtons();
    }

    private async Task OnResourceFilterVisibilityChangedAsync(string resourceType, bool isVisible)
    {
        await UpdateResourceGraphResourcesAsync();
        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
        UpdateMenuButtons();
    }

    private async Task HandleSearchFilterChangedAsync()
    {
        await UpdateResourceGraphResourcesAsync();
        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
    }

    // Internal for tests
    internal bool NoFiltersSet => AreAllTypesVisible && AreAllStatesVisible && AreAllHealthStatesVisible;
    internal bool AreAllTypesVisible => ResourceTypesToVisibility.Values.All(value => value);
    internal bool AreAllStatesVisible => ResourceStatesToVisibility.Values.All(value => value);
    internal bool AreAllHealthStatesVisible => ResourceHealthStatusesToVisibility.Values.All(value => value);

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

        _hideResourceGraph = DashboardOptions.CurrentValue.UI.DisableResourceGraph ?? false;

        PageViewModel = new ResourcesViewModel
        {
            SelectedViewKind = ResourceViewKind.Table
        };

        _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();
        UpdateMenuButtons();

        if (DashboardClient.IsEnabled)
        {
            var collapsedResult = await SessionStorage.GetAsync<List<string>>(BrowserStorageKeys.ResourcesCollapsedResourceNames);
            if (collapsedResult.Success)
            {
                foreach (var resourceName in collapsedResult.Value)
                {
                    _collapsedResourceNames.Add(resourceName);
                }
            }

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
            var preselectedVisibleResourceStates = VisibleStates?.Split(',').ToHashSet();
            var preselectedVisibleResourceHealthStates = VisibleHealthStates?.Split(',').ToHashSet();

            var (snapshot, subscription) = await DashboardClient.SubscribeResourcesAsync(_watchTaskCancellationTokenSource.Token);

            // Apply snapshot.
            foreach (var resource in snapshot)
            {
                var added = UpdateFromResource(
                    resource,
                    type => preselectedVisibleResourceTypes is null || preselectedVisibleResourceTypes.Contains(type),
                    state => preselectedVisibleResourceStates is null || preselectedVisibleResourceStates.Contains(state),
                    healthStatus => preselectedVisibleResourceHealthStates is null || preselectedVisibleResourceHealthStates.Contains(healthStatus));

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
                            UpdateFromResource(
                                resource,
                                t => AreAllTypesVisible,
                                s => AreAllStatesVisible,
                                s => AreAllHealthStatesVisible);

                            if (string.Equals(SelectedResource?.Name, resource.Name, StringComparisons.ResourceName))
                            {
                                SelectedResource = resource;
                                selectedResourceHasChanged = true;
                            }
                        }
                        else if (changeType == ResourceViewModelChangeType.Delete)
                        {
                            var removed = _resourceByName.TryRemove(resource.Name, out _);
                            Debug.Assert(removed, "Cannot remove unknown resource.");
                        }
                    }

                    UpdateMaxHighlightedCount();
                    await UpdateResourceGraphResourcesAsync();
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

        bool UpdateFromResource(ResourceViewModel resource, Func<string, bool> resourceTypeVisible, Func<string, bool> stateVisible, Func<string, bool> healthStatusVisible)
        {
            // This is ok from threadsafty perspective because we are the only thread that's modifying resources.
            bool added;
            if (_resourceByName.TryGetValue(resource.Name, out _))
            {
                added = false;
                _resourceByName[resource.Name] = resource;
            }
            else
            {
                added = _resourceByName.TryAdd(resource.Name, resource);
            }

            ResourceTypesToVisibility.TryAdd(resource.ResourceType, resourceTypeVisible(resource.ResourceType));
            ResourceStatesToVisibility.TryAdd(resource.State ?? string.Empty, stateVisible(resource.State ?? string.Empty));
            ResourceHealthStatusesToVisibility.TryAdd(resource.HealthStatus?.Humanize() ?? string.Empty, healthStatusVisible(resource.HealthStatus?.Humanize() ?? string.Empty));

            UpdateMenuButtons();

            return added;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (PageViewModel.SelectedViewKind == ResourceViewKind.Graph && _jsModule == null)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/js/app-resourcegraph.js");

            _resourcesInteropReference = DotNetObjectReference.Create(new ResourcesInterop(this));

            await _jsModule.InvokeVoidAsync("initializeResourcesGraph", _resourcesInteropReference);
            await UpdateResourceGraphResourcesAsync();
        }
    }

    private async Task UpdateResourceGraphResourcesAsync()
    {
        if (PageViewModel.SelectedViewKind != ResourceViewKind.Graph || _jsModule == null)
        {
            return;
        }

        var activeResources = _resourceByName.Values.Where(Filter).OrderBy(e => e.ResourceType).ThenBy(e => e.Name).ToList();
        var resources = activeResources.Select(r => ResourceGraphMapper.MapResource(r, _resourceByName, ColumnsLoc)).ToList();
        await _jsModule.InvokeVoidAsync("updateResourcesGraph", resources);
    }

    private class ResourcesInterop(Resources resources)
    {
        [JSInvokable]
        public async Task SelectResource(string id)
        {
            if (resources._resourceByName.TryGetValue(id, out var resource))
            {
                await resources.InvokeAsync(async () =>
                {
                    await resources.ShowResourceDetailsAsync(resource, null!);
                    resources.StateHasChanged();
                });
            }
        }
    }

    internal IEnumerable<ResourceViewModel> GetFilteredResources()
    {
        return _resourceByName
            .Values
            .Where(Filter);
    }

    private ValueTask<GridItemsProviderResult<ResourceGridViewModel>> GetData(GridItemsProviderRequest<ResourceGridViewModel> request)
    {
        // Get filtered and ordered resources.
        var filteredResources = GetFilteredResources()
            .Select(r => new ResourceGridViewModel { Resource = r })
            .AsQueryable();
        filteredResources = request.ApplySorting(filteredResources);

        // Rearrange resources based on parent information.
        // This must happen after resources are ordered so nested resources are in the right order.
        // Collapsed resources are filtered out of results.
        var orderedResources = ResourceGridViewModel.OrderNestedResources(filteredResources.ToList(), r => _collapsedResourceNames.Contains(r.Name))
            .Where(r => !r.IsHidden)
            .ToList();

        // Paging visible resources.
        var query = orderedResources
            .Skip(request.StartIndex)
            .Take(request.Count ?? DashboardUIHelpers.DefaultDataGridResultCount)
            .ToList();

        return ValueTask.FromResult(GridItemsProviderResult.From(query, orderedResources.Count));
    }

    private void UpdateMenuButtons()
    {
        _resourcesMenuItems.Clear();

        if (HasCollapsedResources())
        {
            _resourcesMenuItems.Add(new MenuButtonItem
            {
                IsDisabled = false,
                OnClick = OnToggleCollapseAll,
                Text = Loc[nameof(Dashboard.Resources.Resources.ResourceExpandAllChildren)],
                Icon = new Icons.Regular.Size16.Eye()
            });
        }
        else
        {
            _resourcesMenuItems.Add(new MenuButtonItem
            {
                IsDisabled = false,
                OnClick = OnToggleCollapseAll,
                Text = Loc[nameof(Dashboard.Resources.Resources.ResourceCollapseAllChildren)],
                Icon = new Icons.Regular.Size16.EyeOff()
            });
        }
    }

    private bool HasCollapsedResources()
    {
        return _resourceByName.Any(r => !r.Value.IsHiddenState() && _collapsedResourceNames.Contains(r.Key));
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
        if (await this.InitializeViewModelAsync())
        {
            return;
        }

        if (ResourceName is not null)
        {
            if (_resourceByName.TryGetValue(ResourceName, out var selectedResource))
            {
                await ShowResourceDetailsAsync(selectedResource, buttonId: null);

                if (PageViewModel.SelectedViewKind == ResourceViewKind.Graph)
                {
                    await UpdateResourceGraphSelectedAsync();
                }
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
                        _collapsedResourceNames.Remove(value);
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

        if (PageViewModel.SelectedViewKind == ResourceViewKind.Graph)
        {
            await UpdateResourceGraphSelectedAsync();
        }

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
        viewModel.IsCollapsed = !viewModel.IsCollapsed;

        if (viewModel.IsCollapsed)
        {
            _collapsedResourceNames.Add(viewModel.Resource.Name);
        }
        else
        {
            _collapsedResourceNames.Remove(viewModel.Resource.Name);
        }

        await SessionStorage.SetAsync(BrowserStorageKeys.ResourcesCollapsedResourceNames, _collapsedResourceNames.ToList());
        await _dataGrid.SafeRefreshDataAsync();
        UpdateMenuButtons();
    }

    private async Task OnToggleCollapseAll()
    {
        var resourcesWithChildren = _resourceByName.Values
            .Where(r => !r.IsHiddenState())
            .Where(r => _resourceByName.Values.Any(nested => nested.GetResourcePropertyValue(KnownProperties.Resource.ParentName) == r.Name))
            .ToList();

        if (HasCollapsedResources())
        {
            foreach (var resource in resourcesWithChildren)
            {
                _collapsedResourceNames.Remove(resource.Name);
            }
        }
        else
        {
            foreach (var resource in resourcesWithChildren)
            {
                _collapsedResourceNames.Add(resource.Name);
            }
        }

        await SessionStorage.SetAsync(BrowserStorageKeys.ResourcesCollapsedResourceNames, _collapsedResourceNames.ToList());
        await _dataGrid.SafeRefreshDataAsync();
        UpdateMenuButtons();
    }

    private static List<DisplayedEndpoint> GetDisplayedEndpoints(ResourceViewModel resource)
    {
        return ResourceEndpointHelpers.GetEndpoints(resource, includeInternalUrls: false);
    }

    private bool HasAnyChildResources()
    {
        return _resourceByName.Values.Any(r => !string.IsNullOrEmpty(r.GetResourcePropertyValue(KnownProperties.Resource.ParentName)));
    }

    private Task OnTabChangeAsync(FluentTab newTab)
    {
        var id = newTab.Id?.Substring("tab-".Length);

        if (id is null
            || !Enum.TryParse(typeof(ResourceViewKind), id, out var o)
            || o is not ResourceViewKind viewKind)
        {
            return Task.CompletedTask;
        }

        return OnViewChangedAsync(viewKind);
    }

    private async Task OnViewChangedAsync(ResourceViewKind newView)
    {
        PageViewModel.SelectedViewKind = newView;
        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: true);

        if (newView == ResourceViewKind.Graph)
        {
            await UpdateResourceGraphResourcesAsync();
            await UpdateResourceGraphSelectedAsync();
        }
    }

    private async Task UpdateResourceGraphSelectedAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("updateResourcesGraphSelected", SelectedResource?.Name);
        }
    }

    public sealed class ResourcesViewModel
    {
        public required ResourceViewKind SelectedViewKind { get; set; }
    }

    public class ResourcesPageState
    {
        public required string? ViewKind { get; set; }
    }

    public enum ResourceViewKind
    {
        Table,
        Graph
    }

    public Task UpdateViewModelFromQueryAsync(ResourcesViewModel viewModel)
    {
        // Don't allow the view to be updated from the query string if the resource graph is disabled.
        if (!_hideResourceGraph && Enum.TryParse(typeof(ResourceViewKind), ViewKindName, out var view) && view is ResourceViewKind vk)
        {
            viewModel.SelectedViewKind = vk;
        }

        return Task.CompletedTask;
    }

    public string GetUrlFromSerializableViewModel(ResourcesPageState serializable)
    {
        return DashboardUrls.ResourcesUrl(view: serializable.ViewKind);
    }

    public ResourcesPageState ConvertViewModelToSerializable()
    {
        return new ResourcesPageState
        {
            ViewKind = (PageViewModel.SelectedViewKind != ResourceViewKind.Table) ? PageViewModel.SelectedViewKind.ToString() : null
        };
    }

    public async ValueTask DisposeAsync()
    {
        _resourcesInteropReference?.Dispose();
        _watchTaskCancellationTokenSource.Cancel();
        _watchTaskCancellationTokenSource.Dispose();
        _logsSubscription?.Dispose();

        await JSInteropHelpers.SafeDisposeAsync(_jsModule);

        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);
    }
}
