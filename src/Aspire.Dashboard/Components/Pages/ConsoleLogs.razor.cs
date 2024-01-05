// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public sealed partial class ConsoleLogs : ComponentBase, IAsyncDisposable
{
    [Inject]
    public required IDashboardClient DashboardClient { get; init; }
    [Inject]
    public required IJSRuntime JS { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Parameter]
    public string? ResourceName { get; set; }

    private readonly TaskCompletionSource _whenDomReady = new();
    private readonly CancellationTokenSource _resourceSubscriptionCancellation = new();
    private readonly CancellationSeries _logSubscriptionCancellationSeries = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);

    // UI
    private FluentSelect<Option<string>>? _resourceSelectComponent;
    private Option<string> _noSelection = null!;
    private LogViewer _logViewer = null!;

    // State
    private string _status = null!;
    private Option<string>? _selectedOption;
    private ResourceViewModel? _selectedResource;
    private bool? _initialisedSuccessfully;

    private List<Option<string>>? _resources;

    protected override void OnInitialized()
    {
        _noSelection = new() { Value = null, Text = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsSelectAResource)] };
        _status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLoadingResources)];

        TrackResources();

        void TrackResources()
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
                }
            });
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

    protected override async Task OnParametersSetAsync()
    {
        if (_resources is not null && ResourceName is not null)
        {
            _selectedOption = _resources.FirstOrDefault(c => string.Equals(ResourceName, c.Value, StringComparisons.ResourceName)) ?? _noSelection;
            _selectedResource = _selectedOption.Value is null ? null : _resourceByName[_selectedOption.Value];
            await LoadLogsAsync();
        }
        else
        {
            await StopWatchingLogsAsync();
            await ClearLogsAsync();
            _selectedOption = _noSelection;
            _selectedResource = null;
            _status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoResourceSelected)];
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

        if (_selectedResource is null)
        {
            _status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsNoResourceSelected)];
        }
        else if (_logViewer is null)
        {
            _status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsInitializingLogViewer)];
        }
        else
        {
            var cancellationToken = await _logSubscriptionCancellationSeries.NextAsync();

            var subscription = DashboardClient.SubscribeConsoleLogs(_selectedResource.Name, cancellationToken);

            if (subscription is not null)
            {
                var task = _logViewer.SetLogSourceAsync(
                    subscription,
                    convertTimestampsFromUtc: _selectedResource.IsContainer());

                _initialisedSuccessfully = true;
                _status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsWatchingLogs)];

                // Indicate when logs finish (other than by cancellation).
                _ = task.ContinueWith(
                    _ => _status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)],
                    CancellationToken.None,
                    TaskContinuationOptions.NotOnCanceled,
                    TaskScheduler.Current);
            }
            else
            {
                _initialisedSuccessfully = false;
                _status = Loc[_selectedResource.IsContainer()
                    ? nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFailedToInitialize)
                    : nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsLogsNotYetAvailable)];
            }
        }
    }

    private async Task HandleSelectedOptionChangedAsync()
    {
        await StopWatchingLogsAsync();
        await ClearLogsAsync();
        NavigationManager.NavigateTo($"/ConsoleLogs/{_selectedOption?.Value}");
    }

    private async Task OnResourceChanged(ResourceViewModelChangeType changeType, ResourceViewModel resource)
    {
        if (changeType == ResourceViewModelChangeType.Upsert)
        {
            _resourceByName[resource.Name] = resource;

            if (string.Equals(_selectedResource?.Name, resource.Name, StringComparison.Ordinal))
            {
                // The selected resource was updated
                _selectedResource = resource;

                if (_initialisedSuccessfully is false)
                {
                    await LoadLogsAsync();
                }
                else if (!string.Equals(_selectedResource.State, "Running", StringComparison.Ordinal))
                {
                    _status = Loc[nameof(Dashboard.Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)];
                }
            }
        }
        else if (changeType == ResourceViewModelChangeType.Delete)
        {
            var removed = _resourceByName.TryRemove(resource.Name, out _);
            Debug.Assert(removed, "Cannot remove unknown resource.");

            if (string.Equals(_selectedResource?.Name, resource.Name, StringComparison.Ordinal))
            {
                // The selected resource was deleted
                _selectedOption = _noSelection;
                await HandleSelectedOptionChangedAsync();
            }
        }

        UpdateResourcesList();

        await InvokeAsync(StateHasChanged);

        // Workaround for issue in fluent-select web component where the display value of the
        // selected item doesn't update automatically when the item changes
        if (_resourceSelectComponent is not null && JS is not null)
        {
            await JS.InvokeVoidAsync("updateFluentSelectDisplayValue", _resourceSelectComponent.Element);
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

    private async Task StopWatchingLogsAsync()
    {
        await _logSubscriptionCancellationSeries.ClearAsync();
    }
}
