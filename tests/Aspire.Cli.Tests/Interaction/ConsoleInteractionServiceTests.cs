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
}