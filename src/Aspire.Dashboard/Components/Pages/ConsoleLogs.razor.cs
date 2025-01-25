// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Pages;

public sealed partial class ConsoleLogs : ComponentBase, IAsyncDisposable, IPageWithSessionAndUrlState<ConsoleLogs.ConsoleLogsViewModel, ConsoleLogs.ConsoleLogsPageState>
{
    private sealed class ConsoleLogsSubscription
    {
        private static int s_subscriptionId;

        private readonly CancellationTokenSource _cts = new();
        private readonly int _subscriptionId = Interlocked.Increment(ref s_subscriptionId);

        public required string Name { get; init; }
        public Task? SubscriptionTask { get; set; }

        public CancellationToken CancellationToken => _cts.Token;
        public int SubscriptionId => _subscriptionId;
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
    public required IStringLocalizer<Dashboard.Resources.ConsoleLogs> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required DashboardCommandExecutor DashboardCommandExecutor { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    [Parameter]
    public string? ResourceName { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "hideTimestamp")]
    public bool? HideTimestamp { get; set; }

    private readonly CancellationTokenSource _resourceSubscriptionCts = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private ImmutableList<SelectViewModel<ResourceTypeDetails>>? _resources;
    private CancellationToken _resourceSubscriptionToken;
    private Task? _resourceSubscriptionTask;
    private ConsoleLogsSubscription? _consoleLogsSubscription;
    internal LogEntries _logEntries = null!;

    // UI
    private SelectViewModel<ResourceTypeDetails> _noSelection = null!;
    private AspirePageContentLayout? _contentLayout;
    private readonly List<CommandViewModel> _highlightedCommands = new();
    private readonly List<MenuButtonItem> _logsMenuItems = new();
    private readonly List<MenuButtonItem> _resourceMenuItems = new();

    // State
    public ConsoleLogsViewModel PageViewModel { get; set; } = null!;

    public string BasePath => DashboardUrls.ConsoleLogBasePath;
    public string SessionStorageKey => BrowserStorageKeys.ConsoleLogsPageState;

    protected override async Task OnInitializedAsync()
    {
        _resourceSubscriptionToken = _resourceSubscriptionCts.Token;
        _logEntries = new(Options.Value.Frontend.MaxConsoleLogCount);
        _noSelection = new() { Id = null, Name = ControlsStringsLoc[nameof(ControlsStrings.LabelNone)] };
        PageViewModel = new ConsoleLogsViewModel { SelectedOption = _noSelection, SelectedResource = null, Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLoadingResources)], HideTimestamp = false };

        var loadingTcs = new TaskCompletionSource();

        await TrackResourceSnapshotsAsync();

