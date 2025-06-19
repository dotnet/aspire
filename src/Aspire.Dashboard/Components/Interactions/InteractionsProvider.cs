// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Aspire.ResourceService.Proto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Interactions;

public class InteractionsProvider : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly KeyedInteractionCollection _pendingInteractions = new();

    private Task? _interactionsDisplayTask;
    private Task? _watchInteractionsTask;
    private TaskCompletionSource _interactionAvailableTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IDialogReference? _interactionDialogReference;
    private WatchInteractionsResponseUpdate? _interactionDialogInstance;

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required ILogger<InteractionsProvider> Logger { get; init; }

    protected override void OnInitialized()
    {
        // Exit quickly if the dashboard client is not enabled. For example, the dashboard is running in the standalone container.
        if (!DashboardClient.IsEnabled)
        {
            return;
        }

        _interactionsDisplayTask = Task.Run(async () =>
        {
            var waitForInteractionAvailableTask = Task.CompletedTask;

            while (!_cts.IsCancellationRequested)
            {
                // If there are no pending interactions then wait on this task to get notified when one is added.
                await waitForInteractionAvailableTask.WaitAsync(_cts.Token).ConfigureAwait(false);

                IDialogReference? currentDialogReference = null;

                await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
                try
                {
                    if (_pendingInteractions.Count == 0)
                    {
                        // Task is set when a new interaction is added.
                        // Continue here will exit the async lock and wait for the task to complete.
                        waitForInteractionAvailableTask = _interactionAvailableTcs.Task;
                        continue;
                    }

                    waitForInteractionAvailableTask = Task.CompletedTask;
                    var item = ((IList<WatchInteractionsResponseUpdate>)_pendingInteractions)[0];
                    _pendingInteractions.RemoveAt(0);

                    Func<IDialogService, Task<IDialogReference>> openDialog;

                    if (item.ConfirmationDialog is { } confirmation)
                    {
                        var dialogParameters = new DialogParameters<MessageBoxContent>
                        {
                            Content = new MessageBoxContent
                            {
                                Title = item.Title,
                                MarkupMessage = new MarkupString(item.Message),
                                Intent = MessageBoxIntent.Custom
                            },
                            PrimaryAction = "OK",
                            PrimaryActionEnabled = true,
                            PreventDismissOnOverlayClick = true,
                            OnDialogResult = EventCallback.Factory.Create<DialogResult>(this, async dialogResult =>
                            {
                                var request = new WatchInteractionsRequestUpdate
                                {
                                    InteractionId = item.InteractionId
                                };

                                if (dialogResult.Cancelled)
                                {
                                    request.Complete = new InteractionComplete();
                                }
                                else
                                {
                                    request.ConfirmationDialog = item.ConfirmationDialog;
                                }

                                await DashboardClient.SendInteractionRequestAsync(request, _cts.Token).ConfigureAwait(false);
                            }),
                        };

                        openDialog = dialogService => ShowMessageBoxAsync(dialogService, dialogParameters);
                    }
                    else if (item.InputsDialog is { } inputs)
                    {
                        var vm = new InteractionsInputsDialogViewModel
                        {
                            Interaction = item,
                            Inputs = inputs.InputItems.ToList()
                        };
                        var parameters = new DialogParameters
                        {
                            Title = item.Title,
                            PreventDismissOnOverlayClick = true,
                            OnDialogResult = EventCallback.Factory.Create<DialogResult>(this, async dialogResult =>
                            {
                                var request = new WatchInteractionsRequestUpdate
                                {
                                    InteractionId = item.InteractionId
                                };

                                if (dialogResult.Cancelled)
                                {
                                    request.Complete = new InteractionComplete();
                                }
                                else
                                {
                                    request.InputsDialog = item.InputsDialog;
                                }

                                await DashboardClient.SendInteractionRequestAsync(request, _cts.Token).ConfigureAwait(false);
                            })
                        };

                        openDialog = dialogService => dialogService.ShowDialogAsync<InteractionsInputDialog>(vm, parameters);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected interaction type.");
                    }

                    await InvokeAsync(async () =>
                    {
                        _interactionDialogReference = currentDialogReference = await openDialog(DialogService);
                        _interactionDialogInstance = item;
                    });
                }
                finally
                {
                    _semaphore.Release();
                }

                try
                {
                    if (currentDialogReference != null)
                    {
                        await currentDialogReference.Result.WaitAsync(_cts.Token);
                    }
                }
                catch
                {
                    // Ignore any exceptions that occur while waiting for the dialog to close.
                }
            }
        });

        _watchInteractionsTask = Task.Run(async () =>
        {
            var interactions = DashboardClient.SubscribeInteractionsAsync(_cts.Token);
            await foreach (var item in interactions)
            {
                await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
                try
                {
                    if (item.Complete == null)
                    {
                        // New or updated interaction.
                        _pendingInteractions.Remove(item.InteractionId);
                        _pendingInteractions.Add(item);

                        NotifyInteractionAvailable();
                    }
                    else
                    {
                        // Complete interaction.
                        _pendingInteractions.Remove(item.InteractionId);

                        // Close the interaction's dialog if it is open.
                        if (_interactionDialogInstance != null && _interactionDialogReference != null)
                        {
                            if (_interactionDialogInstance.InteractionId == item.InteractionId)
                            {
                                try
                                {
                                    await InvokeAsync(async () =>
                                    {
                                        await _interactionDialogReference.CloseAsync();
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogDebug(ex, "Unexpected error when closing interaction {InteractionId} dialog reference.", item.InteractionId);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        });
    }

    public async Task<IDialogReference> ShowMessageBoxAsync(IDialogService dialogService, DialogParameters<MessageBoxContent> parameters)
    {
        var dialogParameters = new DialogParameters
        {
            DialogType = DialogType.MessageBox,
            Alignment = HorizontalAlignment.Center,
            Title = parameters.Content.Title,
            ShowDismiss = false,
            PrimaryAction = parameters.PrimaryAction,
            SecondaryAction = parameters.SecondaryAction,
            Width = parameters.Width,
            Height = parameters.Height,
            AriaLabel = (parameters.Content.Title ?? ""),
            OnDialogResult = parameters.OnDialogResult
        };
        return await dialogService.ShowDialogAsync(typeof(MessageBox), parameters.Content, dialogParameters);
    }

    private void NotifyInteractionAvailable()
    {
        // Let current waiters know that an interaction is available.
        _interactionAvailableTcs.TrySetResult();

        // Reset the task completion source for future waiters.
        _interactionAvailableTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        await TaskHelpers.WaitIgnoreCancelAsync(_interactionsDisplayTask);
        await TaskHelpers.WaitIgnoreCancelAsync(_watchInteractionsTask);
    }

    private class KeyedInteractionCollection : KeyedCollection<int, WatchInteractionsResponseUpdate>
    {
        protected override int GetKeyForItem(WatchInteractionsResponseUpdate item)
        {
            return item.InteractionId;
        }
    }
}
