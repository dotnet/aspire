// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Spectre.Console;
using Xunit;

namespace Aspire.Cli.Tests.Interaction;

public class ConsoleInteractionServiceTests
{
    [Fact]
    public async Task PromptForSelectionAsync_EmptyChoices_ThrowsEmptyChoicesException()
    {
        // Arrange
        var interactionService = new ConsoleInteractionService(AnsiConsole.Console);
        var choices = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<EmptyChoicesException>(() => 
            interactionService.PromptForSelectionAsync("Select an item:", choices, x => x, CancellationToken.None));
    }

    [Fact]
    public void DisplaySuccess_CallsDisplayMessageWithCheckMark()
    {
        // This test verifies that DisplaySuccess calls DisplayMessage with "check_mark"
        // We'll use reflection to test the private DisplayMessage method calls
        var interactionService = new TestableConsoleInteractionService();
        
        // Act
        interactionService.DisplaySuccess("Test message");
        
        // Assert
        Assert.Equal("check_mark", interactionService.LastEmojiUsed);
        Assert.Equal("Test message", interactionService.LastMessageUsed);
    }

    [Fact]
    public void DisplayError_CallsDisplayMessageWithCrossMark()
    {
        // This test verifies that DisplayError calls DisplayMessage with "cross_mark"
        var interactionService = new TestableConsoleInteractionService();
        
        // Act
        interactionService.DisplayError("Test error");
        
        // Assert
        Assert.Equal("cross_mark", interactionService.LastEmojiUsed);
        Assert.Contains("Test error", interactionService.LastMessageUsed);
    }

    // Test-specific implementation to capture the emoji and message parameters
    private sealed class TestableConsoleInteractionService : ConsoleInteractionService
    {
        public string? LastEmojiUsed { get; private set; }
        public string? LastMessageUsed { get; private set; }

        public TestableConsoleInteractionService() : base(AnsiConsole.Console)
        {
        }

        public new void DisplayMessage(string emoji, string message)
        {
            LastEmojiUsed = emoji;
            LastMessageUsed = message;
            // Don't call base to avoid actual console output during tests
        }
    }
}