        // Wait for resource to be selected. If selected resource isn't available after a few seconds then stop waiting.
        try
        {
            await loadingTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), _resourceSubscriptionToken);
            Logger.LogDebug("Loading task completed.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("Load task canceled.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Load timeout while waiting for resource {ResourceName}.", ResourceName);
            PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLogsNotYetAvailable)];
        }

        async Task TrackResourceSnapshotsAsync()
        {
            if (!DashboardClient.IsEnabled)
            {
                return;
            }

            var (snapshot, subscription) = await DashboardClient.SubscribeResourcesAsync(_resourceSubscriptionToken);

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
                    SetSelectedResourceOption(selectedResource);
                }
            }
            else
            {
                Logger.LogDebug("No resource selected.");
                loadingTcs.TrySetResult();
            }

            _resourceSubscriptionTask = Task.Run(async () =>
            {
                await foreach (var changes in subscription.WithCancellation(_resourceSubscriptionToken).ConfigureAwait(false))
                {
                    foreach (var (changeType, resource) in changes)
                    {
                        await OnResourceChanged(changeType, resource);

                        // the initial snapshot we obtain is [almost] never correct (it's always empty)
                        // we still want to select the user's initial queried resource on page load,
                        // so if there is no selected resource when we
                        // receive an added resource, and that added resource name == ResourceName,
                        // we should mark it as selected
                        if (ResourceName is not null && PageViewModel.SelectedResource is null && changeType == ResourceViewModelChangeType.Upsert && string.Equals(ResourceName, resource.Name, StringComparisons.ResourceName))
                        {
                            SetSelectedResourceOption(resource);
                        }
                    }

                    UpdateMenuButtons();
                    await InvokeAsync(StateHasChanged);
                }
            });
        }

        void SetSelectedResourceOption(ResourceViewModel resource)
        {
            Debug.Assert(_resources is not null);

            PageViewModel.SelectedOption = _resources.Single(option => option.Id?.Type is not OtlpApplicationType.ResourceGrouping && string.Equals(ResourceName, option.Id?.InstanceId, StringComparison.Ordinal));
            PageViewModel.SelectedResource = resource;

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

        UpdateMenuButtons();

        var selectedResourceName = PageViewModel.SelectedResource?.Name;
        if (!string.Equals(selectedResourceName, _consoleLogsSubscription?.Name, StringComparisons.ResourceName))
        {
            Logger.LogDebug("New resource {ResourceName} selected.", selectedResourceName);

            ConsoleLogsSubscription? newConsoleLogsSubscription = null;
            if (selectedResourceName is not null)
            {
                newConsoleLogsSubscription = new ConsoleLogsSubscription { Name = selectedResourceName };
                Logger.LogDebug("Creating new subscription {SubscriptionId}.", newConsoleLogsSubscription.SubscriptionId);

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    newConsoleLogsSubscription.CancellationToken.Register(state =>
                    {
                        var s = (ConsoleLogsSubscription)state!;
                        Logger.LogDebug("Canceling subscription {SubscriptionId} to {ResourceName}.", s.SubscriptionId, s.Name);
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

    private void UpdateMenuButtons()
    {
        _highlightedCommands.Clear();
        _logsMenuItems.Clear();
        _resourceMenuItems.Clear();

        _logsMenuItems.Add(new()
        {
            IsDisabled = PageViewModel.SelectedResource is null,
            OnClick = DownloadLogsAsync,
            Text = Loc[nameof(Dashboard.Resources.ConsoleLogs.DownloadLogs)],
            Icon = new Icons.Regular.Size16.ArrowDownload()
        });

        if (PageViewModel.HideTimestamp)
        {
            _logsMenuItems.Add(new()
            {
                OnClick = () => ToggleTimestamp(hideTimestamp: false),
                Text = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsTimestampShow)],
                Icon = new Icons.Regular.Size16.CalendarClock()
            });
        }
        else
        {
            _logsMenuItems.Add(new()
            {
                OnClick = () => ToggleTimestamp(hideTimestamp: true),
                Text = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsTimestampHide)],
                Icon = new Icons.Regular.Size16.DismissSquareMultiple()
            });
        }

        if (PageViewModel.SelectedResource != null)
        {
            if (ViewportInformation.IsDesktop)
            {
                _highlightedCommands.AddRange(PageViewModel.SelectedResource.Commands.Where(c => c.IsHighlighted && c.State != CommandViewModelState.Hidden).Take(DashboardUIHelpers.MaxHighlightedCommands));
            }

            var menuCommands = PageViewModel.SelectedResource.Commands.Where(c => !_highlightedCommands.Contains(c) && c.State != CommandViewModelState.Hidden).ToList();
            if (menuCommands.Count > 0)
            {
                foreach (var command in menuCommands)
                {
                    var icon = (!string.IsNullOrEmpty(command.IconName) && CommandViewModel.ResolveIconName(command.IconName, command.IconVariant) is { } i) ? i : null;

                    _resourceMenuItems.Add(new MenuButtonItem
                    {
                        Text = command.DisplayName,
                        Tooltip = command.DisplayDescription,
                        Icon = icon,
                        OnClick = () => ExecuteResourceCommandAsync(command),
                        IsDisabled = command.State == CommandViewModelState.Disabled
                    });
                }
            }
        }
    }

    private async Task ToggleTimestamp(bool hideTimestamp)
    {
        PageViewModel.HideTimestamp = hideTimestamp;
        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
    }

    private async Task ExecuteResourceCommandAsync(CommandViewModel command)
    {
        await DashboardCommandExecutor.ExecuteAsync(PageViewModel.SelectedResource!, command, GetResourceName);
    }

    private string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourceByName);

    internal static ImmutableList<SelectViewModel<ResourceTypeDetails>> GetConsoleLogResourceSelectViewModels(
        ConcurrentDictionary<string, ResourceViewModel> resourcesByName,
        SelectViewModel<ResourceTypeDetails> noSelectionViewModel,
        string resourceUnknownStateText)
    {
        var builder = ImmutableList.CreateBuilder<SelectViewModel<ResourceTypeDetails>>();

        foreach (var grouping in resourcesByName
            .Where(r => !r.Value.IsHiddenState())
            .OrderBy(c => c.Value, ResourceViewModelNameComparer.Instance)
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

            foreach (var resource in grouping.Select(g => g.Value).OrderBy(r => r, ResourceViewModelNameComparer.Instance))
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
        Logger.LogDebug("Starting log subscription {SubscriptionId}.", newConsoleLogsSubscription.SubscriptionId);
        var consoleLogsTask = Task.Run(async () =>
        {
            newConsoleLogsSubscription.CancellationToken.ThrowIfCancellationRequested();

            Logger.LogDebug("Subscribing to console logs with subscription {SubscriptionId} to resource {ResourceName}.", newConsoleLogsSubscription.SubscriptionId, newConsoleLogsSubscription.Name);

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
                Logger.LogDebug("Subscription {SubscriptionId} finished watching logs for resource {ResourceName}.", newConsoleLogsSubscription.SubscriptionId, newConsoleLogsSubscription.Name);

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
        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
    }

    private async Task OnResourceChanged(ResourceViewModelChangeType changeType, ResourceViewModel resource)
    {
        if (changeType == ResourceViewModelChangeType.Upsert)
        {
            _resourceByName[resource.Name] = resource;
            UpdateResourcesList();

            if (string.Equals(PageViewModel.SelectedResource?.Name, resource.Name, StringComparisons.ResourceName))
            {
                PageViewModel.SelectedResource = resource;
            }
        }
        else if (changeType == ResourceViewModelChangeType.Delete)
        {
            var removed = _resourceByName.TryRemove(resource.Name, out _);
            Debug.Assert(removed, "Cannot remove unknown resource.");

            if (string.Equals(PageViewModel.SelectedResource?.Name, resource.Name, StringComparisons.ResourceName))
            {
                // The selected resource was deleted
                PageViewModel.SelectedOption = _noSelection;
                await HandleSelectedOptionChangedAsync();
            }

            UpdateResourcesList();
        }
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

    private async Task DownloadLogsAsync()
    {
        // Write all log entry content to a stream as UTF8 chars. Strip control sequences from log lines.
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            foreach (var entry in _logEntries.GetEntries())
            {
                // It's ok to use sync stream methods here because we're writing to a MemoryStream.
                if (entry.RawContent is not null)
                {
                    writer.WriteLine(AnsiParser.StripControlSequences(entry.RawContent));
                }
                else
                {
                    writer.WriteLine();
                }
            }
            writer.Flush();
        }
        stream.Seek(0, SeekOrigin.Begin);

        using var streamReference = new DotNetStreamReference(stream);
        var safeDisplayName = string.Join("_", PageViewModel.SelectedResource!.DisplayName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeDisplayName}-{DateTime.Now.ToString("yyyyMMddhhmmss", CultureInfo.InvariantCulture)}.txt";

        await JS.InvokeVoidAsync("downloadStreamAsFile", fileName, streamReference);
    }

    public async ValueTask DisposeAsync()
    {
        _resourceSubscriptionCts.Cancel();
        _resourceSubscriptionCts.Dispose();
        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);

        await StopAndClearConsoleLogsSubscriptionAsync();
    }

    public class ConsoleLogsViewModel
    {
        public required string Status { get; set; }
        public required SelectViewModel<ResourceTypeDetails> SelectedOption { get; set; }
        public required ResourceViewModel? SelectedResource { get; set; }
        public required bool HideTimestamp { get; set; }
    }

    public class ConsoleLogsPageState
    {
        public string? SelectedResource { get; set; }
        public bool HideTimestamp { get; set; }
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

        if (HideTimestamp is { } hideTimestamp)
        {
            viewModel.HideTimestamp = hideTimestamp;
        }

        return Task.CompletedTask;
    }

    public string GetUrlFromSerializableViewModel(ConsoleLogsPageState serializable)
    {
        return DashboardUrls.ConsoleLogsUrl(serializable.SelectedResource, serializable.HideTimestamp);
    }

    public ConsoleLogsPageState ConvertViewModelToSerializable()
    {
        return new ConsoleLogsPageState
        {
            SelectedResource = PageViewModel.SelectedResource?.Name,
            HideTimestamp = PageViewModel.HideTimestamp
        };
    }
}
