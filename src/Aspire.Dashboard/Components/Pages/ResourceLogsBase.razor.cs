// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public abstract partial class ResourceLogsBase<TResource> : ComponentBase, IAsyncDisposable
    where TResource : ResourceViewModel
{
    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; init; }
    [Inject]
    public required IJSRuntime JS { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    protected abstract string? ResourceName { get; }
    protected abstract string ResourceType { get; }
    protected abstract string LoadingResourcesMessage { get; }
    protected abstract string NoResourceSelectedMessage { get; }
    protected abstract string LogsNotAvailableMessage { get; }
    protected abstract string UrlPrefix { get; }
    protected abstract string SelectResourceTitle { get; }
    protected virtual bool ConvertTimestampsFromUtc => false;

    protected abstract ViewModelMonitor<TResource> GetViewModelMonitor(IDashboardViewModelService dashboardViewModelService);

    private FluentSelect<TResource>? _resourceSelectComponent;
    private TResource? _selectedResource;
    private readonly Dictionary<string, TResource> _resourceNameMapping = new();
    private IEnumerable<TResource> Resources => _resourceNameMapping.Select(kvp => kvp.Value).OrderBy(c => c.Name);
    private LogViewer? _logViewer;
    private readonly CancellationTokenSource _watchContainersTokenSource = new();
    private CancellationTokenSource? _watchLogsTokenSource;
    private string _status = LogStatus.Initializing;

    protected override void OnInitialized()
    {
        _status = LoadingResourcesMessage;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var viewModelMonitor = GetViewModelMonitor(DashboardViewModelService);
            var initialList = viewModelMonitor.Snapshot;
            var watch = viewModelMonitor.Watch;

            foreach (var result in initialList)
            {
                _resourceNameMapping[result.Name] = result;
            }

            if (ResourceName is not null)
            {
                _selectedResource = initialList.FirstOrDefault(c => string.Equals(ResourceName, c.Name, StringComparison.Ordinal));
            }
            else if (initialList.Count > 0)
            {
                _selectedResource = initialList[0];
            }

            await LoadLogsAsync();

            _ = Task.Run(async () =>
            {
                await foreach (var resourceChanged in watch.WithCancellation(_watchContainersTokenSource.Token))
                {
                    await OnResourceListChangedAsync(resourceChanged.ObjectChangeType, resourceChanged.Resource);
                }
            });

            StateHasChanged();
        }
    }

    private Task ClearLogsAsync()
        => _logViewer is not null ? _logViewer.ClearLogsAsync() : Task.CompletedTask;

    private async ValueTask LoadLogsAsync()
    {
        if (_selectedResource is null)
        {
            _status = NoResourceSelectedMessage;
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
                _status = LogsNotAvailableMessage;
            }
        }
    }

    private async Task HandleSelectedOptionChangedAsync()
    {
        if (_selectedResource is not null)
        {
            // Change the URL
            NavigationManager.NavigateTo($"{UrlPrefix}/{_selectedResource.Name}");
            await StopWatchingLogsAsync();
            await ClearLogsAsync();
            await LoadLogsAsync();
        }
    }

    private async Task OnResourceListChangedAsync(ObjectChangeType changeType, TResource resourceViewModel)
    {
        if (changeType == ObjectChangeType.Added)
        {
            _resourceNameMapping[resourceViewModel.Name] = resourceViewModel;

            if (_selectedResource is null)
            {
                if (string.IsNullOrEmpty(ResourceName) || string.Equals(ResourceName, resourceViewModel.Name, StringComparison.Ordinal))
                {
                    _selectedResource = resourceViewModel;
                    await LoadLogsAsync();
                }
            }
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
                if (_resourceNameMapping.Count > 0)
                {
                    _selectedResource = Resources.First();
                    await HandleSelectedOptionChangedAsync();
                }
            }
        }

        await InvokeAsync(StateHasChanged);

        // Workaround for issue in fluent-select web component where the display value of the
        // selected item doesn't update automatically when the item changes
        await UpdateResourceListSelectedResourceAsync();
    }

    private static string GetDisplayText(TResource resource)
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
        return $"{resource.Name}{stateText}";
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeWatchContainersTokenSource();
        await StopWatchingLogsAsync();
    }

    private async Task DisposeWatchContainersTokenSource()
    {
        await _watchContainersTokenSource.CancelAsync();
        _watchContainersTokenSource.Dispose();
    }

    private async Task StopWatchingLogsAsync()
    {
        if (_watchLogsTokenSource is not null)
        {
            await _watchLogsTokenSource.CancelAsync();
            _watchLogsTokenSource.Dispose();
            // The token source only gets created if selected resource is not null
            await _selectedResource!.LogSource.StopAsync();
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
