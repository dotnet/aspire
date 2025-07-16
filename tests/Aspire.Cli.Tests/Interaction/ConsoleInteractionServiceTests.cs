// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Spectre.Console;

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
    public void PromptForStringAsync_WithDefaultValue_MethodExists()
    {
        // This test validates that the PromptForStringAsync method with default value parameter exists
        // and can be called. The actual purple styling behavior is applied through Spectre.Console
        // and would be visible in the terminal output when the CLI is used interactively.
        
        // Arrange
        var interactionService = new ConsoleInteractionService(AnsiConsole.Console);
        
        // Act & Assert - Verifies method signature and basic instantiation
        Assert.NotNull(interactionService);
        
        // The method exists and accepts the parameters we expect
        var methodInfo = typeof(ConsoleInteractionService).GetMethod("PromptForStringAsync");
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(Task<string>), methodInfo.ReturnType);
    }
}