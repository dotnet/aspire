// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Aspire.Hosting.ConsoleLogs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Components.Pages;

public sealed partial class ConsoleLogs : ComponentBase, IAsyncDisposable, IPageWithSessionAndUrlState<ConsoleLogs.ConsoleLogsViewModel, ConsoleLogs.ConsoleLogsPageState>
{
    private sealed class ConsoleLogsSubscription
    {
        private readonly CancellationTokenSource _cts = new();

        public required string Name { get; init; }
        public Task? SubscriptionTask { get; set; }

        public CancellationToken CancellationToken => _cts.Token;
        public void Cancel() => _cts.Cancel();
    }

    [Inject]
    public required IOptions<DashboardOptions> Options { get; init; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required ISessionStorage SessionStorage { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ILogger<ConsoleLogs> Logger { get; init; }

    [Inject]
    public required DimensionManager DimensionManager { get; init; }

    [Inject]
    public required IStringLocalizer<Dashboard.Resources.ConsoleLogs> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    [Parameter]
    public string? ResourceName { get; set; }

    private readonly CancellationTokenSource _resourceSubscriptionCancellation = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private ImmutableList<SelectViewModel<ResourceTypeDetails>>? _resources;
    private Task? _resourceSubscriptionTask;
    private ConsoleLogsSubscription? _consoleLogsSubscription;
    internal LogEntries _logEntries = null!;

    // UI
    private SelectViewModel<ResourceTypeDetails> _noSelection = null!;
    private AspirePageContentLayout? _contentLayout;

    // State
    public ConsoleLogsViewModel PageViewModel { get; set; } = null!;

    public string BasePath => DashboardUrls.ConsoleLogBasePath;
    public string SessionStorageKey => BrowserStorageKeys.ConsoleLogsPageState;

    protected override async Task OnInitializedAsync()
    {
        _logEntries = new(Options.Value.Frontend.MaxConsoleLogCount);
        _noSelection = new() { Id = null, Name = ControlsStringsLoc[nameof(ControlsStrings.None)] };
        PageViewModel = new ConsoleLogsViewModel { SelectedOption = _noSelection, SelectedResource = null, Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLoadingResources)] };

        var loadingTcs = new TaskCompletionSource();

        await TrackResourceSnapshotsAsync();

        // Wait for resource to be selected. If selected resource isn't available after a few seconds then stop waiting.
        try
        {
            await loadingTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
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

            var (snapshot, subscription) = await DashboardClient.SubscribeResourcesAsync(_resourceSubscriptionCancellation.Token);

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
                    await SetSelectedResourceOption(selectedResource);
                }
            }
            else
            {
                Logger.LogDebug("No resource selected.");
                loadingTcs.TrySetResult();
            }

