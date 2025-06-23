// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Hosting.Tests;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class InteractionServiceTests
{
    [Fact]
    public async Task PromptConfirmationAsync_CompleteResult_ReturnResult()
    {
        // Arrange
        var interactionService = CreateInteractionService();

        // Act 1
        var resultTask = interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation");

        // Assert 1
        var interaction = Assert.Single(interactionService.GetCurrentInteractions());
        Assert.False(interaction.CompletionTcs.Task.IsCompleted);
        Assert.Equal(Interaction.InteractionState.InProgress, interaction.State);

        // Act 2
        interactionService.CompleteInteraction(interaction.InteractionId, _ => new InteractionCompletionState { State = true });

        var result = await resultTask.DefaultTimeout();
        Assert.True(result.Data!);

        // Assert 2
        Assert.Empty(interactionService.GetCurrentInteractions());
    }

    [Fact]
    public async Task PromptConfirmationAsync_Cancellation_ReturnResult()
    {
        // Arrange
        var interactionService = CreateInteractionService();

        // Act 1
        var cts = new CancellationTokenSource();
        var resultTask = interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation", cancellationToken: cts.Token);

        // Assert 1
        var interaction = Assert.Single(interactionService.GetCurrentInteractions());
        Assert.False(interaction.CompletionTcs.Task.IsCompleted);
        Assert.Equal(Interaction.InteractionState.InProgress, interaction.State);

        // Act 2
        cts.Cancel();

        var result = await resultTask.DefaultTimeout();
        Assert.True(result.Canceled);

        // Assert 2
        Assert.Empty(interactionService.GetCurrentInteractions());
    }

    [Fact]
    public async Task PromptConfirmationAsync_MultipleCompleteResult_ReturnResult()
    {
        // Arrange
        var interactionService = CreateInteractionService();

        // Act 1
        var resultTask1 = interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation");
        var resultTask2 = interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation");

        // Assert 1
        int? id1 = null;
        int? id2 = null;
        Assert.Collection(interactionService.GetCurrentInteractions(),
            interaction =>
            {
                id1 = interaction.InteractionId;
            },
            interaction =>
            {
                id2 = interaction.InteractionId;
            });
        Assert.True(id1.HasValue && id2.HasValue && id1 < id2);

        // Act & Assert 2
        interactionService.CompleteInteraction(id1.Value, _ => new InteractionCompletionState { State = true });
        Assert.True((bool)(await resultTask1.DefaultTimeout()).Data!);
        Assert.Equal(id2.Value, Assert.Single(interactionService.GetCurrentInteractions()).InteractionId);

        interactionService.CompleteInteraction(id2.Value, _ => new InteractionCompletionState { State = false });
        Assert.False((bool)(await resultTask2.DefaultTimeout()).Data!);
        Assert.Empty(interactionService.GetCurrentInteractions());
    }

    [Fact]
    public async Task SubscribeInteractionUpdates_MultipleCompleteResult_ReturnResult()
    {
        // Arrange
        var interactionService = CreateInteractionService();
        var subscription = interactionService.SubscribeInteractionUpdates();
        var updates = Channel.CreateUnbounded<Interaction>();
        var readTask = Task.Run(async () =>
        {
            await foreach (var interaction in subscription.WithCancellation(CancellationToken.None))
            {
                await updates.Writer.WriteAsync(interaction);
            }
        });

        // Act 1
        var resultTask1 = interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation");
        var interaction1 = Assert.Single(interactionService.GetCurrentInteractions());
        Assert.Equal(interaction1.InteractionId, (await updates.Reader.ReadAsync().DefaultTimeout()).InteractionId);

        var resultTask2 = interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation");
        Assert.Equal(2, interactionService.GetCurrentInteractions().Count);
        var interaction2 = interactionService.GetCurrentInteractions()[1];
        Assert.Equal(interaction2.InteractionId, (await updates.Reader.ReadAsync().DefaultTimeout()).InteractionId);

        // Act & Assert 2
        var result1 = new InteractionCompletionState { State = true };
        interactionService.CompleteInteraction(interaction1.InteractionId, _ => result1);
        Assert.True((await resultTask1.DefaultTimeout()).Data);
        Assert.Equal(interaction2.InteractionId, Assert.Single(interactionService.GetCurrentInteractions()).InteractionId);
        var completedInteraction1 = await updates.Reader.ReadAsync().DefaultTimeout();
        Assert.True(completedInteraction1.CompletionTcs.Task.IsCompletedSuccessfully);
        Assert.Equivalent(result1, await completedInteraction1.CompletionTcs.Task.DefaultTimeout());

        var result2 = new InteractionCompletionState { State = false };
        interactionService.CompleteInteraction(interaction2.InteractionId, _ => result2);
        Assert.False((await resultTask2.DefaultTimeout()).Data);
        Assert.Empty(interactionService.GetCurrentInteractions());
        var completedInteraction2 = await updates.Reader.ReadAsync().DefaultTimeout();
        Assert.True(completedInteraction2.CompletionTcs.Task.IsCompletedSuccessfully);
        Assert.Equivalent(result2, await completedInteraction2.CompletionTcs.Task.DefaultTimeout());
    }

    [Fact]
    public async Task PublicApis_DashboardDisabled_ThrowErrors()
    {
        // Arrange
        var interactionService = CreateInteractionService(options: new DistributedApplicationOptions { DisableDashboard = true });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation")).DefaultTimeout();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => interactionService.PromptMessageBarAsync("Are you sure?", "Confirmation")).DefaultTimeout();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => interactionService.PromptMessageBoxAsync("Are you sure?", "Confirmation")).DefaultTimeout();
    }

    private static InteractionService CreateInteractionService(DistributedApplicationOptions? options = null)
    {
        return new InteractionService(NullLogger<InteractionService>.Instance, options ?? new DistributedApplicationOptions());
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
