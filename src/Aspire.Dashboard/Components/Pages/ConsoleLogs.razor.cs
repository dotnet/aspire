// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class ConsoleLogs : ComponentBase, IAsyncDisposable
{
    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; init; }
    [Inject]
    public required IJSRuntime JS { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Parameter]
    public string? ResourceName { get; set; }

    private bool ConvertTimestampsFromUtc => _selectedResource is ContainerViewModel;

    private FluentSelect<Option<string>>? _resourceSelectComponent;
    private Option<string>? _selectedOption;
    private ResourceViewModel? _selectedResource;
    private readonly Dictionary<string, ResourceViewModel> _resourceNameMapping = new();
    private List<Option<string>>? Resources { get; set; }
    private LogViewer? _logViewer;
    private readonly CancellationTokenSource _watchResourcesCts = new();
    private CancellationTokenSource? _watchLogsTokenSource;
    private string _status = LogStatus.Initializing;

    private readonly TaskCompletionSource _renderCompleteTcs = new();

    private readonly Option<string> _noSelection = new() { Value = null, Text = "(Select a resource)" };

    private const int InstanceIdHintLimit = 8;

    protected override void OnInitialized()
    {
        _status = LogStatus.LoadingResources;

        var viewModelMonitor = DashboardViewModelService.GetResources();
        var initialList = viewModelMonitor.Snapshot;
        var watch = viewModelMonitor.Watch;

        foreach (var result in initialList)
        {
            _resourceNameMapping[result.Name] = result;
        }

        UpdateResourcesList();

        _ = Task.Run(async () =>
        {
            await foreach (var resourceChanged in watch.WithCancellation(_watchResourcesCts.Token))
            {
                await OnResourceListChangedAsync(resourceChanged.ObjectChangeType, resourceChanged.Resource);
            }
        });

        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // Let anyone waiting know that the render is complete so we have access to the underlying log viewer
            _renderCompleteTcs.SetResult();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Resources is not null && ResourceName is not null)
        {
            _selectedOption = Resources.FirstOrDefault(c => string.Equals(ResourceName, c.Value, StringComparison.Ordinal)) ?? _noSelection;
            _selectedResource = _selectedOption.Value is null ? null : _resourceNameMapping[_selectedOption.Value];
            await LoadLogsAsync();
        }
        else
        {
            await StopWatchingLogsAsync();
            await ClearLogsAsync();
            _selectedOption = _noSelection;
            _selectedResource = null;
            _status = LogStatus.NoResourceSelected;
        }
    }

    private static Option<string> GetOption(ResourceViewModel resource)
    {
        return new Option<string>()
        {
            Value = resource.Name,
            Text = GetDisplayText(resource)
        };
    }

    private void UpdateResourcesList()
    {
        Resources = _resourceNameMapping.Values
            .OrderBy(c => c.Name)
            .Select(GetOption)
            .ToList();

        Resources.Insert(0, _noSelection);
    }

    private Task ClearLogsAsync()
        => _logViewer is not null ? _logViewer.ClearLogsAsync() : Task.CompletedTask;

    private async ValueTask LoadLogsAsync()
    {
        // Wait for the first render to complete so that the log viewer is available
        await _renderCompleteTcs.Task;

        if (_selectedResource is null)
        {
            _status = LogStatus.NoResourceSelected;
        }
        else if (_logViewer is null)
        {
            _status = LogStatus.InitializingLogViewer;
        }
        else
        {
            _watchLogsTokenSource = new CancellationTokenSource();
            if (await _selectedResource.LogSource.StartAsync(_watchLogsTokenSource.Token))
            {
                var outputTask = Task.Run(async () =>
                {
                    await _logViewer.WatchLogsAsync(
                        () => _selectedResource.LogSource.WatchOutputLogAsync(_watchLogsTokenSource.Token),
                        new LogParserOptions()
                        {
                            ConvertTimestampsFromUtc = ConvertTimestampsFromUtc
                        }
                    );
                });

                var errorTask = Task.Run(async () =>
                {
                    await _logViewer.WatchLogsAsync(
                        () => _selectedResource.LogSource.WatchErrorLogAsync(_watchLogsTokenSource.Token),
                        new LogParserOptions()
                        {
                            ConvertTimestampsFromUtc = ConvertTimestampsFromUtc,
                            LogEntryType = LogEntryType.Error
                        }
                    );
                });

                _ = Task.WhenAll(outputTask, errorTask).ContinueWith((task) =>
                {
                    // If the task was canceled, that means one or both of the underlying tasks were canceled
                    // which only really happens when we switch to another container source or when leaving
                    // page. In both of those situations we can skip updating the status because it'll just
                    // cause a flash of text change before it changes again or the page is navigated away.
                    if (!task.IsCanceled)
                    {
                        _status = LogStatus.FinishedWatchingLogs;
                    }
                }, TaskScheduler.Current);

                _status = LogStatus.WatchingLogs;
            }
            else
            {
                _watchLogsTokenSource = null;
                if (_selectedResource is ContainerViewModel)
                {
                    _status = LogStatus.FailedToInitialize;
                }
                else
                {
                    _status = LogStatus.LogsNotYetAvailable;
                }
            }
        }
    }

    private async Task HandleSelectedOptionChangedAsync()
    {
        await StopWatchingLogsAsync();
        await ClearLogsAsync();
        NavigationManager.NavigateTo($"/ConsoleLogs/{_selectedOption?.Value}");
    }

    private async Task OnResourceListChangedAsync(ObjectChangeType changeType, ResourceViewModel resourceViewModel)
    {
        if (changeType == ObjectChangeType.Added)
        {
            _resourceNameMapping[resourceViewModel.Name] = resourceViewModel;
        }
        else if (changeType == ObjectChangeType.Modified)
        {
            _resourceNameMapping[resourceViewModel.Name] = resourceViewModel;
            if (string.Equals(_selectedResource?.Name, resourceViewModel.Name, StringComparison.Ordinal))
            {
                _selectedResource = resourceViewModel;

                if (_watchLogsTokenSource is null)
                {
                    await LoadLogsAsync();
                }
                else if (!string.Equals(_selectedResource.State, "Running", StringComparison.Ordinal))
                {
                    _status = LogStatus.FinishedWatchingLogs;
                }
            }
        }
        else if (changeType == ObjectChangeType.Deleted)
        {
            _resourceNameMapping.Remove(resourceViewModel.Name);
            if (string.Equals(_selectedResource?.Name, resourceViewModel.Name, StringComparison.Ordinal))
            {
                _selectedOption = _noSelection;
                await HandleSelectedOptionChangedAsync();
            }
        }

        UpdateResourcesList();

        await InvokeAsync(StateHasChanged);

        // Workaround for issue in fluent-select web component where the display value of the
        // selected item doesn't update automatically when the item changes
        await UpdateResourceListSelectedResourceAsync();
    }

    private static string GetDisplayText(ResourceViewModel resource)
    {
        var stateText = "";
        if (string.IsNullOrEmpty(resource.State))
        {
            stateText = " (Unknown State)";
        }
        else if (resource.State != "Running")
        {
            stateText = $" ({resource.State})";
        }
        var resourceName = resource.DisplayName;
        switch (resource)
        {
            case ExecutableViewModel evm:
                resourceName += $" ({evm.Uid.Substring(0, Math.Min(evm.Uid.Length, InstanceIdHintLimit))})";
                break;
            case ProjectViewModel pvm:
                resourceName += $" ({pvm.Uid.Substring(0, Math.Min(pvm.Uid.Length, InstanceIdHintLimit))})";
                break;
            case ContainerViewModel cvm:
                if (cvm.ContainerId is not null)
                {
                    resourceName += $" (container ID: {cvm.ContainerId.Substring(0, Math.Min(cvm.ContainerId.Length, 8))})";
                }
                break;
        }
        return $"{resourceName}{stateText}";
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeWatchContainersTokenSource();
        await StopWatchingLogsAsync();
    }

    private async Task DisposeWatchContainersTokenSource()
    {
        await _watchResourcesCts.CancelAsync();
        _watchResourcesCts.Dispose();
    }

    private async Task StopWatchingLogsAsync()
    {
        if (_watchLogsTokenSource is not null)
        {
            await _watchLogsTokenSource.CancelAsync();
            _watchLogsTokenSource.Dispose();
            if (_selectedResource?.LogSource is not null)
            {
                await _selectedResource.LogSource.StopAsync();
            }
            _watchLogsTokenSource = null;
        }
    }

    private async Task UpdateResourceListSelectedResourceAsync()
    {
        if (_resourceSelectComponent is not null && JS is not null)
        {
            await JS.InvokeVoidAsync("updateFluentSelectDisplayValue", _resourceSelectComponent.Element);
        }
    }
}
