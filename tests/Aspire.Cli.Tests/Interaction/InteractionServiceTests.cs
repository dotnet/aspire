// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Spectre.Console;
using Xunit;

namespace Aspire.Cli.Tests.Interaction;

public class InteractionServiceTests
{
    [Fact]
    public async Task PromptForSelectionAsync_EmptyChoices_ThrowsEmptyChoicesException()
    {
        // Arrange
        var settings = new AnsiConsoleSettings()
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            ColorSystem = ColorSystemSupport.Standard,
            Out = new AnsiConsoleOutput(new StringWriter())
        };
        var ansiConsole = AnsiConsole.Create(settings);
        var interactionService = new InteractionService(ansiConsole);
        var choices = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<EmptyChoicesException>(() => 
            interactionService.PromptForSelectionAsync("Select an item:", choices, x => x, CancellationToken.None));
    }
}