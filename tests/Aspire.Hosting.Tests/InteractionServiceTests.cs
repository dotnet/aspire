// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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
        await CompleteInteractionAsync(interactionService, interaction.InteractionId, new InteractionCompletionState { Complete = true, State = true });

        var result = await resultTask.DefaultTimeout();
        Assert.True(result.Data);

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
        var resultTask3 = interactionService.PromptConfirmationAsync("Are you sure?", "Confirmation");

        // Assert 1
        int? id1 = null;
        int? id2 = null;
        int? id3 = null;
        Assert.Collection(interactionService.GetCurrentInteractions(),
            interaction =>
            {
                id1 = interaction.InteractionId;
            },
            interaction =>
            {
                id2 = interaction.InteractionId;
            },
            interaction =>
            {
                id3 = interaction.InteractionId;
            });
        Assert.True(id1.HasValue && id2.HasValue && id3.HasValue && id1 < id2 && id2 < id3);

        // Act & Assert 2
        await CompleteInteractionAsync(interactionService, id1.Value, new InteractionCompletionState { Complete = true, State = true });
        var result1 = await resultTask1.DefaultTimeout();
        Assert.True(result1.Data);
        Assert.False(result1.Canceled);
        Assert.Collection(interactionService.GetCurrentInteractions(),
            interaction => Assert.Equal(interaction.InteractionId, id2),
            interaction => Assert.Equal(interaction.InteractionId, id3));

        await CompleteInteractionAsync(interactionService, id2.Value, new InteractionCompletionState { Complete = true, State = false });
        var result2 = await resultTask2.DefaultTimeout();
        Assert.False(result2.Data);
        Assert.False(result1.Canceled);
        Assert.Equal(id3.Value, Assert.Single(interactionService.GetCurrentInteractions()).InteractionId);

        await CompleteInteractionAsync(interactionService, id3.Value, new InteractionCompletionState { Complete = true });
        var result3 = await resultTask3.DefaultTimeout();
        Assert.True(result3.Canceled);
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
        var result1 = new InteractionCompletionState { Complete = true, State = true };
        await CompleteInteractionAsync(interactionService, interaction1.InteractionId, result1);
        Assert.True((await resultTask1.DefaultTimeout()).Data);
        Assert.Equal(interaction2.InteractionId, Assert.Single(interactionService.GetCurrentInteractions()).InteractionId);
        var completedInteraction1 = await updates.Reader.ReadAsync().DefaultTimeout();
        Assert.True(completedInteraction1.CompletionTcs.Task.IsCompletedSuccessfully);
        Assert.Equivalent(result1, await completedInteraction1.CompletionTcs.Task.DefaultTimeout());

        var result2 = new InteractionCompletionState { Complete = true, State = false };
        await CompleteInteractionAsync(interactionService, interaction2.InteractionId, result2);
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

    [Fact]
    public async Task PromptInputAsync_InvalidData()
    {
        var interactionService = CreateInteractionService();

        var input = new InteractionInput { Label = "Value", InputType = InputType.Text, };
        var resultTask = interactionService.PromptInputAsync(
            "Please provide", "please",
            input,
            new InputsDialogInteractionOptions
            {
                ValidationCallback = context =>
                {
                    // everything is invalid
                    context.AddValidationError(input, "Invalid value");
                    return Task.CompletedTask;
                }
            });

        var interaction = Assert.Single(interactionService.GetCurrentInteractions());
        Assert.False(interaction.CompletionTcs.Task.IsCompleted);
        Assert.Equal(Interaction.InteractionState.InProgress, interaction.State);

        await CompleteInteractionAsync(interactionService, interaction.InteractionId, new InteractionCompletionState { Complete = true, State = new [] { input } });

        // The interaction should still be in progress due to validation error
        Assert.False(interaction.CompletionTcs.Task.IsCompleted);
    }

    [Fact]
    public void InteractionInput_WithDescription_SetsProperties()
    {
        // Arrange & Act
        var input = new InteractionInput
        {
            Label = "Test Label",
            InputType = InputType.Text,
            Description = "Test description",
            EnableDescriptionMarkdown = false
        };

        // Assert
        Assert.Equal("Test Label", input.Label);
        Assert.Equal(InputType.Text, input.InputType);
        Assert.Equal("Test description", input.Description);
        Assert.False(input.EnableDescriptionMarkdown);
    }

    [Fact]
    public void InteractionInput_WithMarkdownDescription_SetsMarkupFlag()
    {
        // Arrange & Act
        var input = new InteractionInput
        {
            Label = "Test Label",
            InputType = InputType.Text,
            Description = "**Bold** description",
            EnableDescriptionMarkdown = true
        };

        // Assert
        Assert.Equal("**Bold** description", input.Description);
        Assert.True(input.EnableDescriptionMarkdown);
    }

    [Fact]
    public void InteractionInput_WithNullDescription_AllowsNullValue()
    {
        // Arrange & Act
        var input = new InteractionInput
        {
            Label = "Test Label",
            InputType = InputType.Text,
            Description = null,
            EnableDescriptionMarkdown = false
        };

        // Assert
        Assert.Null(input.Description);
        Assert.False(input.EnableDescriptionMarkdown);
    }

    private static async Task CompleteInteractionAsync(InteractionService interactionService, int interactionId, InteractionCompletionState state)
    {
        await interactionService.CompleteInteractionAsync(interactionId, (_, _) => state, CancellationToken.None);
    }

    private static InteractionService CreateInteractionService(DistributedApplicationOptions? options = null)
    {
        return new InteractionService(
            NullLogger<InteractionService>.Instance,
            options ?? new DistributedApplicationOptions(),
            new ServiceCollection().BuildServiceProvider());
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
