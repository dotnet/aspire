// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public sealed partial class ConsoleLogs : ComponentBase, IAsyncDisposable, IPageWithSessionAndUrlState<ConsoleLogs.ConsoleLogsViewModel, ConsoleLogs.ConsoleLogsPageState>
{
    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required ProtectedSessionStorage SessionStorage { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ILogger<ConsoleLogs> Logger { get; init; }

    [Parameter]
    public string? ResourceName { get; set; }

    private readonly TaskCompletionSource _whenDomReady = new();
    private readonly CancellationTokenSource _resourceSubscriptionCancellation = new();
    private readonly CancellationSeries _logSubscriptionCancellationSeries = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private ImmutableList<SelectViewModel<ResourceTypeDetails>>? _resources;

    // UI
    private FluentSelect<SelectViewModel<ResourceTypeDetails>>? _resourceSelectComponent;
    private SelectViewModel<ResourceTypeDetails> _noSelection = null!;
    private LogViewer _logViewer = null!;

    // State
    public ConsoleLogsViewModel PageViewModel { get; set; } = null!;

    public string BasePath => "consolelogs";
    public string SessionStorageKey => "ConsoleLogs_PageState";

    protected override async Task OnInitializedAsync()
    {
        _noSelection = new() { Id = null, Name = ControlsStringsLoc[nameof(ControlsStrings.SelectAResource)] };
        PageViewModel = new ConsoleLogsViewModel { SelectedOption = _noSelection, SelectedResource = null, Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLoadingResources)] };

        var loadingTcs = new TaskCompletionSource();

        await TrackResourceSnapshotsAsync();

        // Wait for resource to be selected. If selected resource isn't available after a few seconds then stop waiting.
        try
        {
            await loadingTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Logger.LogDebug("Loading task completed.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Load timeout while waiting for resource {ResourceName}.", ResourceName);
        }

        async Task TrackResourceSnapshotsAsync()
        {
            if (!DashboardClient.IsEnabled)
            {
                return;
            }

            var (snapshot, subscription) = DashboardClient.SubscribeResources();

            Logger.LogDebug("Received initial resource snapshot with {ResourceCount} resources.", snapshot.Length);

            foreach (var resource in snapshot)
            {
                var added = _resourceByName.TryAdd(resource.Name, resource);
                Debug.Assert(added, "Should not receive duplicate resources in initial snapshot data.");
            }

            UpdateResourcesList();

            // Set loading task result if the selected resource is already in the snapshot or there is no selected resource.
            if (ResourceName != null)
            {
                if (_resourceByName.TryGetValue(ResourceName, out var selectedResource))
                {
                    await SetSelectedResourceOptionAsync(selectedResource);
                }
            }
            else
            {
                Logger.LogDebug("No resource selected.");
                loadingTcs.TrySetResult();
            }

            _ = Task.Run(async () =>
            {
                await foreach (var (changeType, resource) in subscription.WithCancellation(_resourceSubscriptionCancellation.Token))
                {
                    await OnResourceChanged(changeType, resource);

                    // the initial snapshot we obtain is [almost] never correct (it's always empty)
                    // we still want to select the user's initial queried resource on page load,
                    // so if there is no selected resource when we
                    // receive an added resource, and that added resource name == ResourceName,
                    // we should mark it as selected
                    if (ResourceName is not null && PageViewModel.SelectedResource is null && changeType == ResourceViewModelChangeType.Upsert && string.Equals(ResourceName, resource.Name))
                    {
                        await SetSelectedResourceOptionAsync(resource);
                    }
                }
            });
        }

        async Task SetSelectedResourceOptionAsync(ResourceViewModel resource)
        {
            PageViewModel.SelectedResource = resource;
            Debug.Assert(_resources is not null);
            PageViewModel.SelectedOption = _resources.Single(option => option.Id?.Type is not OtlpApplicationType.ReplicaSet && string.Equals(ResourceName, option.Id?.InstanceId, StringComparison.Ordinal));
            await SetSelectedConsoleResource(resource);

            Logger.LogDebug("Selected console resource from name {ResourceName}.", ResourceName);
            loadingTcs.TrySetResult();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogDebug("Initializing console logs view model.");
        await this.InitializeViewModelAsync();

        await ClearLogsAsync();

        if (PageViewModel.SelectedResource is not null)
        {
            await LoadLogsAsync();
        }
        else
        {
            await StopWatchingLogsAsync();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // Let anyone waiting know that the render is complete, so we have access to the underlying log viewer.
            _whenDomReady.SetResult();
        }
    }

    private void UpdateResourcesList()
    {
        var builder = ImmutableList.CreateBuilder<SelectViewModel<ResourceTypeDetails>>();
        builder.Add(_noSelection);

        foreach (var resourceGroupsByApplicationName in _resourceByName.Values.OrderBy(c => c.Name).GroupBy(resource => resource.DisplayName))
        {
            if (resourceGroupsByApplicationName.Count() > 1)
            {
                builder.Add(new SelectViewModel<ResourceTypeDetails>
                {
                    Id = ResourceTypeDetails.CreateReplicaSet(resourceGroupsByApplicationName.Key),
                    Name = resourceGroupsByApplicationName.Key
                });
            }

            foreach (var resource in resourceGroupsByApplicationName)
            {
                builder.Add(ToOption(resource, resourceGroupsByApplicationName.Count() > 1, resourceGroupsByApplicationName.Key));
            }
        }

        _resources = builder.ToImmutable();

        SelectViewModel<ResourceTypeDetails> ToOption(ResourceViewModel resource, bool isReplica, string applicationName)
        {
            var id = isReplica
                ? ResourceTypeDetails.CreateReplicaInstance(resource.Name, applicationName)
                : ResourceTypeDetails.CreateSingleton(resource.Name);

            return new SelectViewModel<ResourceTypeDetails>
            {
                Id = id,
                Name = GetDisplayText()
            };

            string GetDisplayText()
            {
                var resourceName = ResourceViewModel.GetResourceName(resource, _resourceByName.Values);

                return resource.State switch
                {
                    null or { Length: 0 } => $"{resourceName} ({Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsUnknownState)]})",
                    ResourceStates.RunningState => resourceName,
                    _ => $"{resourceName} ({resource.State})"
                };
            }
        }
    }

    private Task ClearLogsAsync()
    {
        return _logViewer is not null ? _logViewer.ClearLogsAsync() : Task.CompletedTask;
    }

    private async ValueTask LoadLogsAsync()
    {
        // Wait for the first render to complete so that the log viewer is available
        await _whenDomReady.Task;

        if (PageViewModel.SelectedResource is null)
        {
            PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoResourceSelected)];
        }
        else if (_logViewer is null)
        {
            PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsInitializingLogViewer)];
        }
        else
        {
            var cancellationToken = await _logSubscriptionCancellationSeries.NextAsync();

            var subscription = DashboardClient.SubscribeConsoleLogs(PageViewModel.SelectedResource.Name, cancellationToken);

            if (subscription is not null)
            {
                var task = _logViewer.SetLogSourceAsync(
                    subscription,
                    convertTimestampsFromUtc: PageViewModel.SelectedResource.IsContainer());

                PageViewModel.InitialisedSuccessfully = true;
                PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsWatchingLogs)];

                // Indicate when logs finish (other than by cancellation).
                _ = task.ContinueWith(
                    _ => PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)],
                    CancellationToken.None,
                    TaskContinuationOptions.NotOnCanceled,
                    TaskScheduler.Current);
            }
            else
            {
                PageViewModel.InitialisedSuccessfully = false;
                PageViewModel.Status = Loc[PageViewModel.SelectedResource.IsContainer()
                    ? nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFailedToInitialize)
                    : nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLogsNotYetAvailable)];
            }
        }
    }

    private async Task HandleSelectedOptionChangedAsync()
    {
        await StopWatchingLogsAsync();
        await ClearLogsAsync();

        PageViewModel.SelectedResource = PageViewModel.SelectedOption?.Id?.InstanceId is null ? null : _resourceByName[PageViewModel.SelectedOption.Id.InstanceId];
        await this.AfterViewModelChangedAsync();
    }

    private async Task OnResourceChanged(ResourceViewModelChangeType changeType, ResourceViewModel resource)
    {
        if (changeType == ResourceViewModelChangeType.Upsert)
        {
            _resourceByName[resource.Name] = resource;
            UpdateResourcesList();

            if (string.Equals(PageViewModel.SelectedResource?.Name, resource.Name, StringComparison.Ordinal))
            {
                await SetSelectedConsoleResource(resource);
            }
        }
        else if (changeType == ResourceViewModelChangeType.Delete)
        {
            var removed = _resourceByName.TryRemove(resource.Name, out _);
            Debug.Assert(removed, "Cannot remove unknown resource.");

            if (string.Equals(PageViewModel.SelectedResource?.Name, resource.Name, StringComparison.Ordinal))
            {
                // The selected resource was deleted
                PageViewModel.SelectedOption = _noSelection;
                await HandleSelectedOptionChangedAsync();
            }

            UpdateResourcesList();
        }

        await InvokeAsync(StateHasChanged);

        // Workaround for issue in fluent-select web component where the display value of the
        // selected item doesn't update automatically when the item changes
        if (_resourceSelectComponent is not null && JS is not null)
        {
            await JS.InvokeVoidAsync("updateFluentSelectDisplayValue", _resourceSelectComponent.Element);
        }
    }

    private async Task SetSelectedConsoleResource(ResourceViewModel resource)
    {
        // The selected resource was updated
        PageViewModel.SelectedResource = resource;

        if (PageViewModel.InitialisedSuccessfully is not true)
        {
            await LoadLogsAsync();
        }
        else if (PageViewModel.SelectedResource.State != ResourceStates.RunningState)
        {
            PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)];
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _resourceSubscriptionCancellation.CancelAsync();
        _resourceSubscriptionCancellation.Dispose();

        await StopWatchingLogsAsync();

        if (_logViewer is { } logViewer)
        {
            await logViewer.DisposeAsync();
        }
    }

    private Task StopWatchingLogsAsync() => _logSubscriptionCancellationSeries.ClearAsync();

    public class ConsoleLogsViewModel
    {
        public required string Status { get; set; }
        public required SelectViewModel<ResourceTypeDetails> SelectedOption { get; set; }
        public required ResourceViewModel? SelectedResource { get; set; }
        public bool? InitialisedSuccessfully { get; set; }
    }

    public class ConsoleLogsPageState
    {
        public string? SelectedResource { get; set; }
    }

    public void UpdateViewModelFromQuery(ConsoleLogsViewModel viewModel)
    {
        if (_resources is not null && ResourceName is not null)
        {
            var selectedOption = _resources.FirstOrDefault(c => string.Equals(ResourceName, c.Id?.InstanceId, StringComparisons.ResourceName)) ?? _noSelection;

            viewModel.SelectedOption = selectedOption;
            viewModel.SelectedResource = selectedOption.Id?.InstanceId is null ? null : _resourceByName[selectedOption.Id.InstanceId];
            viewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLogsNotYetAvailable)];
        }
        else
        {
            viewModel.SelectedOption = _noSelection;
            viewModel.SelectedResource = null;
            viewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoResourceSelected)];
        }
    }

    public UrlState GetUrlFromSerializableViewModel(ConsoleLogsPageState serializable)
    {
        if (serializable.SelectedResource is { } selectedOption)
        {
            return new UrlState($"{BasePath}/resource/{selectedOption}", null);
        }

        return new UrlState($"/{BasePath}", null);
    }

    public ConsoleLogsPageState ConvertViewModelToSerializable()
    {
        return new ConsoleLogsPageState
        {
            SelectedResource = PageViewModel.SelectedResource?.Name
        };
    }
}
