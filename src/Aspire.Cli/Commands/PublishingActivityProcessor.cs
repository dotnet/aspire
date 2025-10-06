// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class PublishingActivityProcessor
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly List<PublishingActivity> _pendingActivities = new();
    private readonly ILogger _logger;
    private readonly Task _readPublishingActivitiesTask;
    private TaskCompletionSource _activityAvailableTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private PublishingActivity? _currentActivity;
    private CancellationTokenSource? _currentActivityCts;

    public PublishingActivityProcessor(IAsyncEnumerable<PublishingActivity> publishingActivities, ILogger logger, CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _logger = logger;
        _readPublishingActivitiesTask = Task.Run(async () => await ReadPublishingActivitiesAsync(publishingActivities), _cts.Token);
    }

    public async Task ProcessPublishingActivities(Func<PublishingActivity, CancellationToken, Task> callback)
    {
        var waitForInteractionAvailableTask = Task.CompletedTask;

        while (!_cts.IsCancellationRequested)
        {
            // If there are no pending interactions then wait on this task to get notified when one is added.
            await waitForInteractionAvailableTask.WaitAsync(_cts.Token).ConfigureAwait(false);

            await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
            Task currentActivityTask;
            try
            {
                if (_pendingActivities.Count == 0)
                {
                    // Task is set when a new interaction is added.
                    // Continue here will exit the async lock and wait for the task to complete.
                    waitForInteractionAvailableTask = _activityAvailableTcs.Task;
                    continue;
                }

                waitForInteractionAvailableTask = Task.CompletedTask;
                var item = _pendingActivities[0];
                _pendingActivities.RemoveAt(0);

                _currentActivity = item;
                _currentActivityCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                currentActivityTask = callback(item, _currentActivityCts.Token);
            }
            finally
            {
                _semaphore.Release();
            }

            try
            {
                if (currentActivityTask != null)
                {
                    await currentActivityTask;

                    //await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
                    //try
                    //{
                    //    if (_interactionDialogReference?.Dialog == currentDialogReference)
                    //    {
                    //        _interactionDialogReference.Dispose();
                    //        _interactionDialogReference = null;
                    //    }
                    //}
                    //finally
                    //{
                    //    _semaphore.Release();
                    //}
                }
            }
            catch
            {
                // Ignore any exceptions that occur while waiting for the dialog to close.
            }
        }
    }

    private async Task ReadPublishingActivitiesAsync(IAsyncEnumerable<PublishingActivity> publishingActivities)
    {
        try
        {
            await foreach (var item in publishingActivities)
            {
                await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
                try
                {
                    if (item.Type != "prompt")
                    {
                        _pendingActivities.Add(item);
                    }
                    else
                    {
                        if (_currentActivity != null && _currentActivity.Data.Id == item.Data.Id)
                        {
                            _currentActivityCts?.Cancel();
                            _pendingActivities.Insert(0, item);
                        }
                        else
                        {
                            // New or updated interaction.
                            if (_pendingActivities.SingleOrDefault(a => a.Data.Id == item.Data.Id) is { } existingItem)
                            {
                                // Update existing interaction at the same place in collection.
                                var index = _pendingActivities.IndexOf(existingItem);
                                _pendingActivities.RemoveAt(index);
                                _pendingActivities.Insert(index, item); // Reinsert at the same index to maintain order.
                            }
                            else
                            {
                                _pendingActivities.Add(item);
                            }
                        }
                    }

                    NotifyInteractionAvailable();
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while watching interactions.");
        }
    }

    private void NotifyInteractionAvailable()
    {
        // Let current waiters know that an interaction is available.
        _activityAvailableTcs.TrySetResult();

        // Reset the task completion source for future waiters.
        _activityAvailableTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task WaitForFinishedAsync()
    {
        return _readPublishingActivitiesTask;
    }
}
