// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Aspire.ResourceService.Proto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Color = Microsoft.FluentUI.AspNetCore.Components.Color;

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

                    if (item.MessageBox is { } messageBox)
                    {
                        var dialogParameters = CreateDialogParameters(item);
                        dialogParameters.OnDialogResult = EventCallback.Factory.Create<DialogResult>(this, async dialogResult =>
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
                                request.MessageBox = item.MessageBox;
                            }

                            await DashboardClient.SendInteractionRequestAsync(request, _cts.Token).ConfigureAwait(false);
                        });

                        var content = new MessageBoxContent
                        {
                            Title = item.Title,
                            MarkupMessage = new MarkupString(item.Message),
                        };
                        switch (messageBox.Icon)
                        {
                            case MessageBoxIcon.Success:
                                content.IconColor = Color.Success;
                                content.Icon = new Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.Size24.CheckmarkCircle();
                                break;
                            case MessageBoxIcon.Warning:
                                content.IconColor = Color.Warning;
                                content.Icon = new Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.Size24.Warning();
                                break;
                            case MessageBoxIcon.Error:
                                content.IconColor = Color.Error;
                                content.Icon = new Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.Size24.DismissCircle();
                                break;
                            case MessageBoxIcon.Information:
                                content.IconColor = Color.Info;
                                content.Icon = new Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.Size24.Info();
                                break;
                            case MessageBoxIcon.Question:
                                content.IconColor = Color.Success;
                                content.Icon = new Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.Size24.QuestionCircle();
                                break;
                        }

                        openDialog = dialogService => ShowMessageBoxAsync(dialogService, content, dialogParameters);
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

    private static DialogParameters CreateDialogParameters(WatchInteractionsResponseUpdate interaction)
    {
        var dialogParameters = new DialogParameters
        {
            ShowDismiss = interaction.ShowDismiss,
            PrimaryAction = ResolvedPrimaryButtonText(interaction),
            SecondaryAction = ResolvedSecondaryButtonText(interaction),
            PreventDismissOnOverlayClick = true,
            Title = interaction.Title
        };

        return dialogParameters;
    }

    private static string ResolvedSecondaryButtonText(WatchInteractionsResponseUpdate interaction)
    {
        if (!interaction.ShowSecondaryButton)
        {
            return string.Empty;
        }

        return interaction.SecondaryButtonText is { Length: > 0 } secondaryText ? secondaryText : "Cancel";
    }

    private static string ResolvedPrimaryButtonText(WatchInteractionsResponseUpdate interaction)
    {
        return interaction.PrimaryButtonText is { Length: > 0 } primaryText ? primaryText : "OK";
    }

    public async Task<IDialogReference> ShowMessageBoxAsync(IDialogService dialogService, MessageBoxContent content, DialogParameters parameters)
    {
        var dialogParameters = new DialogParameters
        {
            DialogType = DialogType.MessageBox,
            Alignment = HorizontalAlignment.Center,
            Title = content.Title,
            ShowDismiss = false,
            PrimaryAction = parameters.PrimaryAction,
            SecondaryAction = parameters.SecondaryAction,
            Width = parameters.Width,
            Height = parameters.Height,
            AriaLabel = (content.Title ?? ""),
            OnDialogResult = parameters.OnDialogResult
        };
        return await dialogService.ShowDialogAsync(typeof(MessageBox), content, dialogParameters);
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
