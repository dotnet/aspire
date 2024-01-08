// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Aspire.Dashboard.Model;
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
    public required ProtectedSessionStorage SessionStorage { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string? ResourceName { get; set; }

    private readonly TaskCompletionSource _whenDomReady = new();
    private readonly CancellationTokenSource _resourceSubscriptionCancellation = new();
    private readonly CancellationSeries _logSubscriptionCancellationSeries = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private List<Option<string>>? _resources;

    // UI
    private FluentSelect<Option<string>>? _resourceSelectComponent;
    private Option<string> _noSelection = null!;
    private LogViewer _logViewer = null!;

    // State
    public ConsoleLogsViewModel ViewModel { get; set; } = null!;

    public string BasePath => "ConsoleLogs";
    public string SessionStorageKey => "ConsoleLogs_PageState";

    protected override void OnInitialized()
    {
        _noSelection = new() { Value = null, Text = ControlsStringsLoc[nameof(ControlsStrings.SelectAResource)] };
        ViewModel = new ConsoleLogsViewModel { SelectedResource = null, Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLoadingResources)] };

        TrackResourceSnapshots();

        void TrackResourceSnapshots()
        {
            var (snapshot, subscription) = DashboardClient.SubscribeResources();

            foreach (var resource in snapshot)
            {
                var added = _resourceByName.TryAdd(resource.Name, resource);
                Debug.Assert(added, "Should not receive duplicate resources in initial snapshot data.");
            }

            UpdateResourcesList();

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
                    if (ResourceName is not null && ViewModel.SelectedResource is null && changeType == ResourceViewModelChangeType.Upsert && string.Equals(ResourceName, resource.Name))
                    {
                        ViewModel.SelectedResource = resource;
                        Debug.Assert(_resources is not null);
                        ViewModel.SelectedOption = _resources.Single(option => string.Equals(ResourceName, option.Value, StringComparison.Ordinal));
                        await SetSelectedConsoleResource(resource);
                    }
                }
            });
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await this.InitializeViewModelAsync();

        if (ViewModel.SelectedResource is not null)
        {
            await LoadLogsAsync();
        }
        else
        {
            await StopWatchingLogsAsync();
            await ClearLogsAsync();
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
        _resources ??= new(_resourceByName.Count + 1);
        _resources.Clear();
        _resources.Add(_noSelection);
        _resources.AddRange(_resourceByName.Values
            .OrderBy(c => c.Name)
            .Select(ToOption));

        Option<string> ToOption(ResourceViewModel resource)
        {
            return new Option<string>
            {
                Value = resource.Name,
                Text = GetDisplayText()
            };

            string GetDisplayText()
            {
                var resourceName = ResourceViewModel.GetResourceName(resource, _resourceByName.Values);

                return resource.State switch
                {
                    null or { Length: 0 } => $"{resourceName} ({Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsUnknownState)]})",
                    "Running" => resourceName,
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

        if (ViewModel.SelectedResource is null)
        {
            ViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoResourceSelected)];
        }
        else if (_logViewer is null)
        {
            ViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsInitializingLogViewer)];
        }
        else
        {
            var cancellationToken = await _logSubscriptionCancellationSeries.NextAsync();

            var subscription = DashboardClient.SubscribeConsoleLogs(ViewModel.SelectedResource.Name, cancellationToken);

            if (subscription is not null)
            {
                var task = _logViewer.SetLogSourceAsync(
                    subscription,
                    convertTimestampsFromUtc: ViewModel.SelectedResource.IsContainer());

                ViewModel.InitialisedSuccessfully = true;
                ViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsWatchingLogs)];

                // Indicate when logs finish (other than by cancellation).
                _ = task.ContinueWith(
                    _ => ViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)],
                    CancellationToken.None,
                    TaskContinuationOptions.NotOnCanceled,
                    TaskScheduler.Current);
            }
            else
            {
                ViewModel.InitialisedSuccessfully = false;
                ViewModel.Status = Loc[ViewModel.SelectedResource.IsContainer()
                    ? nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFailedToInitialize)
                    : nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLogsNotYetAvailable)];
            }
        }
    }

    private async Task HandleSelectedOptionChangedAsync()
    {
        await StopWatchingLogsAsync();
        await ClearLogsAsync();

        ViewModel.SelectedResource = ViewModel.SelectedOption?.Value is null ? null : _resourceByName[ViewModel.SelectedOption.Value];
        await this.AfterViewModelChangedAsync();
    }

    private async Task OnResourceChanged(ResourceViewModelChangeType changeType, ResourceViewModel resource)
    {
        if (changeType == ResourceViewModelChangeType.Upsert)
        {
            _resourceByName[resource.Name] = resource;
            UpdateResourcesList();

            if (string.Equals(ViewModel.SelectedResource?.Name, resource.Name, StringComparison.Ordinal))
            {
                await SetSelectedConsoleResource(resource);
            }
        }
        else if (changeType == ResourceViewModelChangeType.Delete)
        {
            var removed = _resourceByName.TryRemove(resource.Name, out _);
            Debug.Assert(removed, "Cannot remove unknown resource.");

            if (string.Equals(ViewModel.SelectedResource?.Name, resource.Name, StringComparison.Ordinal))
            {
                // The selected resource was deleted
                ViewModel.SelectedOption = _noSelection;
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
        ViewModel.SelectedResource = resource;

        if (ViewModel.InitialisedSuccessfully is not true)
        {
            await LoadLogsAsync();
        }
        else if (!string.Equals(ViewModel.SelectedResource.State, "Running", StringComparison.Ordinal))
        {
            ViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)];
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
        public Option<string>? SelectedOption { get; set; }
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
            var selectedOption = _resources.FirstOrDefault(c => string.Equals(ResourceName, c.Value, StringComparisons.ResourceName)) ?? _noSelection;

            viewModel.SelectedOption = selectedOption;
            viewModel.SelectedResource = selectedOption?.Value is null ? null : _resourceByName[selectedOption.Value];
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
            return new UrlState($"{BasePath}/{selectedOption}", null);
        }

        return new UrlState($"/{BasePath}", null);
    }

    public ConsoleLogsPageState ConvertViewModelToSerializable()
    {
        return new ConsoleLogsPageState
        {
            SelectedResource = ViewModel.SelectedResource?.Name
        };
    }
}
