// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Resources : ComponentBase, IAsyncDisposable, IPageWithSessionAndUrlState<Resources.ResourcePageViewModel, Resources.ResourcePageState>
{
    private Subscription? _logsSubscription;
    private Dictionary<OtlpApplication, int>? _applicationUnviewedErrorCounts;

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

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Filter { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? VisibleTypes { get; set; }

    private ResourceViewModel? SelectedResource { get; set; }

    public string BasePath => "/traces";
    public string SessionStorageKey => "Traces_PageState";

    public ResourcePageViewModel PageViewModel { get; set; } = null!;

    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly ConcurrentDictionary<string, bool> _allResourceTypes = [];
    private bool _isTypeFilterVisible;
    private Task? _resourceSubscriptionTask;
    private bool _isLoading = true;
    private string? _elementIdBeforeDetailsViewOpened;
    private AspirePageContentLayout? _contentLayout;

    private bool ShouldBeVisible(ResourceViewModel resource) => PageViewModel.VisibleResourceTypes.ContainsKey(resource.ResourceType) && (Filter is null || Filter.Length == 0 || resource.MatchesFilter(Filter)) && resource.State != ResourceStates.HiddenState;

    private async Task OnResourceTypeVisibilityChangedAsync(string resourceType, bool isVisible)
    {
        if (isVisible)
        {
            PageViewModel.VisibleResourceTypes[resourceType] = true;
        }
        else
        {
            PageViewModel.VisibleResourceTypes.TryRemove(resourceType, out _);
        }

        await this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: true);
        await ClearSelectedResourceAsync();
    }

    private async Task HandleSearchFilterChangedAsync()
    {
        PageViewModel.Filter = Filter ?? string.Empty;
        await this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: true);
        await ClearSelectedResourceAsync();
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

                return keysLeft.Count == keysRight.Count && keysLeft.SequenceEqual(keysRight, StringComparers.ResourceType);
            }

            return SetEqualsKeys(PageViewModel.VisibleResourceTypes, _allResourceTypes)
                ? true
                : PageViewModel.VisibleResourceTypes.IsEmpty
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
                UnionWithKeys(PageViewModel.VisibleResourceTypes, _allResourceTypes);
            }
            else if (value is false)
            {
                PageViewModel.VisibleResourceTypes.Clear();
            }

            StateHasChanged();
        }
    }

    private bool HasResourcesWithCommands => _resourceByName.Any(r => r.Value.Commands.Any());

    private IQueryable<ResourceViewModel>? FilteredResources => _resourceByName.Values.Where(ShouldBeVisible).OrderBy(e => e.ResourceType).ThenBy(e => e.Name).AsQueryable();

    private readonly GridSort<ResourceViewModel> _nameSort = GridSort<ResourceViewModel>.ByAscending(p => p.Name);
    private readonly GridSort<ResourceViewModel> _stateSort = GridSort<ResourceViewModel>.ByAscending(p => p.State);
    private readonly GridSort<ResourceViewModel> _startTimeSort = GridSort<ResourceViewModel>.ByDescending(p => p.CreationTimeStamp);

    protected override async Task OnInitializedAsync()
    {
        _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();

        PageViewModel = new ResourcePageViewModel
        {
            Filter = Filter ?? string.Empty
        };

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
                    PageViewModel.VisibleResourceTypes.TryAdd(resource.ResourceType, true);
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

                            _allResourceTypes[resource.ResourceType] = true;
                            PageViewModel.VisibleResourceTypes[resource.ResourceType] = true;
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

    protected override Task OnParametersSetAsync()
    {
        return this.InitializeViewModelAsync();
    }

    private bool ApplicationErrorCountsChanged(Dictionary<OtlpApplication, int> newApplicationUnviewedErrorCounts)
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

    private async Task ShowResourceDetailsAsync(ResourceViewModel resource, string buttonId)
    {
        _elementIdBeforeDetailsViewOpened = buttonId;

        if (SelectedResource == resource)
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
            if (item.State == ResourceStates.HiddenState)
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

    private string? GetRowClass(ResourceViewModel resource)
        => resource == SelectedResource ? "selected-row resource-row" : "resource-row";

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

        if (response.Kind == ResourceCommandResponseKind.Succeeded)
        {
            ToastService.ShowSuccess(string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Resources.ResourceCommandSuccess)], command.DisplayName));
        }
        else
        {
            ToastService.ShowCommunicationToast(new ToastParameters<CommunicationToastContent>()
            {
                Intent = ToastIntent.Error,
                Title = string.Format(CultureInfo.InvariantCulture, Loc[nameof(Dashboard.Resources.Resources.ResourceCommandFailed)], command.DisplayName),
                PrimaryAction = Loc[nameof(Dashboard.Resources.Resources.ResourceCommandToastViewLogs)],
                OnPrimaryAction = EventCallback.Factory.Create<ToastResult>(this, () => NavigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl(resource: resource.Name))),
                Content = new CommunicationToastContent()
                {
                    Details = response.ErrorMessage
                }
            });
        }
    }

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

        if (resource.Properties.TryGetValue(KnownProperties.Resource.Source, out var value) && value.HasStringValue)
        {
            return (Value: value.StringValue, ContentAfterValue: null, ValueToCopy: value.StringValue, Tooltip: value.StringValue);
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

        // Make sure that endpoints have a consistent ordering. Show https first, then everything else.
        return [.. GetEndpoints(resource)
            .OrderByDescending(e => e.Url?.StartsWith("https") == true)
            .ThenBy(e=> e.Url ?? e.Text)];
    }

    /// <summary>
    /// A resource has services and endpoints. These can overlap. This method attempts to return a single list without duplicates.
    /// </summary>
    private static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource)
    {
        return ResourceEndpointHelpers.GetEndpoints(resource, includeInteralUrls: false);
    }

    public void UpdateViewModelFromQuery(ResourcePageViewModel viewModel)
    {
        viewModel.Filter = Filter ?? string.Empty;
        if (VisibleTypes is not null)
        {
            foreach (var type in VisibleTypes.Split(','))
            {
                viewModel.VisibleResourceTypes.TryAdd(type, true);
            }
        }
    }

    public string GetUrlFromSerializableViewModel(ResourcePageState serializable)
    {
        return DashboardUrls.ResourcesUrl(filter: serializable.Filter, visibleTypes: serializable.VisibleTypes);
    }

    public ResourcePageState ConvertViewModelToSerializable()
    {
        return new ResourcePageState
        {
            Filter = PageViewModel.Filter,
            VisibleTypes = AreAllTypesVisible is true ? null : PageViewModel.VisibleResourceTypes?
                .Where(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToList()
        };
    }

    public async ValueTask DisposeAsync()
    {
        _watchTaskCancellationTokenSource.Cancel();
        _watchTaskCancellationTokenSource.Dispose();
        _logsSubscription?.Dispose();

        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);
    }

    public class ResourcePageViewModel : PageViewModelWithFilter
    {
        public ConcurrentDictionary<string, bool> VisibleResourceTypes { get; } = new(StringComparers.ResourceType);
    }

    public class ResourcePageState : PageStateWithFilter
    {
        public required List<string>? VisibleTypes { get; set; }
    }
}
