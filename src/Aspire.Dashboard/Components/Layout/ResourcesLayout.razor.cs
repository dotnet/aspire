// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Humanizer;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;
using ResourcesPage = Aspire.Dashboard.Components.Pages.Resources;

namespace Aspire.Dashboard.Components.Layout;

/// <summary>
/// Layout for the resource-centric pages (overview, console logs, structured logs, traces and metrics).
/// It hosts the resource list on the left, the page title, and a toolbar with resource identity or Table/Graph tabs.
/// </summary>
/// <remarks>
/// Blazor keeps a layout instance alive while navigating between pages that share it, so the resource list,
/// its scroll position, filters and expanded state survive switching pages or resources. The selected resource
/// and view are derived from the current URL, which keeps every view addressable and lets the browser's
/// back/forward buttons work as expected.
/// </remarks>
public sealed partial class ResourcesLayout : LayoutComponentBase, IAsyncDisposable
{
    private const string FilterButtonId = "resourcePaneFilterButton";
    private const string TelemetryResourceType = "Telemetry";
    private const string TelemetryResourceState = "N/A";
    private static readonly Icon s_checkmarkIcon = new Icons.Regular.Size16.Checkmark();
    private static readonly IComparer<ResourceSelectorItem> s_resourceNameComparer = Comparer<ResourceSelectorItem>.Create((x, y) =>
    {
        if (x.Resource is { } xResource && y.Resource is { } yResource)
        {
            return ResourceViewModelNameComparer.Instance.Compare(xResource, yResource);
        }

        var displayNameComparison = StringComparers.ResourceName.Compare(x.Resource?.DisplayName ?? x.Name, y.Resource?.DisplayName ?? y.Name);
        return displayNameComparison != 0 ? displayNameComparison : StringComparers.ResourceName.Compare(x.Resource?.Name ?? x.Name, y.Resource?.Name ?? y.Name);
    });
    private static readonly IComparer<ResourceSelectorItem> s_resourceTypeAndNameComparer = Comparer<ResourceSelectorItem>.Create((x, y) =>
    {
        var typeComparison = Comparer<string>.Default.Compare(x.ResourceType, y.ResourceType);
        return typeComparison != 0 ? typeComparison : s_resourceNameComparer.Compare(x, y);
    });
    internal const int MinimumPaneWidthPx = 200;
    internal const int MaximumPaneWidthPx = 560;
    internal const int DefaultPaneWidthPx = 290;

    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly HashSet<string> _collapsedResourceNames = new(StringComparers.ResourceName);
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.OrdinalIgnoreCase);
    private ResourceGrouping _grouping;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<MenuButtonItem> _resourceMenuItems = [];
    private readonly List<MenuButtonItem> _paneMenuItems = [];
    private readonly List<CommandViewModel> _highlightedCommands = [];
    private readonly ResourcesPage.ResourcesViewModel _filter = new() { SelectedViewKind = ResourcesPage.ResourceViewKind.Table };

    private Task? _resourceSubscriptionTask;
    private Subscription? _logsSubscription;
    private Subscription? _telemetryResourcesSubscription;
    private Dictionary<ResourceKey, int>? _unviewedErrorCounts;
    private List<TelemetryOnlyResource> _telemetryOnlyResources = [];
    private string? _collapsedResourceNamesKey;
    private bool _isLoaded;
    private bool _isPaneCollapsed;
    private bool _isDrawerOpen;
    private bool _pendingLandingRedirect;
    private bool _hasLocation;
    private string? _lastLocation;
    private bool _isDisposed;
    private IReadOnlyList<string> _selectedResourceNames = [];
    private List<ResourceSelectorItem> _selectedItems = [];
    private string? _selectionAnchor;
    private int? _paneWidth;
    private ElementReference _layoutElement;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<ResourcesLayout>? _selfRef;
    private bool _isResizerRegistered;
    private bool _isOrderingRegistered;
    private string? _orderStatus;

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required DashboardDataSource DataSource { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ILocalStorage LocalStorage { get; init; }

    [Inject]
    public required ISessionStorage SessionStorage { get; init; }

    [Inject]
    public required DashboardCommandExecutor DashboardCommandExecutor { get; init; }

    [Inject]
    public required ResourceMenuBuilder ResourceMenuBuilder { get; init; }

    [Inject]
    public required IconResolver IconResolver { get; init; }

    [Inject]
    public required ResourceTagStore TagStore { get; init; }

    [Inject]
    public required ResourceOrderStore OrderStore { get; init; }

    [Inject]
    public required Microsoft.FluentUI.AspNetCore.Components.INotificationService ToastService { get; init; }

    [Inject]
    public required ResourcePaneState PaneState { get; init; }

    [Inject]
    public required IOptionsMonitor<DashboardOptions> DashboardOptions { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Layout> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Resources> ResourcesLoc { get; init; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    [Inject]
    public required IStringLocalizer<Columns> ColumnsLoc { get; init; }

    [Inject]
    public required ILogger<ResourcesLayout> Logger { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    private ITelemetryRepository TelemetryRepository => DataSource.TelemetryRepository;

    private bool ShowTelemetryOnlyResources => CurrentTab is ResourceTab.StructuredLogs or ResourceTab.Traces or ResourceTab.Metrics;

    /// <summary>
    /// Gets a value indicating whether the initial resource snapshot has been received.
    /// </summary>
    internal bool IsLoaded => _isLoaded;

    /// <summary>
    /// Gets the tab of the current page.
    /// </summary>
    internal ResourceTab CurrentTab { get; private set; }

    /// <summary>
    /// Gets the names of the resources selected in the resource list, in selection order. The list is empty when no
    /// resource is selected, in which case telemetry pages show data from every resource.
    /// </summary>
    internal IReadOnlyList<string> SelectedResourceNames => _selectedResourceNames;

    /// <summary>
    /// Gets a value indicating whether more than one resource is selected.
    /// </summary>
    internal bool IsMultiSelection => _selectedResourceNames.Count > 1;

    internal bool ShowResourceViewTabs => DashboardClient.IsEnabled &&
        CurrentTab is ResourceTab.Overview or ResourceTab.Graph && _selectedResourceNames.Count != 1;

    /// <summary>
    /// Gets the resource name when exactly one resource is selected, otherwise <c>null</c>.
    /// </summary>
    internal string? SelectedResourceName => _selectedResourceNames.Count == 1 ? _selectedResourceNames[0] : null;

    /// <summary>
    /// The name of the section in the desktop tabs row where tab pages render their filter controls.
    /// </summary>
    internal const string TabToolbarSectionName = "resource-tab-toolbar";

    /// <summary>
    /// Gets the app model resource that matches <see cref="SelectedResourceName"/>, if any.
    /// </summary>
    internal ResourceViewModel? SelectedResource { get; private set; }

    /// <summary>
    /// Gets the app model resources of a multi-resource selection. Selected resources that only send telemetry are
    /// not included.
    /// </summary>
    internal IEnumerable<ResourceViewModel> SelectedResources => _selectedItems.Select(i => i.Resource).OfType<ResourceViewModel>();

    internal IReadOnlyList<string> AvailableResourceTags => TagStore.GetAllTags(_resourceByName.Values);

    internal ConcurrentDictionary<string, ResourceViewModel> ResourceByName => _resourceByName;

    /// <summary>
    /// Raised on the renderer's synchronization context after resources change, so pages that display data owned by
    /// the layout (such as the resource overview) can refresh.
    /// </summary>
    internal event Action? ResourcesChanged;

    protected override async Task OnInitializedAsync()
    {
        // Switching between the live view and a recording, including restoring one when the dashboard opens,
        // re-creates the page and can dispose this layout while it's still initializing. Capture the token before
        // the first await: DisposeAsync cancels and then disposes the source, after which its Token throws.
        var cancellationToken = _cts.Token;

        _lastLocation = NavigationManager.Uri;
        UpdateFromLocation(_lastLocation);

        _unviewedErrorCounts = TelemetryRepository.GetResourceUnviewedErrorLogsCount();
        UpdateTelemetryOnlyResources();

        var paneCollapsedResult = await LocalStorage.GetUnprotectedAsync<bool>(BrowserStorageKeys.ResourcePaneCollapsed);
        if (paneCollapsedResult.Success)
        {
            _isPaneCollapsed = paneCollapsedResult.Value;
        }

        var paneWidthResult = await LocalStorage.GetUnprotectedAsync<int>(BrowserStorageKeys.ResourcePaneWidth);
        if (paneWidthResult.Success)
        {
            _paneWidth = Math.Clamp(paneWidthResult.Value, MinimumPaneWidthPx, MaximumPaneWidthPx);
        }

        // Migrate the former Tags navigation view to the resource selector's grouping preference.
        var paneModeResult = await LocalStorage.GetUnprotectedAsync<string>(BrowserStorageKeys.ResourcePaneMode);
        if (DashboardClient.IsEnabled && paneModeResult.Success &&
            string.Equals(paneModeResult.Value, nameof(ResourceGrouping.Tags), StringComparison.OrdinalIgnoreCase))
        {
            _grouping = ResourceGrouping.Tags;
        }
        var groupingResult = await LocalStorage.GetUnprotectedAsync<string>(BrowserStorageKeys.ResourcePaneGrouping);
        if (groupingResult.Success && Enum.TryParse<ResourceGrouping>(groupingResult.Value, out var grouping) && Enum.IsDefined(grouping))
        {
            _grouping = grouping;
        }
        else if (_grouping == ResourceGrouping.Tags)
        {
            await LocalStorage.SetUnprotectedAsync(BrowserStorageKeys.ResourcePaneGrouping, _grouping.ToString());
        }
        await ApplyLegacyTagGroupingAsync(_lastLocation);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        TagStore.Changed += OnTagsChanged;
        OrderStore.Changed += OnOrderChanged;

        var showHiddenResources = await SessionStorage.GetAsync<bool>(BrowserStorageKeys.ResourcesShowHiddenResources);
        if (showHiddenResources.Success)
        {
            _filter.ShowHiddenResources = showHiddenResources.Value;
        }

        if (DashboardClient.IsEnabled)
        {
            // The application name is only correct once the dashboard is connected, and it scopes persisted tree state.
            await DashboardClient.WhenConnected;
            _collapsedResourceNamesKey = BrowserStorageKeys.CollapsedResourceNamesKey(DashboardClient.ApplicationName);
            var collapsedResult = await LocalStorage.GetAsync<List<string>>(_collapsedResourceNamesKey);
            if (collapsedResult.Success)
            {
                foreach (var resourceName in collapsedResult.Value)
                {
                    _collapsedResourceNames.Add(resourceName);
                }
            }
        }

        // Don't subscribe after DisposeAsync has run: nothing would dispose these subscriptions.
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        _logsSubscription = TelemetryRepository.OnNewLogs(null, SubscriptionType.Other, async () =>
        {
            var counts = TelemetryRepository.GetResourceUnviewedErrorLogsCount();
            await InvokeAsync(() =>
            {
                _unviewedErrorCounts = counts;
                StateHasChanged();
            });
        });

        _telemetryResourcesSubscription = TelemetryRepository.OnNewResources(async () =>
        {
            await InvokeAsync(() =>
            {
                UpdateTelemetryOnlyResources();
                EnsureGroupOrder();
                UpdateSelection();
                StateHasChanged();
                ResourcesChanged?.Invoke();
            });
        });

        // Standalone resources come from OTLP. The disabled AppHost client cannot be awaited or subscribed to.
        if (!DashboardClient.IsEnabled)
        {
            _isLoaded = true;
            UpdateTelemetryOnlyResources();
            UpdateSelection();
            ResourcesChanged?.Invoke();
            return;
        }

        var (snapshot, subscription) = await DataSource.ResourceRepository.SubscribeResourcesAsync(cancellationToken);
        foreach (var resource in snapshot)
        {
            UpsertResource(resource);
        }
        _isLoaded = true;
        UpdateTelemetryOnlyResources();
        EnsureGroupOrder();
        UpdateSelection();
        UpdatePaneMenuItems();
        await TryRedirectLandingAsync();

        _resourceSubscriptionTask = Task.Run(async () =>
        {
            await foreach (var changes in subscription.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (var (changeType, resource) in changes)
                {
                    if (changeType == ResourceViewModelChangeType.Upsert)
                    {
                        UpsertResource(resource);
                    }
                    else if (changeType == ResourceViewModelChangeType.Delete)
                    {
                        _resourceByName.TryRemove(resource.Name, out _);
                    }
                }

                await InvokeAsync(async () =>
                {
                    UpdateTelemetryOnlyResources();
                    EnsureGroupOrder();
                    UpdateSelection();
                    UpdatePaneMenuItems();
                    await TryRedirectLandingAsync();
                    StateHasChanged();
                    ResourcesChanged?.Invoke();
                });
            }
        });

        ResourcesChanged?.Invoke();
    }

    private void UpsertResource(ResourceViewModel resource)
    {
        _resourceByName[resource.Name] = resource;

        // New types and states are visible unless the user has hidden them already.
        if (!resource.IsParameter)
        {
            _filter.ResourceTypesToVisibility.TryAdd(resource.ResourceType, true);
        }
        _filter.ResourceStatesToVisibility.TryAdd(resource.State ?? string.Empty, true);
        _filter.ResourceHealthStatusesToVisibility.TryAdd(resource.HealthStatus?.Humanize() ?? string.Empty, true);
    }

    protected override async Task OnParametersSetAsync()
    {
        // The router sets a new body on every navigation. Selection is updated here, rather than in a
        // LocationChanged handler, so it's current before the page in the body renders.
        var location = NavigationManager.Uri;
        if (string.Equals(location, _lastLocation, StringComparison.Ordinal))
        {
            return;
        }

        _lastLocation = location;
        var previousSelection = _selectedResourceNames;
        UpdateFromLocation(location);
        await ApplyLegacyTagGroupingAsync(location);
        EnsureGroupOrder();
        _isDrawerOpen = false;
        await TryRedirectLandingAsync();
        await PersistSelectedResourcesAsync();

        if (!previousSelection.SequenceEqual(_selectedResourceNames, StringComparers.ResourceName))
        {
            // Pages that render the selection, such as the overview, aren't always given new parameters when only
            // the selection changes, so notify them.
            ResourcesChanged?.Invoke();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var showResizer = ViewportInformation.IsDesktop && !_isPaneCollapsed;
        if (showResizer == _isResizerRegistered && _isOrderingRegistered)
        {
            return;
        }

        try
        {
            _jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", $"./{Assets["Components/Layout/ResourcesLayout.razor.js"]}");
            _selfRef ??= DotNetObjectReference.Create(this);
            if (!_isOrderingRegistered)
            {
                await _jsModule.InvokeVoidAsync("registerResourceOrdering", _layoutElement, _selfRef);
                _isOrderingRegistered = true;
            }
            if (showResizer)
            {
                await _jsModule.InvokeVoidAsync("registerPaneResizer", _layoutElement, _selfRef, MinimumPaneWidthPx, MaximumPaneWidthPx);
            }
            else
            {
                await _jsModule.InvokeVoidAsync("unregisterPaneResizer", _layoutElement);
            }

            _isResizerRegistered = showResizer;
        }
        catch (JSDisconnectedException)
        {
            // The circuit disconnected while rendering. There is nothing to register.
        }
    }

    /// <summary>
    /// Called by the pane resizer after the user resizes the resource list.
    /// </summary>
    [JSInvokable]
    public async Task SetPaneWidthAsync(int widthPx)
    {
        _paneWidth = Math.Clamp(widthPx, MinimumPaneWidthPx, MaximumPaneWidthPx);
        await LocalStorage.SetUnprotectedAsync(BrowserStorageKeys.ResourcePaneWidth, _paneWidth.Value);
        StateHasChanged();
    }

    private string? GetLayoutStyle() => _paneWidth is { } width && ViewportInformation.IsDesktop
        ? string.Create(CultureInfo.InvariantCulture, $"--resource-pane-expanded-width: {width}px")
        : null;

    /// <summary>
    /// Derives the tab and selected resources from a URL such as <c>/traces/resource/api?type=http</c>.
    /// </summary>
    private void UpdateFromLocation(string location)
    {
        var parsedLocation = ParseLocation(NavigationManager.ToBaseRelativePath(location));
        var wasAllResources = _hasLocation && _selectedResourceNames.Count == 0;
        _hasLocation = true;

        CurrentTab = parsedLocation.Tab;
        RouteResourceName = parsedLocation.RouteResourceName;
        if (!parsedLocation.KeepSelection)
        {
            _selectedResourceNames = parsedLocation.SelectedResourceNames;
        }

        // An explicit empty selection (?resource=) must show all resources, even when navigation removed every
        // OTLP-only source. Only an unqualified landing URL restores the last selected resource.
        var hasExplicitSelection = QueryHelpers.ParseQuery(new Uri(location).Query).ContainsKey(DashboardUrls.ResourceSelectionQueryName);
        _pendingLandingRedirect = DashboardClient.IsEnabled && parsedLocation.Tab == ResourceTab.Overview &&
            parsedLocation.SelectedResourceNames.Count == 0 && !wasAllResources && !hasExplicitSelection;
        UpdateSelection();
        if (CurrentTab == ResourceTab.Graph &&
            (DashboardOptions.CurrentValue.UI.DisableResourceGraph == true || _selectedResourceNames.Count == 1))
        {
            CurrentTab = ResourceTab.Overview;
            NavigationManager.NavigateTo(GetTabUrl(ResourceTab.Overview), new NavigationOptions { ReplaceHistoryEntry = true });
        }
    }

    /// <summary>
    /// Gets the resource in the URL path of the current page. It's usually the selected resource, but with several
    /// selected resources, the metrics page uses it for the resource whose instruments are displayed.
    /// </summary>
    internal string? RouteResourceName { get; private set; }

    /// <summary>
    /// Gets the URL of the given tab for the resources currently selected in the resource list.
    /// </summary>
    internal string GetTabUrl(ResourceTab tab) => GetTabUrl(tab, _selectedResourceNames, RouteResourceName);

    private async Task ApplyLegacyTagGroupingAsync(string location)
    {
        if (DashboardClient.IsEnabled && IsTagGroupingRequested(NavigationManager.ToBaseRelativePath(location)))
        {
            await SetGroupingAsync(ResourceGrouping.Tags);
        }
    }

    /// <summary>
    /// Gets whether a legacy URL such as <c>/resources?pane=tags</c> requests tag grouping.
    /// </summary>
    internal static bool IsTagGroupingRequested(string baseRelativePath)
    {
        var queryIndex = baseRelativePath.IndexOf('?');
        if (queryIndex < 0)
        {
            return false;
        }

        var query = baseRelativePath[queryIndex..];
        var fragmentIndex = query.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            query = query[..fragmentIndex];
        }

        if (!QueryHelpers.ParseQuery(query).TryGetValue(DashboardUrls.ResourcePaneQueryName, out var values))
        {
            return false;
        }

        return string.Equals(values.ToString(), DashboardUrls.ResourcePaneTagsValue, StringComparisons.UrlPath);
    }

    internal static ResourceLocation ParseLocation(string baseRelativePath)
    {
        var path = baseRelativePath;
        var query = string.Empty;
        var fragmentIndex = path.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            path = path[..fragmentIndex];
        }
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            query = path[queryIndex..];
            path = path[..queryIndex];
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return new ResourceLocation(ResourceTab.Overview, RouteResourceName: null, SelectedResourceNames: [], KeepSelection: false);
        }

        // Trace details are addressed by trace id. Keep the previously selected resources so the resource header
        // and tabs stay stable while drilling into a trace.
        if (string.Equals(segments[0], DashboardUrls.TracesBasePath, StringComparisons.UrlPath) &&
            segments.Length > 1 &&
            string.Equals(segments[1], "detail", StringComparisons.UrlPath))
        {
            return new ResourceLocation(ResourceTab.Traces, RouteResourceName: null, SelectedResourceNames: [], KeepSelection: true);
        }

        var tab = segments[0].ToLowerInvariant() switch
        {
            DashboardUrls.ResourceOverviewBasePath when QueryHelpers.ParseQuery(query).TryGetValue("view", out var view) &&
                string.Equals(view, "graph", StringComparisons.UrlPath) => ResourceTab.Graph,
            DashboardUrls.ConsoleLogBasePath => ResourceTab.Console,
            DashboardUrls.StructuredLogsBasePath => ResourceTab.StructuredLogs,
            DashboardUrls.TracesBasePath => ResourceTab.Traces,
            DashboardUrls.MetricsBasePath => ResourceTab.Metrics,
            _ => ResourceTab.Overview
        };

        string? routeResourceName = null;
        if (tab == ResourceTab.Overview)
        {
            // resources/{name}
            if (segments.Length > 1)
            {
                routeResourceName = Uri.UnescapeDataString(segments[1]);
            }
        }
        else if (segments.Length > 2 && string.Equals(segments[1], "resource", StringComparisons.UrlPath))
        {
            // {page}/resource/{name}
            routeResourceName = Uri.UnescapeDataString(segments[2]);
        }

        // Several selected resources are listed in the query string, for example ?resource=api&resource=worker.
        var selectedResourceNames = new List<string>();
        if (QueryHelpers.ParseQuery(query).TryGetValue(DashboardUrls.ResourceSelectionQueryName, out var queryValues))
        {
            foreach (var value in queryValues)
            {
                if (!string.IsNullOrEmpty(value) && !selectedResourceNames.Contains(value, StringComparers.ResourceName))
                {
                    selectedResourceNames.Add(value);
                }
            }
        }

        if (selectedResourceNames.Count == 0 && routeResourceName is not null)
        {
            selectedResourceNames.Add(routeResourceName);
        }

        return new ResourceLocation(tab, routeResourceName, selectedResourceNames, KeepSelection: false);
    }

    private void UpdateSelection()
    {
        _selectedItems = _selectedResourceNames.Select(name =>
        {
            if (ResourceViewModel.TryGetResourceByName(name, _resourceByName, out var resource))
            {
                return new ResourceSelectorItem(name, resource, TelemetryOnly: null);
            }

            var telemetryOnly = _telemetryOnlyResources.FirstOrDefault(r => string.Equals(r.Name, name, StringComparisons.ResourceName));
            return new ResourceSelectorItem(name, Resource: null, telemetryOnly);
        }).ToList();

        SelectedResource = _selectedItems is [{ Resource: { } selected }] ? selected : null;
        PaneState.SetSelectedResources(_selectedResourceNames);

        UpdateResourceMenuItems();
    }

    private async Task TryRedirectLandingAsync()
    {
        if (!_pendingLandingRedirect || !_isLoaded)
        {
            return;
        }

        _pendingLandingRedirect = false;

        List<string> resourceNames;
        try
        {
            var lastSelected = await SessionStorage.GetAsync<List<string>>(BrowserStorageKeys.LastSelectedResources);
            if (lastSelected is { Success: true, Value: { } names })
            {
                // An empty list means the user cleared the selection, so the overview asks them to select resources.
                resourceNames = names.Where(IsKnownResourceName).ToList();
                if (names.Count > 0 && resourceNames.Count == 0 && GetPaneRows().FirstOrDefault() is { } fallbackRow)
                {
                    resourceNames.Add(fallbackRow.Name);
                }
            }
            else
            {
                resourceNames = GetPaneRows().FirstOrDefault() is { } firstRow ? [firstRow.Name] : [];
            }
        }
        catch (JSDisconnectedException)
        {
            return;
        }

        if (resourceNames.Count > 0)
        {
            NavigationManager.NavigateTo(GetTabUrl(ResourceTab.Overview, resourceNames), new NavigationOptions { ReplaceHistoryEntry = true });
        }
    }

    private bool IsKnownResourceName(string name) =>
        ResourceViewModel.TryGetResourceByName(name, _resourceByName, out _);

    private async Task PersistSelectedResourcesAsync()
    {
        // Navigating to a page without a selection, such as all structured logs, doesn't clear the remembered
        // selection. Only clearing the selection in the resource list does.
        if (_selectedResourceNames.Count == 0)
        {
            return;
        }

        await SetStoredSelectionAsync(_selectedResourceNames);
    }

    private async Task SetStoredSelectionAsync(IReadOnlyList<string> resourceNames)
    {
        if (!DashboardClient.IsEnabled)
        {
            return;
        }

        try
        {
            await SessionStorage.SetAsync(BrowserStorageKeys.LastSelectedResources, resourceNames.ToList());
        }
        catch (JSDisconnectedException)
        {
            // The circuit disconnected while navigating. There is nothing to persist to.
        }
    }

    private void UpdateTelemetryOnlyResources()
    {
        // Match telemetry to the app model before combining the lists, so instrumented app host resources
        // aren't listed twice. Sources outside the model (for example a browser app) only appear on telemetry pages.
        var telemetryResources = TelemetryRepository.GetResources();
        var appModelKeys = new HashSet<ResourceKey>();
        foreach (var resource in _resourceByName.Values)
        {
            if (TelemetryRepository.GetResourceByCompositeName(resource.Name) is { } otlpResource)
            {
                appModelKeys.Add(otlpResource.ResourceKey);
            }
        }

        _telemetryOnlyResources = telemetryResources
            .Where(r => !appModelKeys.Contains(r.ResourceKey))
            .Select(r => new TelemetryOnlyResource(OtlpHelpers.GetResourceName(r, telemetryResources), r))
            .OrderBy(r => r.Name, StringComparers.ResourceName)
            .ToList();
        PaneState.SetTelemetryOnlyResources(_telemetryOnlyResources.Select(r => r.Name));
        if (_telemetryOnlyResources.Count > 0)
        {
            _filter.ResourceTypesToVisibility.TryAdd(TelemetryResourceType, true);
            _filter.ResourceStatesToVisibility.TryAdd(TelemetryResourceState, true);
            _filter.ResourceHealthStatusesToVisibility.TryAdd(string.Empty, true);
        }
    }

    private IEnumerable<ResourceSelectorItem> GetResourceItems() =>
        _resourceByName.Values.Select(resource => new ResourceSelectorItem(GetResourceName(resource), resource, TelemetryOnly: null))
            .Concat(ShowTelemetryOnlyResources
                ? _telemetryOnlyResources.Select(resource => new ResourceSelectorItem(resource.Name, Resource: null, resource))
                : []);

    private bool FilterResourceItem(ResourceSelectorItem item, string textFilter) =>
        item.Resource is { } resource
            ? _filter.Filter(resource, textFilter)
            : _filter.ResourceTypesToVisibility.GetValueOrDefault(TelemetryResourceType, true) &&
                _filter.ResourceStatesToVisibility.GetValueOrDefault(TelemetryResourceState, true) &&
                _filter.ResourceHealthStatusesToVisibility.GetValueOrDefault(string.Empty, true) &&
                MatchesFilter(item, textFilter);

    private static bool MatchesFilter(ResourceSelectorItem item, string textFilter) =>
        item.Resource is { } resource ? resource.MatchesFilter(textFilter) : item.Name.Contains(textFilter, StringComparisons.UserTextSearch);

    private IEnumerable<ResourceSelectorItem> GetPaneRows()
    {
        var items = GetResourceItems()
            .Where(item => FilterResourceItem(item, _filter.TextFilter))
            .OrderBy(item => item, s_resourceTypeAndNameComparer)
            .ToList();
        var filteredResources = items.Select(item => item.Resource).OfType<ResourceViewModel>()
            .Select(resource => new ResourceGridViewModel { Resource = resource })
            .ToList();

        var rows = ResourceGridViewModel.OrderNestedResources(filteredResources, r => _collapsedResourceNames.Contains(r.PersistentKey))
            .Where(r => !r.IsHidden);
        using var telemetryItems = items.Where(item => item.TelemetryOnly is not null).GetEnumerator();
        var hasTelemetryItem = telemetryItems.MoveNext();
        foreach (var row in rows)
        {
            var item = new ResourceSelectorItem(GetResourceName(row.Resource), row.Resource, TelemetryOnly: null) { Row = row };
            // Match the table's type/name sort, keeping nested children beside their parent rather than
            // inserting a telemetry source between a parent and its children.
            while (row.Depth == 0 && hasTelemetryItem && s_resourceTypeAndNameComparer.Compare(telemetryItems.Current, item) < 0)
            {
                yield return telemetryItems.Current;
                hasTelemetryItem = telemetryItems.MoveNext();
            }
            yield return item;
        }
        while (hasTelemetryItem)
        {
            yield return telemetryItems.Current;
            hasTelemetryItem = telemetryItems.MoveNext();
        }
    }

    /// <summary>
    /// Gets the filtered resources grouped by the selected property. Text search matches group names or resources.
    /// </summary>
    private List<ResourceGroup> GetResourceGroups()
    {
        var groups = _grouping == ResourceGrouping.Tags ? GetTagGroups() : GetResourceItems()
            .Where(item => FilterResourceItem(item, string.Empty))
            .GroupBy(item => _grouping == ResourceGrouping.ResourceType ? item.ResourceType : item.State, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ResourceGroup(group.Key,
                group.Where(item => GetGroupName(group.Key).Contains(_filter.TextFilter, StringComparisons.UserTextSearch) || MatchesFilter(item, _filter.TextFilter))
                    .OrderBy(item => item, s_resourceNameComparer).ToList()))
            .Where(group => group.Resources.Count > 0)
            .OrderBy(group => GetGroupName(group.Value), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var positions = OrderStore.GetGroupPositions(_grouping.ToString());
        return groups.OrderBy(group => positions.GetValueOrDefault(GetGroupOrderKey(group), long.MaxValue)).ToList();
    }

    private string GetGroupName(string? value) => GetGroupName(_grouping, value);

    private string GetGroupName(ResourceGrouping grouping, string? value) => grouping switch
    {
        ResourceGrouping.Tags => value ?? Loc[nameof(Resources.Layout.ResourcePaneUntaggedGroup)],
        ResourceGrouping.State when value == TelemetryResourceState => Loc[nameof(Resources.Layout.ResourcePaneNotApplicableState)],
        ResourceGrouping.ResourceType when value == TelemetryResourceType => Loc[nameof(Resources.Layout.ResourcePaneTelemetryType)],
        ResourceGrouping.State => string.IsNullOrEmpty(value) ? ColumnsLoc[nameof(Columns.UnknownStateLabel)] : value.Humanize(),
        _ => value ?? string.Empty
    };

    private void EnsureGroupOrder()
    {
        if (!_isLoaded || !DashboardClient.IsEnabled)
        {
            return;
        }

        // Register groups independently of search/type/state/health filters, so a filtered view cannot change
        // their saved order. Hidden resources participate once the user chooses to show them.
        var items = GetResourceItems().Where(item => item.Resource is not { } resource ||
            (!resource.IsParameter && !resource.IsResourceHidden(_filter.ShowHiddenResources))).ToList();
        var resources = items.Select(item => item.Resource).OfType<ResourceViewModel>().ToList();
        var tagsByResource = TagStore.GetTagsByResource(resources);
        var tags = TagStore.GetAllTags(resources)
            .Where(tag => resources.Any(r => tagsByResource.TryGetValue(r.DisplayName, out var resourceTags) && resourceTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            .ToList();
        if (items.Any(item => item.Resource is null) || resources.Any(r => !tagsByResource.ContainsKey(r.DisplayName)))
        {
            tags.Add(string.Empty);
        }
        OrderStore.EnsureGroups(nameof(ResourceGrouping.Tags), tags);
        OrderStore.EnsureGroups(nameof(ResourceGrouping.ResourceType), items.Select(item => item.ResourceType)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.CurrentCultureIgnoreCase));
        OrderStore.EnsureGroups(nameof(ResourceGrouping.State), items.Select(item => item.State)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => GetGroupName(ResourceGrouping.State, value), StringComparer.CurrentCultureIgnoreCase));
    }

    // Tags are normalized to nonempty values, so the empty key uniquely identifies the synthetic untagged group.
    private static string GetGroupOrderKey(ResourceGroup group) => group.Value ?? string.Empty;

    private List<MenuButtonItem> GetGroupingMenuItems() =>
        Enum.GetValues<ResourceGrouping>().Select(grouping => new MenuButtonItem
        {
            Id = $"resource-grouping-{grouping}",
            Text = Loc[grouping switch
            {
                ResourceGrouping.None => nameof(Resources.Layout.ResourcePaneGroupingNone),
                ResourceGrouping.Tags => nameof(Resources.Layout.NavMenuTagsTab),
                ResourceGrouping.ResourceType => nameof(Resources.Layout.ResourcePaneGroupingResourceType),
                _ => nameof(Resources.Layout.ResourcePaneGroupingState)
            }],
            Tooltip = string.Empty,
            Role = MenuItemRole.Radio,
            Icon = s_checkmarkIcon,
            Checked = _grouping == grouping,
            OnClick = () => SetGroupingAsync(grouping)
        }).ToList();

    private async Task SetGroupingAsync(ResourceGrouping grouping)
    {
        _grouping = grouping;
        StateHasChanged();
        await LocalStorage.SetUnprotectedAsync(BrowserStorageKeys.ResourcePaneGrouping, grouping.ToString());
    }

    // Resources with multiple tags appear in each group; untagged resources initially appear last.
    private List<ResourceGroup> GetTagGroups()
    {
        var textFilter = _filter.TextFilter;
        var items = GetResourceItems()
            .Where(item => FilterResourceItem(item, string.Empty))
            .OrderBy(item => item, s_resourceTypeAndNameComparer)
            .ToList();

        var resources = items.Select(item => item.Resource).OfType<ResourceViewModel>().ToList();
        var tagsByResource = TagStore.GetTagsByResource(resources);
        var groups = new List<ResourceGroup>();
        foreach (var tag in TagStore.GetAllTags(resources))
        {
            var tagMatchesFilter = textFilter.Length == 0 || tag.Contains(textFilter, StringComparisons.UserTextSearch);
            var taggedResources = items
                .Where(item => item.Resource is { } resource && tagsByResource.TryGetValue(resource.DisplayName, out var tags) && tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .Where(item => tagMatchesFilter || MatchesFilter(item, textFilter))
                .ToList();

            if (taggedResources.Count > 0)
            {
                groups.Add(new ResourceGroup(tag, taggedResources));
            }
        }

        var untaggedResources = items
            .Where(item => item.Resource is null || !tagsByResource.ContainsKey(item.Resource.DisplayName))
            .Where(item => MatchesFilter(item, textFilter))
            .ToList();

        if (untaggedResources.Count > 0)
        {
            groups.Add(new ResourceGroup(Value: null, untaggedResources));
        }

        return groups;
    }

    private bool IsGroupCollapsed(ResourceGroup group) => _collapsedGroups.Contains(GetGroupKey(group));

    private void ToggleGroup(ResourceGroup group)
    {
        var key = GetGroupKey(group);
        if (!_collapsedGroups.Remove(key))
        {
            _collapsedGroups.Add(key);
        }
    }

    private string GetGroupKey(ResourceGroup group) => $"{_grouping}:{group.Value}";

    private void OnTagsChanged() => _ = InvokeAsync(() =>
    {
        if (_isLoaded)
        {
            EnsureGroupOrder();
        }
        StateHasChanged();
    });

    private List<MenuButtonItem> GetFilterMenuItems()
    {
        var items = new List<MenuButtonItem>
        {
            new()
            {
                IsGroupHeader = true,
                Text = Loc[nameof(Resources.Layout.ResourcePaneGrouping)],
                Tooltip = string.Empty,
                Id = "resource-grouping-header",
                RenderKey = "grouping-header"
            }
        };
        items.AddRange(GetGroupingMenuItems());
        AddFilterGroup(items, "types", ResourcesLoc[nameof(Dashboard.Resources.Resources.ResourcesResourceTypesHeader)], _filter.ResourceTypesToVisibility);
        AddFilterGroup(items, "states", ResourcesLoc[nameof(Dashboard.Resources.Resources.ResourcesResourceStatesHeader)], _filter.ResourceStatesToVisibility);
        AddFilterGroup(items, "health", ResourcesLoc[nameof(Dashboard.Resources.Resources.ResourcesDetailsHealthStateProperty)], _filter.ResourceHealthStatusesToVisibility);
        items.Add(new MenuButtonItem { IsDivider = true, RenderKey = "hidden-resources-divider" });
        CommonMenuItems.AddToggleHiddenResourcesMenuItem(
            items,
            ControlsStringsLoc,
            _filter.ShowHiddenResources,
            _resourceByName.Values,
            SessionStorage,
            EventCallback.Factory.Create<bool>(this, value =>
            {
                _filter.ShowHiddenResources = value;
                EnsureGroupOrder();
                UpdatePaneMenuItems();
            }));
        items[^1].Tooltip = string.Empty;
        return items;
    }

    private void AddFilterGroup(List<MenuButtonItem> items, string groupId, string header, ConcurrentDictionary<string, bool> values)
    {
        if (items.Count > 0)
        {
            items.Add(new MenuButtonItem { IsDivider = true, RenderKey = $"{groupId}-divider" });
        }

        items.Add(new MenuButtonItem
        {
            IsGroupHeader = true,
            Text = header,
            Tooltip = string.Empty,
            Id = $"resource-filter-{groupId}-header",
            RenderKey = $"{groupId}-header"
        });

        // The menu is regenerated after every toggle and the toggled item is refocused by id, so ids
        // must be stable across regenerations. Values can contain characters that aren't valid in an
        // id, so use the item's sorted position instead.
        // OrderBy doesn't use thread safe APIs on ConcurrentDictionary. Call ToArray first.
        var sortedValues = values.ToArray().OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToList();

        // A menu checkbox can't show an indeterminate state, so "All" is checked only when every value
        // is visible. Checking it shows every value; unchecking it hides every value.
        items.Add(CreateFilterMenuItem(
            id: $"resource-filter-{groupId}-all",
            renderKey: $"{groupId}-all",
            text: ControlsStringsLoc[nameof(ControlsStrings.LabelAll)],
            isChecked: sortedValues.All(pair => pair.Value),
            onCheckedChanged: isChecked =>
            {
                foreach (var key in values.Keys)
                {
                    values[key] = isChecked;
                }
            }));

        for (var i = 0; i < sortedValues.Count; i++)
        {
            var (key, isVisible) = sortedValues[i];
            items.Add(CreateFilterMenuItem(
                id: $"resource-filter-{groupId}-{i}",
                renderKey: $"{groupId}:{key}",
                text: (groupId, key) switch
                {
                    ("types", TelemetryResourceType) => Loc[nameof(Resources.Layout.ResourcePaneTelemetryType)],
                    ("states", TelemetryResourceState) => Loc[nameof(Resources.Layout.ResourcePaneNotApplicableState)],
                    (_, "") => ResourcesLoc[nameof(Dashboard.Resources.Resources.ResourceFilterOptionEmpty)],
                    _ => key
                },
                isChecked: isVisible,
                onCheckedChanged: isChecked => values[key] = isChecked));
        }
    }

    private MenuButtonItem CreateFilterMenuItem(string id, string renderKey, string text, bool isChecked, Action<bool> onCheckedChanged)
    {
        return new MenuButtonItem
        {
            Id = id,
            RenderKey = renderKey,
            Text = text,
            Tooltip = string.Empty,
            Icon = s_checkmarkIcon,
            Role = MenuItemRole.Checkbox,
            Checked = isChecked,
            OnCheckedChanged = value =>
            {
                onCheckedChanged(value);

                // The toggle comes from the menu component, so re-render the pane to apply the filter.
                StateHasChanged();
                return Task.CompletedTask;
            }
        };
    }

    private bool NoFiltersSet =>
        _filter.ResourceTypesToVisibility.Values.All(v => v) &&
        _filter.ResourceStatesToVisibility.Values.All(v => v) &&
        _filter.ResourceHealthStatusesToVisibility.Values.All(v => v);

    private bool CanReorderPane =>
        _isLoaded && DashboardClient.IsEnabled &&
        _filter.TextFilter.Length == 0 && NoFiltersSet &&
        !(ViewportInformation.IsDesktop && _isPaneCollapsed);

    private bool CanReorderGroups => CanReorderPane && _grouping != ResourceGrouping.None;

    private void OnOrderChanged() => _ = InvokeAsync(StateHasChanged);

    [JSInvokable]
    public async Task<bool> ReorderGroupAsync(string grouping, string groupKey, string targetKey, bool after)
    {
        var groups = CanReorderGroups && string.Equals(grouping, _grouping.ToString(), StringComparison.Ordinal)
            ? GetResourceGroups() : [];
        var group = groups.Find(group => StringComparers.ResourceName.Equals(GetGroupKey(group), groupKey));
        var target = groups.Find(group => StringComparers.ResourceName.Equals(GetGroupKey(group), targetKey));
        if (group is null || target is null)
        {
            Logger.LogDebug("Rejected group reorder from '{GroupKey}' to '{TargetKey}' because the grouping or visible list changed.", groupKey, targetKey);
            await ToastService.ShowWarningToastAsync(Loc[nameof(Resources.Layout.ResourcePaneGroupReorderUnavailable)]);
            return false;
        }
        if (!OrderStore.TryMoveGroup(grouping, GetGroupOrderKey(group), GetGroupOrderKey(target), after))
        {
            await ToastService.ShowErrorToastAsync(Loc[nameof(Resources.Layout.ResourcePaneGroupReorderFailed)]);
            return false;
        }

        _orderStatus = string.Format(CultureInfo.CurrentCulture,
            Loc[after ? nameof(Resources.Layout.ResourcePaneMovedAfter) : nameof(Resources.Layout.ResourcePaneMovedBefore)],
            GetGroupName(group.Value), GetGroupName(target.Value));
        StateHasChanged();
        return true;
    }

    private async Task OnToggleCollapseAsync(ResourceGridViewModel viewModel)
    {
        if (!_collapsedResourceNames.Remove(viewModel.Resource.PersistentKey))
        {
            _collapsedResourceNames.Add(viewModel.Resource.PersistentKey);
        }

        await PersistCollapsedResourceNamesAsync();
        UpdatePaneMenuItems();
    }

    private async Task PersistCollapsedResourceNamesAsync()
    {
        if (_collapsedResourceNamesKey is not null)
        {
            await LocalStorage.SetAsync(_collapsedResourceNamesKey, _collapsedResourceNames.ToList());
        }
    }

    private async Task TogglePaneCollapsedAsync()
    {
        _isPaneCollapsed = !_isPaneCollapsed;
        await LocalStorage.SetUnprotectedAsync(BrowserStorageKeys.ResourcePaneCollapsed, _isPaneCollapsed);
    }

    private void OpenDrawer() => _isDrawerOpen = true;

    private void CloseDrawer() => _isDrawerOpen = false;

    private void UpdatePaneMenuItems()
    {
        _paneMenuItems.Clear();

        var resourcesWithChildren = _resourceByName.Values
            .Where(r => !r.IsResourceHidden(_filter.ShowHiddenResources))
            .Where(r => _resourceByName.Values.Any(nested => string.Equals(nested.GetResourcePropertyValue(KnownProperties.Resource.ParentName), r.Name, StringComparisons.ResourceName)))
            .ToList();

        if (resourcesWithChildren.Count > 0)
        {
            var hasCollapsed = resourcesWithChildren.Any(r => _collapsedResourceNames.Contains(r.PersistentKey));
            _paneMenuItems.Add(new MenuButtonItem
            {
                Text = hasCollapsed ? ResourcesLoc[nameof(Resources.Resources.ResourceExpandAllChildren)] : ResourcesLoc[nameof(Resources.Resources.ResourceCollapseAllChildren)],
                Icon = hasCollapsed ? new Icons.Regular.Size16.Eye() : new Icons.Regular.Size16.EyeOff(),
                OnClick = async () =>
                {
                    foreach (var resource in resourcesWithChildren)
                    {
                        if (hasCollapsed)
                        {
                            _collapsedResourceNames.Remove(resource.PersistentKey);
                        }
                        else
                        {
                            _collapsedResourceNames.Add(resource.PersistentKey);
                        }
                    }

                    await PersistCollapsedResourceNamesAsync();
                    UpdatePaneMenuItems();
                    StateHasChanged();
                }
            });
        }
    }

    private void UpdateResourceMenuItems()
    {
        _highlightedCommands.Clear();
        _resourceMenuItems.Clear();

        if (SelectedResource is not { } resource)
        {
            return;
        }

        _highlightedCommands.AddRange(resource.Commands
            .Where(c => c.IsHighlighted && c.State != CommandViewModelState.Hidden)
            .Take(DashboardUIHelpers.MaxHighlightedCommands));

        ResourceMenuBuilder.AddMenuItems(
            _resourceMenuItems,
            resource,
            _resourceByName,
            onViewDetails: EventCallback.Empty,
            EventCallback.Factory.Create<CommandViewModel>(this, command => ExecuteResourceCommandAsync(resource, command)),
            (r, command) => DashboardCommandExecutor.IsExecuting(r.Name, command.Name),
            showViewDetails: false,
            showTerminalItem: true,
            showConsoleLogsItem: false,
            showUrls: false);
    }

    private Task ExecuteResourceCommandAsync(ResourceViewModel resource, CommandViewModel command)
        => DashboardCommandExecutor.ExecuteAsync(resource, command, GetResourceName);

    internal string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourceByName);

    private string GetResourceTypeText(ResourceSelectorItem item) =>
        item.ResourceType == TelemetryResourceType ? Loc[nameof(Resources.Layout.ResourcePaneTelemetryType)] : item.ResourceType;

    private string GetResourceStateText(ResourceSelectorItem item) =>
        item.Resource is { } resource ? ResourceStateViewModel.GetStateViewModel(resource, ColumnsLoc).Text : Loc[nameof(Resources.Layout.ResourcePaneNotApplicableState)];

    private Icon GetResourceIcon(ResourceSelectorItem item) =>
        item.Resource is { } resource ? ResourceIconHelpers.GetIconForResource(IconResolver, resource, IconSize.Size16) : new Icons.Regular.Size16.DataUsage();

    private static string GetStateClass(ResourceSelectorItem item) => item.Resource is { } resource ? GetStateClass(resource) : "state-neutral";

    private string GetResourceAccessibilityDescription(ResourceSelectorItem item, string stateText, int errorCount)
    {
        List<string> descriptions = [stateText, GetResourceTypeText(item)];
        if (item.Resource?.IsResourceHidden(showHiddenResources: false) == true)
        {
            descriptions.Add(ColumnsLoc[nameof(Columns.HiddenResourceIconTooltip)]);
        }
        if (item.Resource is { IsPersistentContainer: true })
        {
            descriptions.Add(ColumnsLoc[nameof(Columns.PersistentContainerIconTooltip)]);
        }
        if (item.TelemetryOnly is not null)
        {
            descriptions.Add(Loc[nameof(Resources.Layout.ResourcePaneTelemetryOnly)]);
        }
        if (errorCount > 0)
        {
            descriptions.Add(string.Format(CultureInfo.CurrentCulture, Loc[nameof(Resources.Layout.ResourcePaneUnviewedErrors)], errorCount));
        }

        return string.Join("; ", descriptions);
    }

    private OtlpResource? GetTelemetryResource(ResourceViewModel resource) => TelemetryRepository.GetResourceByCompositeName(resource.Name);

    private int GetUnviewedErrorCount(ResourceSelectorItem item) =>
        item.Resource is { } resource ? GetUnviewedErrorCount(resource)
            : item.TelemetryOnly is { } telemetryOnly ? _unviewedErrorCounts?.GetValueOrDefault(telemetryOnly.Resource.ResourceKey) ?? 0 : 0;

    internal int GetUnviewedErrorCount(ResourceViewModel resource)
    {
        return _unviewedErrorCounts is not null &&
            GetTelemetryResource(resource) is { } otlpResource &&
            _unviewedErrorCounts.TryGetValue(otlpResource.ResourceKey, out var count)
            ? count
            : 0;
    }

    private TelemetryOnlyResource? SelectedTelemetryOnlyResource =>
        _selectedItems is [{ TelemetryOnly: { } telemetryOnly }] ? telemetryOnly : null;

    /// <summary>
    /// Gets the telemetry keys for the selected resources, or all sources in the current pane when there is
    /// no selection.
    /// </summary>
    internal IReadOnlyList<ResourceKey> GetSelectionTelemetryKeys(IReadOnlyCollection<string>? resourceNames)
    {
        if (resourceNames is null || resourceNames.Count == 0)
        {
            // The router can refresh a child page before updating this layout's parameters.
            // Resolve the URL's selection instead of showing the previous resource's telemetry.
            var location = ParseLocation(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
            resourceNames = location.KeepSelection ? _selectedResourceNames : location.SelectedResourceNames;
        }

        if (resourceNames.Count == 0)
        {
            return GetPaneTelemetryKeys();
        }

        var keys = new List<ResourceKey>();
        foreach (var name in resourceNames)
        {
            OtlpResource? otlpResource = null;
            if (ResourceViewModel.TryGetResourceByName(name, _resourceByName, out var resource))
            {
                otlpResource = GetTelemetryResource(resource);
            }
            else if (_telemetryOnlyResources.FirstOrDefault(r => string.Equals(r.Name, name, StringComparisons.ResourceName)) is { } telemetryOnly)
            {
                otlpResource = telemetryOnly.Resource;
            }

            if (otlpResource is not null && !keys.Contains(otlpResource.ResourceKey))
            {
                keys.Add(otlpResource.ResourceKey);
            }
        }

        return keys;
    }

    internal IReadOnlyList<ResourceKey> GetPaneTelemetryKeys() => _resourceByName.Values
        .Select(GetTelemetryResource).OfType<OtlpResource>().Select(r => r.ResourceKey)
        .Concat(ShowTelemetryOnlyResources ? _telemetryOnlyResources.Select(r => r.Resource.ResourceKey) : [])
        .Distinct().ToList();

    private string GetPaneCountText(int count) => string.Format(CultureInfo.CurrentCulture,
        Loc[!DashboardClient.IsEnabled
            ? count == 1 ? nameof(Resources.Layout.TelemetryPaneCountSingular) : nameof(Resources.Layout.TelemetryPaneCountPlural)
            : count == 1 ? nameof(Resources.Layout.ResourcePaneCountSingular) : nameof(Resources.Layout.ResourcePaneCountPlural)], count);

    private string GetPageTitle() => Loc[CurrentTab switch
    {
        ResourceTab.Overview or ResourceTab.Graph => nameof(Resources.Layout.NavMenuResourcesTab),
        ResourceTab.Console => nameof(Resources.Layout.NavMenuConsoleLogsTab),
        ResourceTab.StructuredLogs => nameof(Resources.Layout.NavMenuStructuredLogsTab),
        ResourceTab.Traces => nameof(Resources.Layout.NavMenuTracesTab),
        ResourceTab.Metrics => nameof(Resources.Layout.NavMenuMetricsTab),
        _ => throw new InvalidOperationException($"Unknown resource tab: {CurrentTab}")
    }];

    /// <summary>
    /// Gets a value indicating whether an app model resource is one of the named resources. Names are either the
    /// resource's unique name or, for resources without replicas, its display name.
    /// </summary>
    internal static bool IsResourceInSelection(ResourceViewModel resource, IEnumerable<string> resourceNames) =>
        resourceNames.Contains(resource.Name, StringComparers.ResourceName) ||
        resourceNames.Contains(resource.DisplayName, StringComparers.ResourceName);

    /// <summary>
    /// Adds the current multi-resource selection to a page URL, so changing page state such as filters keeps the
    /// selection. A single selected resource is already part of the page's URL path.
    /// </summary>
    internal string AddSelectionToUrl(string url) => IsMultiSelection ? DashboardUrls.AddResourceSelection(url, _selectedResourceNames) : url;

    internal bool IsTabAvailable(ResourceTab tab) => IsTabAvailable(tab, _selectedItems);

    private bool IsTabAvailable(ResourceTab tab, IReadOnlyList<ResourceSelectorItem> selection)
    {
        if (tab == ResourceTab.Graph)
        {
            return DashboardClient.IsEnabled && DashboardOptions.CurrentValue.UI.DisableResourceGraph != true && selection.Count != 1;
        }

        if (!DashboardClient.IsEnabled && tab is ResourceTab.Overview or ResourceTab.Console)
        {
            return false;
        }

        if (selection.Count == 0)
        {
            // Standalone keeps Metrics reachable without a selection so the page can prompt for a service.
            // AppHost mode only offers Metrics once resources have been selected.
            return tab is not ResourceTab.Metrics || !DashboardClient.IsEnabled;
        }

        // With several selected resources, a tab is available when any of them supports it.
        return selection.Any(item => IsTabAvailable(tab, item));
    }

    private bool IsTabAvailable(ResourceTab tab, ResourceSelectorItem item)
    {
        if (item.Resource is { } resource)
        {
            var telemetryResource = GetTelemetryResource(resource);
            return tab switch
            {
                ResourceTab.Overview or ResourceTab.Console => true,
                ResourceTab.Traces => telemetryResource is not null,
                _ => telemetryResource is { UninstrumentedPeer: false }
            };
        }

        if (item.TelemetryOnly is not null)
        {
            return tab is ResourceTab.StructuredLogs or ResourceTab.Traces or ResourceTab.Metrics;
        }

        // The resource isn't known (yet). Keep the tab so the page can report it.
        return tab == CurrentTab;
    }

    internal static string GetTabUrl(ResourceTab tab, IReadOnlyList<string> resourceNames, string? activeResourceName = null)
    {
        if (tab == ResourceTab.Graph)
        {
            return DashboardUrls.AddResourceSelection(
                QueryHelpers.AddQueryString(DashboardUrls.ResourceOverviewUrl(), "view", "graph"), resourceNames);
        }

        if (resourceNames.Count > 1)
        {
            // The metrics page displays one resource's instruments at a time, and keeps the displayed resource in
            // the URL path.
            var url = tab switch
            {
                ResourceTab.Overview => DashboardUrls.ResourceOverviewUrl(),
                ResourceTab.Console => DashboardUrls.ConsoleLogsUrl(),
                ResourceTab.StructuredLogs => DashboardUrls.StructuredLogsUrl(),
                ResourceTab.Traces => DashboardUrls.TracesUrl(),
                ResourceTab.Metrics => DashboardUrls.MetricsUrl(activeResourceName is not null && resourceNames.Contains(activeResourceName, StringComparers.ResourceName) ? activeResourceName : null),
                _ => throw new InvalidOperationException($"Unexpected tab: {tab}")
            };

            return DashboardUrls.AddResourceSelection(url, resourceNames);
        }

        var resourceName = resourceNames.Count == 1 ? resourceNames[0] : null;
        return tab switch
        {
            ResourceTab.Overview => DashboardUrls.ResourceOverviewUrl(resourceName),
            ResourceTab.Console => DashboardUrls.ConsoleLogsUrl(resourceName),
            ResourceTab.StructuredLogs => DashboardUrls.StructuredLogsUrl(resourceName),
            ResourceTab.Traces => DashboardUrls.TracesUrl(resourceName),
            ResourceTab.Metrics => DashboardUrls.MetricsUrl(resourceName),
            _ => throw new InvalidOperationException($"Unexpected tab: {tab}")
        };
    }

    /// <summary>
    /// Gets the URL that selects the given resources, keeping the current telemetry page even when the resources
    /// haven't sent telemetry.
    /// </summary>
    private string GetSelectionUrl(IReadOnlyList<string> resourceNames)
    {
        if (CurrentTab is ResourceTab.Console or ResourceTab.StructuredLogs or ResourceTab.Traces or ResourceTab.Metrics)
        {
            return GetTabUrl(CurrentTab, resourceNames, RouteResourceName);
        }

        var selection = resourceNames.Select(name =>
        {
            if (ResourceViewModel.TryGetResourceByName(name, _resourceByName, out var resource))
            {
                return new ResourceSelectorItem(name, resource, TelemetryOnly: null);
            }

            return new ResourceSelectorItem(name, Resource: null, _telemetryOnlyResources.FirstOrDefault(r => string.Equals(r.Name, name, StringComparisons.ResourceName)));
        }).ToList();

        ResourceTab tab;
        if (IsTabAvailable(CurrentTab, selection))
        {
            tab = CurrentTab;
        }
        else if (DashboardClient.IsEnabled && (selection.Any(i => i.Resource is not null) || selection.Count == 0))
        {
            tab = ResourceTab.Overview;
        }
        else
        {
            // Resources that only send telemetry don't have an overview.
            tab = ResourceTab.StructuredLogs;
        }

        return GetTabUrl(tab, resourceNames, RouteResourceName);
    }

    /// <summary>
    /// Gets the URL a resource row links to, which selects only that resource.
    /// </summary>
    private string GetRowUrl(string resourceName) => GetSelectionUrl([resourceName]);

    /// <summary>
    /// Handles a click on a resource row. A click selects the resource, Ctrl+click (Cmd+click on macOS) adds or
    /// removes it from the selection, and Shift+click selects the range of rows between the last clicked row and
    /// this one.
    /// </summary>
    private async Task OnRowClickAsync(MouseEventArgs e, string resourceName)
    {
        // The selection can also change by navigating, for example with a link to a resource. Ranges then start at
        // the last selected resource.
        var anchor = _selectionAnchor is not null && IsSelected(_selectionAnchor) ? _selectionAnchor : _selectedResourceNames is [.., var last] ? last : null;

        IReadOnlyList<string> selection;
        if (e.ShiftKey && anchor is not null && GetVisibleRowNames() is var rowNames &&
            rowNames.FindIndex(n => string.Equals(n, anchor, StringComparisons.ResourceName)) is var anchorIndex and >= 0 &&
            rowNames.FindIndex(n => string.Equals(n, resourceName, StringComparisons.ResourceName)) is var clickedIndex and >= 0)
        {
            var start = Math.Min(anchorIndex, clickedIndex);
            selection = rowNames.GetRange(start, Math.Abs(anchorIndex - clickedIndex) + 1);
            _selectionAnchor = anchor;
        }
        else if (e.CtrlKey || e.MetaKey)
        {
            var toggled = _selectedResourceNames.ToList();
            if (toggled.RemoveAll(n => string.Equals(n, resourceName, StringComparisons.ResourceName)) == 0)
            {
                toggled.Add(resourceName);
            }

            selection = toggled;
            _selectionAnchor = resourceName;
        }
        else
        {
            selection = [resourceName];
            _selectionAnchor = resourceName;
        }

        if (selection.Count == 0)
        {
            // Remember the cleared selection so the overview doesn't select a resource again.
            await SetStoredSelectionAsync(selection);
        }

        NavigationManager.NavigateTo(GetSelectionUrl(selection));
    }

    // A single selected resource is shown by its row alone, so a one-resource group doesn't duplicate the highlight.
    private bool IsResourceGroupSelected(List<string> groupResourceNames) =>
        groupResourceNames.Count > 1 &&
        groupResourceNames.Count == _selectedResourceNames.Count &&
        groupResourceNames.All(IsSelected);

    private void OnResourceGroupClick(MouseEventArgs e, List<string> groupResourceNames)
    {
        if (groupResourceNames.Count == 0)
        {
            return;
        }

        IReadOnlyList<string> selection;
        if (e.CtrlKey || e.MetaKey)
        {
            // Like rows, a modifier click toggles the group's resources in the current selection.
            var toggled = _selectedResourceNames.ToList();
            if (groupResourceNames.All(IsSelected))
            {
                toggled.RemoveAll(n => groupResourceNames.Contains(n, StringComparers.ResourceName));
            }
            else
            {
                toggled.AddRange(groupResourceNames.Where(n => !IsSelected(n)));
            }

            selection = toggled;
        }
        else
        {
            selection = groupResourceNames;
        }

        _selectionAnchor = selection is [.., var last] ? last : null;
        NavigationManager.NavigateTo(GetSelectionUrl(selection));
    }

    private async Task OnAllResourcesClickAsync()
    {
        _selectionAnchor = null;

        // Remember the cleared selection so entering the overview later doesn't select a resource again.
        await SetStoredSelectionAsync([]);
        NavigationManager.NavigateTo(GetSelectionUrl([]));
    }

    /// <summary>
    /// Gets the names of the resources that "All resources" stands for, in the order of the resource list. The pane's
    /// filters don't apply because they only narrow down the list.
    /// </summary>
    internal List<string> GetAllResourceNames()
    {
        return GetResourceItems()
            .Where(item => item.Resource is not { } resource || (!resource.IsParameter && !resource.IsResourceHidden(_filter.ShowHiddenResources)))
            .OrderBy(item => item, s_resourceTypeAndNameComparer)
            .Select(item => item.Name)
            .ToList();
    }

    private List<string> GetVisibleRowNames()
    {
        List<string> names;
        if (DashboardClient.IsEnabled && _grouping != ResourceGrouping.None)
        {
            // A resource with several tags is listed several times. Ranges use its first position.
            var showAllGroups = ViewportInformation.IsDesktop && _isPaneCollapsed;
            names = GetResourceGroups()
                .Where(g => showAllGroups || !IsGroupCollapsed(g))
                .SelectMany(g => g.Resources)
                .Select(item => item.Name)
                .Distinct(StringComparers.ResourceName)
                .ToList();
        }
        else
        {
            names = GetPaneRows().Select(item => item.Name).ToList();
        }

        return names;
    }

    private bool IsSelected(string resourceName) => _selectedResourceNames.Contains(resourceName, StringComparers.ResourceName);

    private string? GetRowAriaCurrent(bool isSelected) => isSelected ? (IsMultiSelection ? "true" : "page") : null;

    private static string GetTabId(ResourceTab tab) => $"tab-{tab}";

    private void OnTabChanged(FluentTab? newTab)
    {
        // FluentTabs also raises this when a tab is disposed, for example when leaving the resources view, and
        // when it syncs with ActiveTabId after a navigation. Only a change made while this layout shows the current
        // URL is a user selecting a tab.
        // Switching between the live view and a recording re-creates the page, and the outgoing layout's tabs
        // raise changes while they're torn down, which would otherwise navigate to another tab.
        if (_isDisposed
            || !ShowResourceViewTabs
            || !string.Equals(NavigationManager.Uri, _lastLocation, StringComparison.Ordinal)
            || newTab?.Id is not { } id
            || !id.StartsWith("tab-", StringComparison.Ordinal)
            || !Enum.TryParse<ResourceTab>(id["tab-".Length..], out var tab)
            || tab == CurrentTab)
        {
            return;
        }

        // Tabs map to pages, so a tab change is a navigation. The tab URL keeps the current resource selection.
        if (GetTabs().FirstOrDefault(t => t.Tab == tab && t.IsVisible)?.Href is { } href)
        {
            NavigationManager.NavigateTo(href);
        }
    }

    private IEnumerable<TabItem> GetTabs()
    {
        yield return new TabItem(ResourceTab.Overview, ControlsStringsLoc[nameof(ControlsStrings.ChartContainerTableTab)],
            new Icons.Regular.Size24.Table(), IsVisible: true, GetTabUrl(ResourceTab.Overview));
        yield return new TabItem(ResourceTab.Graph, Loc[nameof(Resources.Layout.NavMenuGraphTab)],
            DesktopNavMenu.GraphIcon(), DashboardOptions.CurrentValue.UI.DisableResourceGraph != true,
            GetTabUrl(ResourceTab.Graph));
    }

    internal static string GetStateClass(ResourceViewModel resource) => resource.KnownState switch
    {
        KnownResourceState.Running when resource.HealthStatus is Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy => "state-warning",
        KnownResourceState.Running => "state-success",
        KnownResourceState.FailedToStart or KnownResourceState.RuntimeUnhealthy => "state-error",
        KnownResourceState.Exited when resource.TryGetExitCode(out var exitCode) && exitCode is not 0 => "state-error",
        KnownResourceState.Starting or KnownResourceState.Building or KnownResourceState.Waiting or KnownResourceState.Stopping => "state-pending",
        _ => "state-neutral"
    };

    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        _cts.Cancel();
        TagStore.Changed -= OnTagsChanged;
        OrderStore.Changed -= OnOrderChanged;
        _logsSubscription?.Dispose();
        _telemetryResourcesSubscription?.Dispose();
        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);
        _cts.Dispose();

        if (_jsModule is { } module)
        {
            try
            {
                await module.InvokeVoidAsync("unregisterPaneResizer", _layoutElement);
                await module.InvokeVoidAsync("unregisterResourceOrdering", _layoutElement);
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone, so the browser has already discarded the handlers.
            }

            await JSInteropHelpers.SafeDisposeAsync(module);
        }

        _selfRef?.Dispose();
    }

    internal enum ResourceTab
    {
        Overview,
        Console,
        StructuredLogs,
        Traces,
        Metrics,
        Graph
    }

    internal sealed record ResourceLocation(ResourceTab Tab, string? RouteResourceName, IReadOnlyList<string> SelectedResourceNames, bool KeepSelection);

    private sealed record TelemetryOnlyResource(string Name, OtlpResource Resource);

    // Selector metadata must not turn OTLP-only sources into AppHost resources with detail pages or commands.
    private sealed record ResourceSelectorItem(string Name, ResourceViewModel? Resource, TelemetryOnlyResource? TelemetryOnly)
    {
        public string ResourceType => Resource?.ResourceType ?? TelemetryResourceType;
        public string State => Resource is null ? TelemetryResourceState
            : string.IsNullOrEmpty(Resource.State) ? nameof(KnownResourceState.Unknown) : Resource.State;
        public ResourceGridViewModel? Row { get; init; }
    }

    private enum ResourceGrouping
    {
        None,
        Tags,
        ResourceType,
        State
    }

    private sealed record ResourceGroup(string? Value, List<ResourceSelectorItem> Resources);

    private sealed record TabItem(ResourceTab Tab, string Text, Icon Icon, bool IsVisible, string Href);
}
