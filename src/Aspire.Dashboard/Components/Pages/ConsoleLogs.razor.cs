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
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Aspire.Hosting.ConsoleLogs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Pages;

public sealed partial class ConsoleLogs : ComponentBase, IComponentWithTelemetry, IAsyncDisposable, IPageWithSessionAndUrlState<ConsoleLogs.ConsoleLogsViewModel, ConsoleLogs.ConsoleLogsPageState>
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
    public required ILocalStorage LocalStorage { get; init; }

    [Inject]
    public required ISessionStorage SessionStorage { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required ILogger<ConsoleLogs> Logger { get; init; }

    [Inject]
    public required IStringLocalizer<Dashboard.Resources.ConsoleLogs> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<Dashboard.Resources.Resources> ResourcesLoc { get; init; }

    [Inject]
    public required IStringLocalizer<Commands> CommandsLoc { get; init; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required DashboardCommandExecutor DashboardCommandExecutor { get; init; }

    [Inject]
    public required ConsoleLogsManager ConsoleLogsManager { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required PauseManager PauseManager { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ComponentTelemetryContextProvider TelemetryContextProvider { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    [Parameter]
    public string? ResourceName { get; set; }

    private readonly CancellationTokenSource _resourceSubscriptionCts = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private ImmutableList<SelectViewModel<ResourceTypeDetails>>? _resources;
    private CancellationToken _resourceSubscriptionToken;
    private Task? _resourceSubscriptionTask;
    private readonly ConcurrentDictionary<string, ConsoleLogsSubscription> _consoleLogsSubscriptions = new(StringComparers.ResourceName);
    private bool _isSubscribedToAll;
    internal LogEntries _logEntries = null!;
    private readonly object _updateLogsLock = new object();

    // UI
    private SelectViewModel<ResourceTypeDetails> _noSelection = null!;
    private SelectViewModel<ResourceTypeDetails> _allResource = null!;
    private AspirePageContentLayout? _contentLayout;
    private readonly List<CommandViewModel> _highlightedCommands = new();
    private readonly List<MenuButtonItem> _logsMenuItems = new();
    private readonly List<MenuButtonItem> _resourceMenuItems = new();

    // State
    private bool _showHiddenResources;
    private bool _showTimestamp;
    private bool _isTimestampUtc;
    private bool _noWrapLogs;
    public ConsoleLogsViewModel PageViewModel { get; set; } = null!;
    private IDisposable? _consoleLogsFiltersChangedSubscription;
    private ConsoleLogsFilters _consoleLogFilters = new();

    public string BasePath => DashboardUrls.ConsoleLogBasePath;
    public string SessionStorageKey => BrowserStorageKeys.ConsoleLogsPageState;

    protected override async Task OnInitializedAsync()
    {
        TelemetryContextProvider.Initialize(TelemetryContext);
        _resourceSubscriptionToken = _resourceSubscriptionCts.Token;
        _logEntries = new(Options.Value.Frontend.MaxConsoleLogCount);
        _noSelection = new() { Id = null, Name = ControlsStringsLoc[nameof(ControlsStrings.LabelNone)] };
        _allResource = new() { Id = null, Name = ControlsStringsLoc[nameof(ControlsStrings.LabelAll)] };
        PageViewModel = new ConsoleLogsViewModel { SelectedOption = _noSelection, SelectedResource = null, Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLoadingResources)] };

        _consoleLogsFiltersChangedSubscription = ConsoleLogsManager.OnFiltersChanged(async () =>
        {
            lock (_updateLogsLock)
            {
                _consoleLogFilters = ConsoleLogsManager.Filters;
                _logEntries.Clear(keepActivePauseEntries: true);
            }

            await InvokeAsync(StateHasChanged);
        });

        var consoleSettingsResult = await LocalStorage.GetUnprotectedAsync<ConsoleLogConsoleSettings>(BrowserStorageKeys.ConsoleLogConsoleSettings);
        if (consoleSettingsResult.Value is { } consoleSettings)
        {
            _showTimestamp = consoleSettings.ShowTimestamp;
            _isTimestampUtc = consoleSettings.IsTimestampUtc;
            _noWrapLogs = consoleSettings.NoWrapLogs;
        }

        var showHiddenResources = await SessionStorage.GetAsync<bool>(BrowserStorageKeys.ResourcesShowHiddenResources);
        if (showHiddenResources.Success)
        {
            _showHiddenResources = showHiddenResources.Value;
        }

        await ConsoleLogsManager.EnsureInitializedAsync();
        _consoleLogFilters = ConsoleLogsManager.Filters;

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
                if (ResourceViewModel.TryGetResourceByName(ResourceName, _resourceByName, out var selectedResource))
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

                    await InvokeAsync(() =>
                    {
                        // The selected resource may have changed, so update resource action buttons.
                        // Update inside in the render's sync context so the buttons don't change while the UI is rendering.
                        UpdateMenuButtons();

                        StateHasChanged();
                    });
                }
            });
        }

        void SetSelectedResourceOption(ResourceViewModel resource)
        {
            PageViewModel.SelectedOption = GetSelectedOption();
            PageViewModel.SelectedResource = resource;

            Logger.LogDebug("Selected console resource from name {ResourceName}.", ResourceName);
            loadingTcs.TrySetResult();
        }
    }

    private SelectViewModel<ResourceTypeDetails> GetSelectedOption()
    {
        Debug.Assert(_resources is not null);
        return _resources.GetResource(Logger, ResourceName, canSelectGrouping: true, fallback: _noSelection);
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogDebug("Initializing console logs view model.");
        if (await this.InitializeViewModelAsync())
        {
            return;
        }

        UpdateMenuButtons();

        // Determine if we're subscribing to "All" resources or a specific resource
        var isAllSelected = PageViewModel.SelectedOption.Id is null && 
                           ReferenceEquals(PageViewModel.SelectedOption, _allResource);
        var selectedResourceName = PageViewModel.SelectedResource?.Name;

        // Check if subscription needs to change
        bool needsNewSubscription = false;
        
        if (isAllSelected && !_isSubscribedToAll)
        {
            // Switching from single resource to "All"
            needsNewSubscription = true;
        }
        else if (!isAllSelected && _isSubscribedToAll)
        {
            // Switching from "All" to single resource
            needsNewSubscription = true;
        }
        else if (!isAllSelected && selectedResourceName is not null)
        {
            // Check if the selected single resource changed
            if (!_consoleLogsSubscriptions.ContainsKey(selectedResourceName))
            {
                needsNewSubscription = true;
            }
        }
        else if (!isAllSelected && selectedResourceName is null)
        {
            // No resource selected
            needsNewSubscription = !_consoleLogsSubscriptions.IsEmpty;
        }

        if (needsNewSubscription)
        {
            Logger.LogDebug("Subscription change needed. IsAllSelected: {IsAllSelected}, SelectedResource: {SelectedResource}", 
                isAllSelected, selectedResourceName);

            // Cancel all existing subscriptions
            await CancelAllSubscriptionsAsync();

            // Clear log entries for new subscription
            Logger.LogDebug("Creating new log entries collection.");
            _logEntries = new(Options.Value.Frontend.MaxConsoleLogCount);

            if (isAllSelected)
            {
                // Subscribe to all available resources
                _isSubscribedToAll = true;
                await SubscribeToAllResourcesAsync();
            }
            else if (selectedResourceName is not null)
            {
                // Subscribe to single resource
                _isSubscribedToAll = false;
                await SubscribeToSingleResourceAsync(selectedResourceName);
            }
            else
            {
                // No resource selected
                _isSubscribedToAll = false;
            }
        }

        UpdateTelemetryProperties();
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

        _logsMenuItems.Add(new()
        {
            IsDivider = true
        });

        CommonMenuItems.AddToggleHiddenResourcesMenuItem(
            _logsMenuItems,
            ControlsStringsLoc,
            _showHiddenResources,
            _resourceByName.Values,
            SessionStorage,
            EventCallback.Factory.Create<bool>(this, async
            value =>
            {
                _showHiddenResources = value;
                UpdateResourcesList();
                UpdateMenuButtons();

                if (!_showHiddenResources && PageViewModel.SelectedResource?.IsResourceHidden(showHiddenResources: false) is true)
                {
                    PageViewModel.SelectedResource = null;
                    PageViewModel.SelectedOption = _noSelection;
                    await this.AfterViewModelChangedAsync(_contentLayout, false);
                }

                await this.RefreshIfMobileAsync(_contentLayout);
            }));

        _logsMenuItems.Add(new()
        {
            OnClick = () => ToggleTimestampAsync(showTimestamp: !_showTimestamp, isTimestampUtc: _isTimestampUtc),
            Text = _showTimestamp ? Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsTimestampHide)] : Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsTimestampShow)],
            Icon = new Icons.Regular.Size16.CalendarClock()
        });

        _logsMenuItems.Add(new()
        {
            OnClick = () => ToggleTimestampAsync(showTimestamp: _showTimestamp, isTimestampUtc: !_isTimestampUtc),
            Text = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsTimestampShowUtc)],
            Icon = _isTimestampUtc ? new Icons.Regular.Size16.CheckboxChecked() : new Icons.Regular.Size16.CheckboxUnchecked(),
            IsDisabled = !_showTimestamp
        });

        _logsMenuItems.Add(new()
        {
            OnClick = () => ToggleWrapLogsAsync(noWrapLogs: !_noWrapLogs),
            Text = _noWrapLogs ? Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsWrapLogs)] : Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoWrapLogs)],
            Icon = _noWrapLogs ? new Icons.Regular.Size16.TextWrap() : new Icons.Regular.Size16.TextWrapOff()
        });

        if (PageViewModel.SelectedResource != null)
        {
            if (ViewportInformation.IsDesktop)
            {
                _highlightedCommands.AddRange(PageViewModel.SelectedResource.Commands.Where(c => c.IsHighlighted && c.State != CommandViewModelState.Hidden).Take(DashboardUIHelpers.MaxHighlightedCommands));
            }

            ResourceMenuItems.AddMenuItems(
                _resourceMenuItems,
                PageViewModel.SelectedResource,
                NavigationManager,
                TelemetryRepository,
                GetResourceName,
                ControlsStringsLoc,
                ResourcesLoc,
                CommandsLoc,
                EventCallback.Factory.Create(this, () =>
                {
                    NavigationManager.NavigateTo(DashboardUrls.ResourcesUrl(resource: PageViewModel.SelectedResource.Name));
                    return Task.CompletedTask;
                }),
                EventCallback.Factory.Create<CommandViewModel>(this, ExecuteResourceCommandAsync),
                (resource, command) => DashboardCommandExecutor.IsExecuting(resource.Name, command.Name),
                showConsoleLogsItem: false,
                showUrls: true);
        }
    }

    private async Task ToggleTimestampAsync(bool showTimestamp, bool isTimestampUtc)
    {
        _showTimestamp = showTimestamp;
        _isTimestampUtc = isTimestampUtc;
        await UpdateConsoleLogSettingsAsync();
    }

    private async Task ToggleWrapLogsAsync(bool noWrapLogs)
    {
        _noWrapLogs = noWrapLogs;
        await UpdateConsoleLogSettingsAsync();
    }

    private async Task UpdateConsoleLogSettingsAsync()
    {
        await LocalStorage.SetUnprotectedAsync(BrowserStorageKeys.ConsoleLogConsoleSettings, new ConsoleLogConsoleSettings(_showTimestamp, _isTimestampUtc, _noWrapLogs));
        UpdateMenuButtons();
        StateHasChanged();
        await this.RefreshIfMobileAsync(_contentLayout);
    }

    private async Task ExecuteResourceCommandAsync(CommandViewModel command)
    {
        await DashboardCommandExecutor.ExecuteAsync(PageViewModel.SelectedResource!, command, GetResourceName);
    }

    private async Task CancelAllSubscriptionsAsync()
    {
        var subscriptionsToCancel = _consoleLogsSubscriptions.Values.ToList();
        _consoleLogsSubscriptions.Clear();

        foreach (var subscription in subscriptionsToCancel)
        {
            subscription.Cancel();
        }

        // Wait for all subscriptions to finish
        var tasks = subscriptionsToCancel
            .Where(s => s.SubscriptionTask is not null)
            .Select(s => TaskHelpers.WaitIgnoreCancelAsync(s.SubscriptionTask))
            .ToArray();

        if (tasks.Length > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task SubscribeToAllResourcesAsync()
    {
        var availableResources = _resourceByName.Values
            .Where(r => !r.IsResourceHidden(_showHiddenResources))
            .ToList();

        Logger.LogDebug("Subscribing to {ResourceCount} resources for 'All' view.", availableResources.Count);

        foreach (var resource in availableResources)
        {
            await SubscribeToSingleResourceAsync(resource.Name);
        }
    }

    private Task SubscribeToSingleResourceAsync(string resourceName)
    {
        if (_consoleLogsSubscriptions.ContainsKey(resourceName))
        {
            Logger.LogDebug("Already subscribed to resource {ResourceName}.", resourceName);
            return Task.CompletedTask;
        }

        var subscription = new ConsoleLogsSubscription { Name = resourceName };
        Logger.LogDebug("Creating new subscription {SubscriptionId} for resource {ResourceName}.", 
            subscription.SubscriptionId, resourceName);

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            subscription.CancellationToken.Register(state =>
            {
                var s = (ConsoleLogsSubscription)state!;
                Logger.LogDebug("Canceling subscription {SubscriptionId} to {ResourceName}.", s.SubscriptionId, s.Name);
            }, subscription);
        }

        _consoleLogsSubscriptions.TryAdd(resourceName, subscription);
        LoadLogsForResource(subscription);
        return Task.CompletedTask;
    }

    private string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourceByName);

    internal static ImmutableList<SelectViewModel<ResourceTypeDetails>> GetConsoleLogResourceSelectViewModels(
        ConcurrentDictionary<string, ResourceViewModel> resourcesByName,
        SelectViewModel<ResourceTypeDetails> noSelectionViewModel,
        SelectViewModel<ResourceTypeDetails> allResourceViewModel,
        string resourceUnknownStateText,
        bool showHiddenResources,
        out SelectViewModel<ResourceTypeDetails>? optionToSelect)
    {
        var builder = ImmutableList.CreateBuilder<SelectViewModel<ResourceTypeDetails>>();

        foreach (var grouping in resourcesByName
            .Where(r => !r.Value.IsResourceHidden(showHiddenResources))
            .OrderBy(c => c.Value, ResourceViewModelNameComparer.Instance)
            .GroupBy(r => r.Value.DisplayName, StringComparers.ResourceName))
        {
            string resourceName;

            if (grouping.Count() > 1)
            {
                resourceName = grouping.Key;

                builder.Add(new SelectViewModel<ResourceTypeDetails>
                {
                    Id = ResourceTypeDetails.CreateResourceGrouping(resourceName, true),
                    Name = resourceName
                });
            }
            else
            {
                resourceName = grouping.First().Value.DisplayName;
            }

            foreach (var resource in grouping.Select(g => g.Value).OrderBy(r => r, ResourceViewModelNameComparer.Instance))
            {
                builder.Add(ToOption(resource, grouping.Count() > 1, resourceName));
            }
        }

        // If there is one resource then it is automatically selected. Remove None button so there is no way to unselect.
        // If there are multiple resources, or zero, then none is the default so add it.
        // Also add "All" option when there are multiple resources.
        if (builder.Count > 1)
        {
            builder.Insert(0, allResourceViewModel);
            builder.Insert(1, noSelectionViewModel);
            optionToSelect = null;
        }
        else if (builder.Count == 1)
        {
            optionToSelect = builder.Single();
        }
        else
        {
            builder.Insert(0, noSelectionViewModel);
            optionToSelect = null;
        }

        return builder.ToImmutableList();

        SelectViewModel<ResourceTypeDetails> ToOption(ResourceViewModel resource, bool isReplica, string resourceName)
        {
            var id = isReplica
                ? ResourceTypeDetails.CreateReplicaInstance(resource.Name, resourceName)
                : ResourceTypeDetails.CreateSingleton(resource.Name, resourceName);

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

    private void UpdateResourcesList()
    {
        _resources = GetConsoleLogResourceSelectViewModels(_resourceByName, _noSelection, _allResource, Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsUnknownState)], _showHiddenResources, out var optionToSelect);

        if (optionToSelect is not null)
        {
            Debug.Assert(optionToSelect.Id?.InstanceId is not null);
            PageViewModel.SelectedOption = optionToSelect;
            PageViewModel.SelectedResource = _resourceByName[optionToSelect.Id.InstanceId];
        }
    }

    private void LoadLogsForResource(ConsoleLogsSubscription subscription)
    {
        Logger.LogDebug("Starting log subscription {SubscriptionId}.", subscription.SubscriptionId);
        var consoleLogsTask = Task.Run(async () =>
        {
            subscription.CancellationToken.ThrowIfCancellationRequested();

            Logger.LogDebug("Subscribing to console logs with subscription {SubscriptionId} to resource {ResourceName}.", subscription.SubscriptionId, subscription.Name);

            var logSubscription = DashboardClient.SubscribeConsoleLogs(subscription.Name, subscription.CancellationToken);

            // Update status only for single resource subscriptions or when this is the first subscription
            var shouldUpdateStatus = !_isSubscribedToAll || _consoleLogsSubscriptions.Count == 1;
            if (shouldUpdateStatus)
            {
                PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsWatchingLogs)];
                await InvokeAsync(StateHasChanged);
            }

            try
            {
                lock (_updateLogsLock)
                {
                    // Only add pause intervals once, not for each resource when subscribing to all
                    if (!_isSubscribedToAll || _consoleLogsSubscriptions.Count == 1)
                    {
                        var pauseIntervals = PauseManager.ConsoleLogPauseIntervals;
                        Logger.LogDebug("Adding {PauseIntervalsCount} pause intervals on initial logs load.", pauseIntervals.Length);

                        foreach (var priorPause in pauseIntervals)
                        {
                            _logEntries.InsertSorted(LogEntry.CreatePause(priorPause.Start, priorPause.End));
                        }
                    }
                }

                // Console logs are filtered in the UI by the timestamp of the log entry.
                var timestampFilterDate = GetFilteredDateFromRemove();

                var logParser = new LogParser(ConsoleColor.Black);
                await foreach (var batch in logSubscription.ConfigureAwait(true))
                {
                    if (batch.Count is 0)
                    {
                        continue;
                    }

                    lock (_updateLogsLock)
                    {
                        foreach (var (lineNumber, content, isErrorOutput) in batch)
                        {
                            // Set the base line number using the reported line number of the first log line.
                            _logEntries.BaseLineNumber ??= lineNumber;

                            // Add resource name prefix for multi-resource views
                            var processedContent = _isSubscribedToAll ? $"[{subscription.Name}] {content}" : content;
                            var logEntry = logParser.CreateLogEntry(processedContent, isErrorOutput);

                            // Check if log entry is not displayed because of remove.
                            if (logEntry.Timestamp is not null && timestampFilterDate is not null && !(logEntry.Timestamp > timestampFilterDate))
                            {
                                continue;
                            }

                            // Check if log entry is not displayed because of pause.
                            if (_logEntries.ProcessPauseFilters(logEntry))
                            {
                                continue;
                            }

                            _logEntries.InsertSorted(logEntry);
                        }
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // If the subscription is being canceled then error could be transient from cancellation. Ignore errors during cancellation.
                if (!subscription.CancellationToken.IsCancellationRequested)
                {
                    Logger.LogError(ex, "Error watching logs for resource {ResourceName}.", subscription.Name);

                    if (shouldUpdateStatus)
                    {
                        PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsErrorWatchingLogs)];
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
            finally
            {
                Logger.LogDebug("Subscription {SubscriptionId} finished watching logs for resource {ResourceName}.", subscription.SubscriptionId, subscription.Name);

                // Remove the subscription from tracking
                _consoleLogsSubscriptions.TryRemove(subscription.Name, out _);

                // If the subscription is being canceled then a new one could be starting.
                // Don't set the status when finishing because overwrite the status from the new subscription.
                if (!subscription.CancellationToken.IsCancellationRequested && shouldUpdateStatus)
                {
                    PageViewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)];
                    await InvokeAsync(StateHasChanged);
                }
            }
        });

        subscription.SubscriptionTask = consoleLogsTask;
    }

    private DateTime? GetFilteredDateFromRemove()
    {
        DateTime? timestampFilterDate;

        if (PageViewModel.SelectedOption.Id is not null &&
            _consoleLogFilters.FilterResourceLogsDates.TryGetValue(
                PageViewModel.SelectedOption.Id.GetResourceKey().ToString(),
                out var filterResourceLogsDate))
        {
            // There is a filter for this individual resource.
            timestampFilterDate = filterResourceLogsDate;
        }
        else
        {
            // Fallback to the global filter (if any, it could be null).
            timestampFilterDate = _consoleLogFilters.FilterAllLogsDate;
        }

        return timestampFilterDate;
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

            // If we're subscribed to all resources and this is a new resource, subscribe to it
            if (_isSubscribedToAll && !_consoleLogsSubscriptions.ContainsKey(resource.Name) && 
                !resource.IsResourceHidden(_showHiddenResources))
            {
                await SubscribeToSingleResourceAsync(resource.Name);
            }
        }
        else if (changeType == ResourceViewModelChangeType.Delete)
        {
            var removed = _resourceByName.TryRemove(resource.Name, out _);
            Debug.Assert(removed, "Cannot remove unknown resource.");

            // Cancel subscription for the deleted resource
            if (_consoleLogsSubscriptions.TryRemove(resource.Name, out var subscription))
            {
                subscription.Cancel();
                _ = TaskHelpers.WaitIgnoreCancelAsync(subscription.SubscriptionTask); // Fire and forget
            }

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
        await CancelAllSubscriptionsAsync();
    }

    private async Task DownloadLogsAsync()
    {
        // Write all log entry content to a stream as UTF8 chars. Strip control sequences from log lines.
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            foreach (var entry in _logEntries.GetEntries())
            {
                if (entry.Type is LogEntryType.Pause)
                {
                    continue;
                }

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
        var fileName = $"{safeDisplayName}-{TimeProvider.GetLocalNow().ToString("yyyyMMddhhmmss", CultureInfo.InvariantCulture)}.txt";

        await JS.InvokeVoidAsync("downloadStreamAsFile", fileName, streamReference);
    }

    private async Task ClearConsoleLogs(ResourceKey? key)
    {
        var now = TimeProvider.GetUtcNow().UtcDateTime;
        if (key is null)
        {
            _consoleLogFilters.FilterAllLogsDate = now;
            _consoleLogFilters.FilterResourceLogsDates?.Clear();
        }
        else
        {
            _consoleLogFilters.FilterResourceLogsDates ??= [];
            _consoleLogFilters.FilterResourceLogsDates[key.Value.ToString()] = now;
        }

        // Save filters to session storage so they're persisted when navigating to and from the console logs page.
        // This makes remove behavior persistant which matches removing telemetry.
        await ConsoleLogsManager.UpdateFiltersAsync(_consoleLogFilters);
    }

    private void OnPausedChanged(bool isPaused)
    {
        Logger.LogDebug("Console logs paused new value: {IsPausedNewValue}", isPaused);

        var timestamp = DateTime.UtcNow;
        PauseManager.SetConsoleLogsPaused(isPaused, timestamp);

        if (PageViewModel.SelectedResource != null)
        {
            lock (_updateLogsLock)
            {
                if (isPaused)
                {
                    Logger.LogDebug("Inserting new pause log entry starting at {StartTimestamp}.", timestamp);
                    _logEntries.InsertSorted(LogEntry.CreatePause(timestamp));
                }
                else
                {
                    var pause = _logEntries.GetEntries().Last().Pause;
                    Debug.Assert(pause is not null);

                    Logger.LogDebug("Updating pause log entry starting at {StartTimestamp} with end of {EndTimestamp}.", pause.StartTime, timestamp);
                    pause.EndTime = timestamp;
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _consoleLogsFiltersChangedSubscription?.Dispose();

        _resourceSubscriptionCts.Cancel();
        _resourceSubscriptionCts.Dispose();
        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);

        await StopAndClearConsoleLogsSubscriptionAsync();
        TelemetryContext.Dispose();
    }

    public class ConsoleLogsViewModel
    {
        public required string Status { get; set; }
        public required SelectViewModel<ResourceTypeDetails> SelectedOption { get; set; }
        public required ResourceViewModel? SelectedResource { get; set; }
    }

    public record ConsoleLogsPageState(string? SelectedResource);

    public record ConsoleLogConsoleSettings(bool ShowTimestamp, bool IsTimestampUtc, bool NoWrapLogs);

    public Task UpdateViewModelFromQueryAsync(ConsoleLogsViewModel viewModel)
    {
        if (_resources is not null)
        {
            if (ResourceName is not null)
            {
                viewModel.SelectedOption = GetSelectedOption();
                viewModel.SelectedResource = viewModel.SelectedOption.Id?.InstanceId is null ? null : _resourceByName[viewModel.SelectedOption.Id.InstanceId];
                viewModel.Status ??= Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLogsNotYetAvailable)];
                return Task.CompletedTask;
            }
            else if (TryGetSingleResource() is { } r)
            {
                // If there is no resource selected and there is only one resource available, select it.
                viewModel.SelectedOption = _resources.GetResource(Logger, r.Name, canSelectGrouping: false, fallback: _noSelection);
                viewModel.SelectedResource = r;
                return this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
            }
        }

        viewModel.SelectedOption = _noSelection;
        viewModel.SelectedResource = null;
        viewModel.Status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoResourceSelected)];
        return Task.CompletedTask;

        ResourceViewModel? TryGetSingleResource()
        {
            var actualResources = _resourceByName.Values.Where(r => !r.IsResourceHidden(showHiddenResources: _showHiddenResources)).ToList();
            return actualResources.Count == 1 ? actualResources[0] : null;
        }
    }

    public string GetUrlFromSerializableViewModel(ConsoleLogsPageState serializable)
    {
        return DashboardUrls.ConsoleLogsUrl(serializable.SelectedResource);
    }

    public ConsoleLogsPageState ConvertViewModelToSerializable()
    {
        var selectedResourceName = PageViewModel.SelectedResource is { } selectedResource
            ? GetResourceName(selectedResource)
            : null;
        return new ConsoleLogsPageState(selectedResourceName);
    }

    // IComponentWithTelemetry impl
    public ComponentTelemetryContext TelemetryContext { get; } = new(ComponentType.Page, TelemetryComponentIds.ConsoleLogs);

    public void UpdateTelemetryProperties()
    {
        TelemetryContext.UpdateTelemetryProperties([
            new ComponentTelemetryProperty(TelemetryPropertyKeys.ConsoleLogsShowTimestamp, new AspireTelemetryProperty(_showTimestamp, AspireTelemetryPropertyType.UserSetting))
        ], Logger);
    }
}
