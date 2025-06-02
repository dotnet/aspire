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
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Aspire.Hosting.Utils;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Pages;

public partial class Resources : ComponentBase, IComponentWithTelemetry, IAsyncDisposable, IPageWithSessionAndUrlState<Resources.ResourcesViewModel, Resources.ResourcesPageState>
{
    private const string TypeColumn = nameof(TypeColumn);
    private const string NameColumn = nameof(NameColumn);
    private const string StateColumn = nameof(StateColumn);
    private const string StartTimeColumn = nameof(StartTimeColumn);
    private const string SourceColumn = nameof(SourceColumn);
    private const string UrlsColumn = nameof(UrlsColumn);
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
    [Inject]
    public required ComponentTelemetryContextProvider TelemetryContextProvider { get; init; }
    [Inject]
    public required ILogger<Resources> Logger { get; init; }

    public string BasePath => DashboardUrls.ResourcesBasePath;
    public string SessionStorageKey => BrowserStorageKeys.ResourcesPageState;
    public ResourcesViewModel PageViewModel { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "view")]
    public string? ViewKindName { get; set; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? HiddenTypes { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? HiddenStates { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? HiddenHealthStates { get; set; }

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
    private bool _graphInitialized;
    private AspirePageContentLayout? _contentLayout;

    private AspireMenu? _contextMenu;
    private bool _contextMenuOpen;
    private readonly List<MenuButtonItem> _contextMenuItems = new();
    private TaskCompletionSource? _contextMenuClosedTcs;

    private ColumnResizeLabels _resizeLabels = ColumnResizeLabels.Default;
    private ColumnSortLabels _sortLabels = ColumnSortLabels.Default;
    private bool _showResourceTypeColumn;

    private bool Filter(ResourceViewModel resource)
    {
        return IsKeyValueTrue(resource.ResourceType, PageViewModel.ResourceTypesToVisibility)
               && IsKeyValueTrue(resource.State ?? string.Empty, PageViewModel.ResourceStatesToVisibility)
               && IsKeyValueTrue(resource.HealthStatus?.Humanize() ?? string.Empty, PageViewModel.ResourceHealthStatusesToVisibility)
               && (_filter.Length == 0 || resource.MatchesFilter(_filter))
               && !resource.IsResourceHidden();

        static bool IsKeyValueTrue(string key, IDictionary<string, bool> dictionary) => dictionary.TryGetValue(key, out var value) && value;
    }

    private async Task OnAllFilterVisibilityCheckedChangedAsync()
    {
        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
        UpdateMenuButtons();
        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
    }

    private async Task OnResourceFilterVisibilityChangedAsync(string resourceType, bool isVisible)
    {
        await UpdateResourceGraphResourcesAsync();
        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
        UpdateMenuButtons();
        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
    }

    private async Task HandleSearchFilterChangedAsync()
    {
        await UpdateResourceGraphResourcesAsync();
        await ClearSelectedResourceAsync();
        await _dataGrid.SafeRefreshDataAsync();
    }

    // Internal for tests
    internal bool NoFiltersSet => AreAllTypesVisible && AreAllStatesVisible && AreAllHealthStatesVisible;
    internal bool AreAllTypesVisible => PageViewModel.ResourceTypesToVisibility.Values.All(value => value);
    internal bool AreAllStatesVisible => PageViewModel.ResourceStatesToVisibility.Values.All(value => value);
    internal bool AreAllHealthStatesVisible => PageViewModel.ResourceHealthStatusesToVisibility.Values.All(value => value);

    private readonly GridSort<ResourceGridViewModel> _nameSort = GridSort<ResourceGridViewModel>.ByAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);
    private readonly GridSort<ResourceGridViewModel> _stateSort = GridSort<ResourceGridViewModel>.ByAscending(p => p.Resource.State).ThenAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);
    private readonly GridSort<ResourceGridViewModel> _startTimeSort = GridSort<ResourceGridViewModel>.ByDescending(p => p.Resource.StartTimeStamp).ThenAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);
    private readonly GridSort<ResourceGridViewModel> _typeSort = GridSort<ResourceGridViewModel>.ByAscending(p => p.Resource.ResourceType).ThenAscending(p => p.Resource, ResourceViewModelNameComparer.Instance);

    protected override async Task OnInitializedAsync()
    {
        TelemetryContextProvider.Initialize(TelemetryContext);
        (_resizeLabels, _sortLabels) = DashboardUIHelpers.CreateGridLabels(ControlsStringsLoc);

        _gridColumns = [
            new GridColumn(Name: NameColumn, DesktopWidth: "1.5fr", MobileWidth: "1.5fr"),
            new GridColumn(Name: StateColumn, DesktopWidth: "1.25fr", MobileWidth: "1.25fr"),
            new GridColumn(Name: StartTimeColumn, DesktopWidth: "1fr"),
            new GridColumn(Name: TypeColumn, DesktopWidth: "1fr", IsVisible: () => _showResourceTypeColumn),
            new GridColumn(Name: SourceColumn, DesktopWidth: "2.25fr"),
            new GridColumn(Name: UrlsColumn, DesktopWidth: "2.25fr", MobileWidth: "2fr"),
            new GridColumn(Name: ActionsColumn, DesktopWidth: "minmax(150px, 1.5fr)", MobileWidth: "1fr")
        ];

        _hideResourceGraph = DashboardOptions.CurrentValue.UI.DisableResourceGraph ?? false;

        PageViewModel = new ResourcesViewModel
        {
            SelectedViewKind = ResourceViewKind.Table
        };

        _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();
        UpdateMenuButtons();

        var showResourceTypeColumn = await SessionStorage.GetAsync<bool>(BrowserStorageKeys.ResourcesShowResourceTypes);
        if (showResourceTypeColumn.Success)
        {
            _showResourceTypeColumn = showResourceTypeColumn.Value;
        }

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
            var (snapshot, subscription) = await DashboardClient.SubscribeResourcesAsync(_watchTaskCancellationTokenSource.Token);

            // Apply snapshot.
            foreach (var resource in snapshot)
            {
                var added = UpdateFromResource(resource);
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
                                // The new type/state/health status should be visible if it's either
                                // 1) new, or
                                // 2) previously visible
                                t => !PageViewModel.ResourceTypesToVisibility.TryGetValue(t, out var value) || value,
                                s => !PageViewModel.ResourceStatesToVisibility.TryGetValue(s, out var value) || value,
                                s => !PageViewModel.ResourceHealthStatusesToVisibility.TryGetValue(s, out var value) || value);

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
    }

    private bool UpdateFromResource(ResourceViewModel resource)
    {
        var preselectedHiddenResourceTypes = HiddenTypes?.Split(' ').Select(StringUtils.Unescape).ToHashSet();
        var preselectedHiddenResourceStates = HiddenStates?.Split(' ').Select(StringUtils.Unescape).ToHashSet();
        var preselectedHiddenResourceHealthStates = HiddenHealthStates?.Split(' ').Select(StringUtils.Unescape).ToHashSet();

        return UpdateFromResource(
            resource,
            type => preselectedHiddenResourceTypes is null || !preselectedHiddenResourceTypes.Contains(type),
            state => preselectedHiddenResourceStates is null || !preselectedHiddenResourceStates.Contains(state),
            healthStatus => preselectedHiddenResourceHealthStates is null || !preselectedHiddenResourceHealthStates.Contains(healthStatus));
    }

    private bool UpdateFromResource(ResourceViewModel resource, Func<string, bool> resourceTypeVisible, Func<string, bool> stateVisible, Func<string, bool> healthStatusVisible)
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

        PageViewModel.ResourceTypesToVisibility.AddOrUpdate(resource.ResourceType, resourceTypeVisible(resource.ResourceType), (_, _) => resourceTypeVisible(resource.ResourceType));
        PageViewModel.ResourceStatesToVisibility.AddOrUpdate(resource.State ?? string.Empty, stateVisible(resource.State ?? string.Empty), (_, _) => stateVisible(resource.State ?? string.Empty));
        PageViewModel.ResourceHealthStatusesToVisibility.AddOrUpdate(resource.HealthStatus?.Humanize() ?? string.Empty, healthStatusVisible(resource.HealthStatus?.Humanize() ?? string.Empty), (_, _) => healthStatusVisible(resource.HealthStatus?.Humanize() ?? string.Empty));

        UpdateMenuButtons();

        return added;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (PageViewModel.SelectedViewKind == ResourceViewKind.Graph && !_graphInitialized)
        {
            // Before any awaits, set a flag to indicate the graph is initialized. This prevents the graph being initialized multiple times.
            _graphInitialized = true;

            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/js/app-resourcegraph.js");

            _resourcesInteropReference = DotNetObjectReference.Create(new ResourcesInterop(this));

            await _jsModule.InvokeVoidAsync("initializeResourcesGraph", _resourcesInteropReference);
            await UpdateResourceGraphResourcesAsync();
            await UpdateResourceGraphSelectedAsync();
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

        [JSInvokable]
        public async Task ResourceContextMenu(string id, int screenWidth, int screenHeight, int clientX, int clientY)
        {
            if (resources._resourceByName.TryGetValue(id, out var resource))
            {
                await resources.InvokeAsync(async () =>
                {
                    await resources.ShowContextMenuAsync(resource, screenWidth, screenHeight, clientX, clientY);
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

        if (_showResourceTypeColumn)
        {
             _resourcesMenuItems.Add(new MenuButtonItem
            {
                IsDisabled = false,
                OnClick = OnToggleResourceType,
                Text = Loc[nameof(Dashboard.Resources.Resources.ResourcesHideTypes)],
                Icon = new Icons.Regular.Size16.EyeOff()
            });
        }
        else
        {
            _resourcesMenuItems.Add(new MenuButtonItem
            {
                IsDisabled = false,
                OnClick = OnToggleResourceType,
                Text = Loc[nameof(Dashboard.Resources.Resources.ResourcesShowTypes)],
                Icon = new Icons.Regular.Size16.Eye()
            });
        }
    }

    private bool HasCollapsedResources()
    {
        return _resourceByName.Any(r => !r.Value.IsResourceHidden() && _collapsedResourceNames.Contains(r.Key));
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

        // If filters were saved in page state, resource filters now need to be recomputed since the URL has changed.
        foreach (var resourceViewModel in _resourceByName)
        {
            UpdateFromResource(resourceViewModel.Value);
        }

        if (ResourceName is not null)
        {
            if (_resourceByName.TryGetValue(ResourceName, out var selectedResource))
            {
                await ShowResourceDetailsAsync(selectedResource, buttonId: null);
            }

            // Navigate to remove ?resource=xxx in the URL.
            NavigationManager.NavigateTo(DashboardUrls.ResourcesUrl(), new NavigationOptions { ReplaceHistoryEntry = true });
        }

        UpdateTelemetryProperties();
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

    private async Task ShowContextMenuAsync(ResourceViewModel resource, int screenWidth, int screenHeight, int clientX, int clientY)
    {
        // This is called when the browser requests to show the context menu for a resource.
        // The method doesn't complete until the context menu is closed so the browser can await
        // it and perform clean up when the context menu is closed.
        if (_contextMenu is { } contextMenu)
        {
            _contextMenuItems.Clear();
            ResourceMenuItems.AddMenuItems(
                _contextMenuItems,
                openingMenuButtonId: null,
                resource,
                NavigationManager,
                TelemetryRepository,
                GetResourceName,
                ControlsStringsLoc,
                Loc,
                CommandsLoc,
                (buttonId) => ShowResourceDetailsAsync(resource, buttonId),
                (command) => ExecuteResourceCommandAsync(resource, command),
                (resource, command) => DashboardCommandExecutor.IsExecuting(resource.Name, command.Name),
                showConsoleLogsItem: true,
                showUrls: true);

            // The previous context menu should always be closed by this point but complete just in case.
            _contextMenuClosedTcs?.TrySetResult();

            _contextMenuClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await contextMenu.OpenAsync(screenWidth, screenHeight, clientX, clientY);
            StateHasChanged();

            // Completed when the overlay closes.
            await _contextMenuClosedTcs.Task;
        }
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

            if (PageViewModel.SelectedViewKind == ResourceViewKind.Graph)
            {
                await UpdateResourceGraphSelectedAsync();
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
            if (item.IsResourceHidden())
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

    private static string GetUrlsTooltip(ResourceViewModel resource)
    {
        var displayedUrls = GetDisplayedUrls(resource);

        if (displayedUrls.Count == 0)
        {
            return string.Empty;
        }

        if (displayedUrls.Count == 1)
        {
            return displayedUrls[0].Text;
        }

        var maxShownUrls = 3;
        var tooltipBuilder = new StringBuilder(string.Join(", ", displayedUrls.Take(maxShownUrls).Select(url => url.Text)));

        if (displayedUrls.Count > maxShownUrls)
        {
            tooltipBuilder.Append(CultureInfo.CurrentCulture, $" + {displayedUrls.Count - maxShownUrls}");
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
            .Where(r => !r.IsResourceHidden())
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

    private async Task OnToggleResourceType()
    {
        _showResourceTypeColumn = !_showResourceTypeColumn;
        await SessionStorage.SetAsync(BrowserStorageKeys.ResourcesShowResourceTypes, _showResourceTypeColumn);
        await _dataGrid.SafeRefreshDataAsync();
        UpdateMenuButtons();
    }

    private static List<DisplayedUrl> GetDisplayedUrls(ResourceViewModel resource)
    {
        return ResourceUrlHelpers.GetUrls(resource, includeInternalUrls: false, includeNonEndpointUrls: true);
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
        public ConcurrentDictionary<string, bool> ResourceTypesToVisibility { get; } = new(StringComparers.ResourceName);
        public ConcurrentDictionary<string, bool> ResourceStatesToVisibility { get; } = new(StringComparers.ResourceState);
        public ConcurrentDictionary<string, bool> ResourceHealthStatusesToVisibility { get; } = new(StringComparer.Ordinal);
    }

    public class ResourcesPageState
    {
        public required string? ViewKind { get; set; }
        public required IDictionary<string, bool> ResourceTypesToVisibility { get; set; }
        public required IDictionary<string, bool> ResourceStatesToVisibility { get; set; }
        public required IDictionary<string, bool> ResourceHealthStatusesToVisibility { get; set; }
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
        return DashboardUrls.ResourcesUrl(
            view: serializable.ViewKind,
            // add resource?
            hiddenTypes: SerializeFiltersToString(serializable.ResourceTypesToVisibility),
            hiddenStates: SerializeFiltersToString(serializable.ResourceStatesToVisibility),
            hiddenHealthStates: SerializeFiltersToString(serializable.ResourceHealthStatusesToVisibility));

        static string? SerializeFiltersToString(IDictionary<string, bool> filters)
        {
            var escapedFilters = filters.Where(kvp => !kvp.Value).Select(kvp => StringUtils.Escape(kvp.Key)).ToList();
            return escapedFilters.Count == 0 ? null : string.Join(" ", escapedFilters);
        }
    }

    public ResourcesPageState ConvertViewModelToSerializable()
    {
        return new ResourcesPageState
        {
            ViewKind = PageViewModel.SelectedViewKind != ResourceViewKind.Table ? PageViewModel.SelectedViewKind.ToString() : null,
            ResourceTypesToVisibility = PageViewModel.ResourceTypesToVisibility,
            ResourceStatesToVisibility = PageViewModel.ResourceStatesToVisibility,
            ResourceHealthStatusesToVisibility = PageViewModel.ResourceHealthStatusesToVisibility
        };
    }

    public async ValueTask DisposeAsync()
    {
        _resourcesInteropReference?.Dispose();
        _watchTaskCancellationTokenSource.Cancel();
        _watchTaskCancellationTokenSource.Dispose();
        _logsSubscription?.Dispose();
        TelemetryContext.Dispose();

        await JSInteropHelpers.SafeDisposeAsync(_jsModule);

        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);
    }

    private async Task ContextMenuClosed(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (_contextMenu is { } menu)
        {
            await menu.CloseAsync();
        }

        _contextMenuClosedTcs?.TrySetResult();
        _contextMenuClosedTcs = null;
    }

    // IComponentWithTelemetry impl
    public ComponentTelemetryContext TelemetryContext { get; } = new(DashboardUrls.ResourcesBasePath);

    public void UpdateTelemetryProperties()
    {
        var properties = new List<ComponentTelemetryProperty>
        {
            new(TelemetryPropertyKeys.ResourceView, new AspireTelemetryProperty(PageViewModel.SelectedViewKind.ToString(), AspireTelemetryPropertyType.UserSetting)),
            new(TelemetryPropertyKeys.ResourceTypes, new AspireTelemetryProperty(_resourceByName.Values.Select(r => TelemetryPropertyValues.GetResourceTypeTelemetryValue(r.ResourceType)).OrderBy(t => t).ToList()))
        };

        TelemetryContext.UpdateTelemetryProperties(properties.ToArray(), Logger);
    }
}