            _resourceSubscriptionTask = Task.Run(async () =>
            {
                await foreach (var changes in subscription.WithCancellation(_resourceSubscriptionCancellation.Token).ConfigureAwait(false))
                {
                    // TODO: This could be updated to be more efficient.
                    // It should apply on the resource changes in a batch and then update the UI.
                    foreach (var (changeType, resource) in changes)
                    {
                        await OnResourceChanged(changeType, resource);

                        // the initial snapshot we obtain is [almost] never correct (it's always empty)
                        // we still want to select the user's initial queried resource on page load,
                        // so if there is no selected resource when we
                        // receive an added resource, and that added resource name == ResourceName,
                        // we should mark it as selected
                        if (ResourceName is not null && PageViewModel.SelectedResource is null && changeType == ResourceViewModelChangeType.Upsert && string.Equals(ResourceName, resource.Name))
                        {
                            await SetSelectedResourceOption(resource);
                        }
                    }
                }
            });
        }

        async Task SetSelectedResourceOption(ResourceViewModel resource)
        {
            Debug.Assert(_resources is not null);

            PageViewModel.SelectedOption = _resources.Single(option => option.Id?.Type is not OtlpApplicationType.ResourceGrouping && string.Equals(ResourceName, option.Id?.InstanceId, StringComparison.Ordinal));
            PageViewModel.SelectedResource = resource;

            await this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: false);

            Logger.LogDebug("Selected console resource from name {ResourceName}.", ResourceName);
            loadingTcs.TrySetResult();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogDebug("Initializing console logs view model.");
        if (await this.InitializeViewModelAsync())
        {
            return;
        }

        var selectedResourceName = PageViewModel.SelectedResource?.Name;
        if (!string.Equals(selectedResourceName, _consoleLogsSubscription?.Name, StringComparisons.ResourceName))
        {
            Logger.LogDebug("New resource {ResourceName} selected.", selectedResourceName);

            ConsoleLogsSubscription? newConsoleLogsSubscription = null;
            if (selectedResourceName is not null)
            {
                newConsoleLogsSubscription = new ConsoleLogsSubscription { Name = selectedResourceName };

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    newConsoleLogsSubscription.CancellationToken.Register(state =>
                    {
                        var s = (ConsoleLogsSubscription)state!;
                        Logger.LogDebug("Canceling current subscription to {ResourceName}.", s.Name);
                    }, newConsoleLogsSubscription);
                }
            }

            if (_consoleLogsSubscription is { } currentSubscription)
            {
                currentSubscription.Cancel();
                _consoleLogsSubscription = newConsoleLogsSubscription;

                await TaskHelpers.WaitIgnoreCancelAsync(currentSubscription.SubscriptionTask);
            }
            else
            {
                _consoleLogsSubscription = newConsoleLogsSubscription;
            }

            Logger.LogDebug("Creating new log entries collection.");
            _logEntries = new(Options.Value.Frontend.MaxConsoleLogCount);

            if (newConsoleLogsSubscription is not null)
            {
                LoadLogs(newConsoleLogsSubscription);
            }
        }
    }

    internal static ImmutableList<SelectViewModel<ResourceTypeDetails>> GetConsoleLogResourceSelectViewModels(
        ConcurrentDictionary<string, ResourceViewModel> resourcesByName,
        SelectViewModel<ResourceTypeDetails> noSelectionViewModel,
        string resourceUnknownStateText)
    {
        var builder = ImmutableList.CreateBuilder<SelectViewModel<ResourceTypeDetails>>();

        foreach (var grouping in resourcesByName
            .Where(r => !r.Value.IsHiddenState())
            .OrderBy(c => c.Value.Name, StringComparers.ResourceName)
            .GroupBy(r => r.Value.DisplayName, StringComparers.ResourceName))
        {
            string applicationName;

            if (grouping.Count() > 1)
            {
                applicationName = grouping.Key;

                builder.Add(new SelectViewModel<ResourceTypeDetails>
                {
                    Id = ResourceTypeDetails.CreateApplicationGrouping(applicationName, true),
                    Name = applicationName
                });
            }
            else
            {
                applicationName = grouping.First().Value.DisplayName;
            }

            foreach (var resource in grouping.Select(g => g.Value).OrderBy(r => r.Name, StringComparers.ResourceName))
            {
                builder.Add(ToOption(resource, grouping.Count() > 1, applicationName));
            }
        }

        builder.Insert(0, noSelectionViewModel);
        return builder.ToImmutableList();

        SelectViewModel<ResourceTypeDetails> ToOption(ResourceViewModel resource, bool isReplica, string applicationName)
        {
            var id = isReplica
                ? ResourceTypeDetails.CreateReplicaInstance(resource.Name, applicationName)
                : ResourceTypeDetails.CreateSingleton(resource.Name, applicationName);

            return new SelectViewModel<ResourceTypeDetails>
            {
                Id = id,
                Name = GetDisplayText()
            };

            string GetDisplayText()
            {
                var resourceName = ResourceViewModel.GetResourceName(resource, resourcesByName);

                if (resource.HasNoState())
                {
                    return $"{resourceName} ({resourceUnknownStateText})";
                }

                if (resource.IsRunningState())
                {
                    return resourceName;
                }

                return $"{resourceName} ({resource.State})";
            }
        }
    }

    private void UpdateResourcesList() => _resources = GetConsoleLogResourceSelectViewModels(_resourceByName, _noSelection, Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsUnknownState)]);

    private void LoadLogs(ConsoleLogsSubscription newConsoleLogsSubscription)
    {
        Logger.LogDebug("Starting log subscription.");
        var consoleLogsTask = Task.Run(async () =>
        {
            newConsoleLogsSubscription.CancellationToken.ThrowIfCancellationRequested();

            Logger.LogDebug("Subscribing to console logs for resource {ResourceName}.", newConsoleLogsSubscription.Name);

            var subscription = DashboardClient.SubscribeConsoleLogs(newConsoleLogsSubscription.Name, newConsoleLogsSubscription.CancellationToken);

            PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsWatchingLogs)];
            await InvokeAsync(StateHasChanged);

            try
            {
                var logParser = new LogParser();
                await foreach (var batch in subscription.ConfigureAwait(true))
                {
                    if (batch.Count is 0)
                    {
                        continue;
                    }

                    foreach (var (lineNumber, content, isErrorOutput) in batch)
                    {
                        // Set the base line number using the reported line number of the first log line.
                        if (_logEntries.EntriesCount == 0)
                        {
                            _logEntries.BaseLineNumber = lineNumber;
                        }

                        var logEntry = logParser.CreateLogEntry(content, isErrorOutput);
                        _logEntries.InsertSorted(logEntry);
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }
            finally
            {
                Logger.LogDebug("Finished watching logs for resource {ResourceName}.", newConsoleLogsSubscription.Name);

                // If the subscription is being canceled then a new one could be starting.
                // Don't set the status when finishing because overwrite the status from the new subscription.
                if (!newConsoleLogsSubscription.CancellationToken.IsCancellationRequested)
                {
                    PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)];
                    await InvokeAsync(StateHasChanged);
                }
            }
        });

        newConsoleLogsSubscription.SubscriptionTask = consoleLogsTask;
    }

    private async Task HandleSelectedOptionChangedAsync()
    {
        PageViewModel.SelectedResource = PageViewModel.SelectedOption?.Id?.InstanceId is null ? null : _resourceByName[PageViewModel.SelectedOption.Id.InstanceId];
        await this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: false);
    }

    private async Task OnResourceChanged(ResourceViewModelChangeType changeType, ResourceViewModel resource)
    {
        if (changeType == ResourceViewModelChangeType.Upsert)
        {
            _resourceByName[resource.Name] = resource;
            UpdateResourcesList();

            if (string.Equals(PageViewModel.SelectedResource?.Name, resource.Name, StringComparison.Ordinal))
            {
                PageViewModel.SelectedResource = resource;
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
    }

    private async Task StopAndClearConsoleLogsSubscriptionAsync()
    {
        if (_consoleLogsSubscription is { } consoleLogsSubscription)
        {
            consoleLogsSubscription.Cancel();
            await TaskHelpers.WaitIgnoreCancelAsync(consoleLogsSubscription.SubscriptionTask);

            _consoleLogsSubscription = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _resourceSubscriptionCancellation.CancelAsync();
        _resourceSubscriptionCancellation.Dispose();
        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);

        await StopAndClearConsoleLogsSubscriptionAsync();
    }

    public class ConsoleLogsViewModel
    {
        public required string Status { get; set; }
        public required SelectViewModel<ResourceTypeDetails> SelectedOption { get; set; }
        public required ResourceViewModel? SelectedResource { get; set; }
    }

    public class ConsoleLogsPageState
    {
        public string? SelectedResource { get; set; }
    }

    public Task UpdateViewModelFromQueryAsync(ConsoleLogsViewModel viewModel)
    {
        if (_resources is not null && ResourceName is not null)
        {
            var selectedOption = _resources.FirstOrDefault(c => string.Equals(ResourceName, c.Id?.InstanceId, StringComparisons.ResourceName)) ?? _noSelection;

            viewModel.SelectedOption = selectedOption;
            viewModel.SelectedResource = selectedOption.Id?.InstanceId is null ? null : _resourceByName[selectedOption.Id.InstanceId];
            viewModel.Status ??= Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLogsNotYetAvailable)];
        }
        else
        {
            viewModel.SelectedOption = _noSelection;
            viewModel.SelectedResource = null;
            viewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoResourceSelected)];
        }
        return Task.CompletedTask;
    }

    public string GetUrlFromSerializableViewModel(ConsoleLogsPageState serializable)
    {
        return DashboardUrls.ConsoleLogsUrl(serializable.SelectedResource);
    }

    public ConsoleLogsPageState ConvertViewModelToSerializable()
    {
        return new ConsoleLogsPageState
        {
            SelectedResource = PageViewModel.SelectedResource?.Name
        };
    }
}
