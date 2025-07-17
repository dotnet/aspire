// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Tests;

public class PublishModeInteractionServiceTests
{
    [Fact]
    public void IsAvailable_DelegatesToInnerService()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Run);

        // Act & Assert
        Assert.Equal(innerService.IsAvailable, publishModeService.IsAvailable);
    }

    [Fact]
    public async Task PromptInputAsync_WithLabel_IsAllowedInPublishMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Publish);

        // Act & Assert - Should not throw
        var inputTask = publishModeService.PromptInputAsync("Test", "Message", "Label", "Placeholder");
        
        // Cancel the task since we don't have a real interaction handler
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => inputTask.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task PromptInputAsync_WithInteractionInput_IsAllowedInPublishMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Publish);
        var input = new InteractionInput { Label = "Test", InputType = InputType.Text };

        // Act & Assert - Should not throw
        var inputTask = publishModeService.PromptInputAsync("Test", "Message", input);
        
        // Cancel the task since we don't have a real interaction handler
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => inputTask.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task PromptInputsAsync_IsAllowedInPublishMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Publish);
        var inputs = new List<InteractionInput>
        {
            new() { Label = "Test1", InputType = InputType.Text },
            new() { Label = "Test2", InputType = InputType.Text }
        };

        // Act & Assert - Should not throw
        var inputTask = publishModeService.PromptInputsAsync("Test", "Message", inputs);
        
        // Cancel the task since we don't have a real interaction handler
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => inputTask.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task PromptConfirmationAsync_ThrowsInPublishMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Publish);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await publishModeService.PromptConfirmationAsync("Test", "Message"));
        
        Assert.Contains("not supported when running in publish mode", exception.Message);
        Assert.Contains("Only PromptInputAsync and PromptInputsAsync methods are supported", exception.Message);
    }

    [Fact]
    public async Task PromptMessageBoxAsync_ThrowsInPublishMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Publish);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await publishModeService.PromptMessageBoxAsync("Test", "Message"));
        
        Assert.Contains("not supported when running in publish mode", exception.Message);
        Assert.Contains("Only PromptInputAsync and PromptInputsAsync methods are supported", exception.Message);
    }

    [Fact]
    public async Task PromptNotificationAsync_ThrowsInPublishMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Publish);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await publishModeService.PromptNotificationAsync("Test", "Message"));
        
        Assert.Contains("not supported when running in publish mode", exception.Message);
        Assert.Contains("Only PromptInputAsync and PromptInputsAsync methods are supported", exception.Message);
    }

    [Fact]
    public async Task PromptConfirmationAsync_IsAllowedInRunMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Run);

        // Act & Assert - Should not throw
        var confirmTask = publishModeService.PromptConfirmationAsync("Test", "Message");
        
        // Cancel the task since we don't have a real interaction handler
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => confirmTask.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task PromptMessageBoxAsync_IsAllowedInRunMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Run);

        // Act & Assert - Should not throw
        var messageTask = publishModeService.PromptMessageBoxAsync("Test", "Message");
        
        // Cancel the task since we don't have a real interaction handler
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => messageTask.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task PromptNotificationAsync_IsAllowedInRunMode()
    {
        // Arrange
        var innerService = CreateInteractionService();
        var publishModeService = CreatePublishModeInteractionService(innerService, DistributedApplicationOperation.Run);

        // Act & Assert - Should not throw
        var notificationTask = publishModeService.PromptNotificationAsync("Test", "Message");
        
        // Cancel the task since we don't have a real interaction handler
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => notificationTask.WaitAsync(cts.Token));
    }

    private static InteractionService CreateInteractionService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<InteractionService>();
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<InteractionService>>();
        return new InteractionService(logger, new DistributedApplicationOptions(), provider);
    }

    private static PublishModeInteractionService CreatePublishModeInteractionService(InteractionService innerService, DistributedApplicationOperation operation)
    {
        var executionContext = new DistributedApplicationExecutionContext(operation);
        return new PublishModeInteractionService(innerService, executionContext);
    }
}