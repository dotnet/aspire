// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class Resources : ComponentBase, IAsyncDisposable
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

    private ResourceViewModel? SelectedResource { get; set; }

    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly ConcurrentDictionary<string, bool> _allResourceTypes = [];
    private readonly ConcurrentDictionary<string, bool> _visibleResourceTypes;
    private string _filter = "";
    private bool _isTypeFilterVisible;
    private Task? _resourceSubscriptionTask;
    private bool _isLoading = true;

    public Resources()
    {
        _visibleResourceTypes = new(StringComparers.ResourceType);
    }

    private bool Filter(ResourceViewModel resource) => _visibleResourceTypes.ContainsKey(resource.ResourceType) && (_filter.Length == 0 || resource.MatchesFilter(_filter)) && resource.State != ResourceStates.HiddenState;

    protected void OnResourceTypeVisibilityChanged(string resourceType, bool isVisible)
    {
        if (isVisible)
        {
            _visibleResourceTypes[resourceType] = true;
        }
        else
        {
            _visibleResourceTypes.TryRemove(resourceType, out _);
        }

        ClearSelectedResource();
    }

    private void HandleSearchFilterChanged()
    {
        ClearSelectedResource();
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
        }
    }

    private bool HasResourcesWithCommands => _resourceByName.Any(r => r.Value.Commands.Any());

    private IQueryable<ResourceViewModel>? FilteredResources => _resourceByName.Values.Where(Filter).OrderBy(e => e.ResourceType).ThenBy(e => e.Name).AsQueryable();

    private readonly GridSort<ResourceViewModel> _nameSort = GridSort<ResourceViewModel>.ByAscending(p => p.Name);
    private readonly GridSort<ResourceViewModel> _stateSort = GridSort<ResourceViewModel>.ByAscending(p => p.State);
    private readonly GridSort<ResourceViewModel> _startTimeSort = GridSort<ResourceViewModel>.ByDescending(p => p.CreationTimeStamp);

    protected override async Task OnInitializedAsync()
    {
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
            var (snapshot, subscription) = await DashboardClient.SubscribeResourcesAsync(_watchTaskCancellationTokenSource.Token);

            // Apply snapshot.
            foreach (var resource in snapshot)
            {
                var added = _resourceByName.TryAdd(resource.Name, resource);

                _allResourceTypes.TryAdd(resource.ResourceType, true);
                _visibleResourceTypes.TryAdd(resource.ResourceType, true);

                Debug.Assert(added, "Should not receive duplicate resources in initial snapshot data.");
            }

            // Listen for updates and apply.
            _resourceSubscriptionTask = Task.Run(async () =>
            {
                await foreach (var changes in subscription.WithCancellation(_watchTaskCancellationTokenSource.Token))
                {
                    foreach (var (changeType, resource) in changes)
                    {
                        if (changeType == ResourceViewModelChangeType.Upsert)
                        {
                            _resourceByName[resource.Name] = resource;

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

    private void ShowResourceDetails(ResourceViewModel resource)
    {
        if (SelectedResource == resource)
        {
            ClearSelectedResource();
        }
        else
        {
            SelectedResource = resource;
        }
    }

    private void ClearSelectedResource()
    {
        SelectedResource = null;
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

    public async ValueTask DisposeAsync()
    {
        _watchTaskCancellationTokenSource.Cancel();
        _watchTaskCancellationTokenSource.Dispose();
        _logsSubscription?.Dispose();

        await TaskHelpers.WaitIgnoreCancelAsync(_resourceSubscriptionTask);
    }
}
