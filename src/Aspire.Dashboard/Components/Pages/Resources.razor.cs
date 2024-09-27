// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Dashboard.Extensions;

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
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
    public required IDialogService DialogService { get; init; }
    [Inject]
    public required IToastService ToastService { get; init; }
    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }
    [Inject]
    public required IJSRuntime JS { get; init; }
    [Inject]
    public required ProtectedSessionStorage SessionStorage { get; init; }
    [Inject]
    public required DimensionManager DimensionManager { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? VisibleTypes { get; set; }

    private ResourceViewModel? SelectedResource { get; set; }

    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly ConcurrentDictionary<string, bool> _allResourceTypes = [];
    private readonly ConcurrentDictionary<string, bool> _visibleResourceTypes = new(StringComparers.ResourceName);
    private string _filter = "";
    private bool _isTypeFilterVisible;
    private Task? _resourceSubscriptionTask;
    private bool _isLoading = true;
    private string? _elementIdBeforeDetailsViewOpened;
    private GridColumnManager _manager = null!;

    private bool Filter(ResourceViewModel resource) => _visibleResourceTypes.ContainsKey(resource.ResourceType) && (_filter.Length == 0 || resource.MatchesFilter(_filter)) && !resource.IsHiddenState();

    private Task OnResourceTypeVisibilityChangedAsync(string resourceType, bool isVisible)
    {
        if (isVisible)
        {
            _visibleResourceTypes[resourceType] = true;
        }
        else
        {
            _visibleResourceTypes.TryRemove(resourceType, out _);
        }

        return ClearSelectedResourceAsync();
    }

    private Task HandleSearchFilterChangedAsync()
    {
        return ClearSelectedResourceAsync();
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

    private IQueryable<ResourceViewModel>? FilteredResources => _resourceByName.Values.Where(Filter).OrderBy(e => e.ResourceType).ThenBy(e => e.Name).AsQueryable();

    private readonly GridSort<ResourceViewModel> _nameSort = GridSort<ResourceViewModel>.ByAscending(p => p.Name);
    private readonly GridSort<ResourceViewModel> _stateSort = GridSort<ResourceViewModel>.ByAscending(p => p.State);
    private readonly GridSort<ResourceViewModel> _startTimeSort = GridSort<ResourceViewModel>.ByDescending(p => p.CreationTimeStamp);

    protected override async Task OnInitializedAsync()
    {
        _gridColumns = [
            new GridColumn(Name: TypeColumn, DesktopWidth: "1fr"),
            new GridColumn(Name: NameColumn, DesktopWidth: "1.5fr", MobileWidth: "1.5fr"),
            new GridColumn(Name: StateColumn, DesktopWidth: "1.25fr", MobileWidth: "1.25fr"),
            new GridColumn(Name: StartTimeColumn, DesktopWidth: "1.5fr"),
            new GridColumn(Name: SourceColumn, DesktopWidth: "2.5fr"),
            new GridColumn(Name: EndpointsColumn, DesktopWidth: "2.5fr", MobileWidth: "2fr"),
            new GridColumn(Name: ActionsColumn, DesktopWidth: "1.5fr", MobileWidth: "1fr")
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
                await InvokeAsync(StateHasChanged);
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

            // Listen for updates and apply.
            _resourceSubscriptionTask = Task.Run(async () =>
            {
                await foreach (var changes in subscription.WithCancellation(_watchTaskCancellationTokenSource.Token).ConfigureAwait(false))
                {
                    foreach (var (changeType, resource) in changes)
                    {
                        if (changeType == ResourceViewModelChangeType.Upsert)
                        {
                            _resourceByName[resource.Name] = resource;
                            if (string.Equals(SelectedResource?.Name, resource.Name, StringComparisons.ResourceName))
                            {
                                SelectedResource = resource;
                            }

                            _allResourceTypes[resource.ResourceType] = true;
                            _visibleResourceTypes[resource.ResourceType] = true;
                        }
                        else if (changeType == ResourceViewModelChangeType.Delete)
                        {
                            var removed = _resourceByName.TryRemove(resource.Name, out _);
                            Debug.Assert(removed, "Cannot remove unknown resource.");
                        }
                    }

                    await InvokeAsync(StateHasChanged);
                }
            });
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

            if (item.DisplayName == resource.DisplayName)
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
        if (!string.IsNullOrWhiteSpace(command.ConfirmationMessage))
        {
            var dialogReference = await DialogService.ShowConfirmationAsync(command.ConfirmationMessage);
            var result = await dialogReference.Result;
            if (result.Cancelled)
            {
                return;
            }
        }

        var response = await DashboardClient.ExecuteResourceCommandAsync(resource.Name, resource.ResourceType, command, CancellationToken.None);

        var messageResourceName = GetResourceName(resource);

        if (response.Kind == ResourceCommandResponseKind.Succeeded)
        {
            ToastService.ShowSuccess(string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Resources.ResourceCommandSuccess)], command.DisplayName + " " + messageResourceName));
        }
        else
        {
            ToastService.ShowCommunicationToast(new ToastParameters<CommunicationToastContent>()
            {
                Intent = ToastIntent.Error,
                Title = string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Resources.ResourceCommandFailed)], command.DisplayName + " " + messageResourceName),
                PrimaryAction = Loc[nameof(Dashboard.Resources.Resources.ResourceCommandToastViewLogs)],
                OnPrimaryAction = EventCallback.Factory.Create<ToastResult>(this, () => NavigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl(resource: resource.Name))),
                Content = new CommunicationToastContent()
                {
                    Details = response.ErrorMessage
                }
            });
        }
    }

    private static string GetResourceStateTooltip(ResourceViewModel resource) =>
        resource.ShowReadinessState() ?
        $"{resource.State.Humanize()} ({resource.ReadinessState.Humanize()})"
        : resource.State.Humanize();

    private static (string Value, string? ContentAfterValue, string ValueToCopy, string Tooltip)? GetSourceColumnValueAndTooltip(ResourceViewModel resource)
    {
        // NOTE projects are also executables, so we have to check for projects first
        if (resource.IsProject() && resource.TryGetProjectPath(out var projectPath))
        {
            return (Value: Path.GetFileName(projectPath), ContentAfterValue: null, ValueToCopy: projectPath, Tooltip: projectPath);
        }

        if (resource.TryGetExecutablePath(out var executablePath))
        {
            resource.TryGetExecutableArguments(out var arguments);
            var argumentsString = arguments.IsDefaultOrEmpty ? "" : string.Join(" ", arguments);
            var fullCommandLine = $"{executablePath} {argumentsString}";

            return (Value: Path.GetFileName(executablePath), ContentAfterValue: argumentsString, ValueToCopy: fullCommandLine, Tooltip: fullCommandLine);
        }

        if (resource.TryGetContainerImage(out var containerImage))
        {
            return (Value: containerImage, ContentAfterValue: null, ValueToCopy: containerImage, Tooltip: containerImage);
        }

        if (resource.Properties.TryGetValue(KnownProperties.Resource.Source, out var property) && property.Value is { HasStringValue: true, StringValue: var value })
        {
            return (Value: value, ContentAfterValue: null, ValueToCopy: value, Tooltip: value);
        }

        return null;
    }

    private string GetEndpointsTooltip(ResourceViewModel resource)
    {
        var displayedEndpoints = GetDisplayedEndpoints(resource, out var additionalMessage);

        if (additionalMessage is not null)
        {
            return additionalMessage;
        }

        if (displayedEndpoints.Count == 1)
        {
            return displayedEndpoints.First().Text;
        }

        var maxShownEndpoints = 3;
        var tooltipBuilder = new StringBuilder(string.Join(", ", displayedEndpoints.Take(maxShownEndpoints).Select(endpoint => endpoint.Text)));

        if (displayedEndpoints.Count > maxShownEndpoints)
        {
            tooltipBuilder.Append(CultureInfo.CurrentCulture, $" + {displayedEndpoints.Count - maxShownEndpoints}");
        }

        return tooltipBuilder.ToString();
    }

    private List<DisplayedEndpoint> GetDisplayedEndpoints(ResourceViewModel resource, out string? additionalMessage)
    {
        if (resource.Urls.Length == 0)
        {
            // If we have no endpoints, and the app isn't running anymore or we're not expecting any, then just say None
            additionalMessage = ColumnsLoc[nameof(Columns.EndpointsColumnDisplayNone)];
            return [];
        }

        additionalMessage = null;

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
