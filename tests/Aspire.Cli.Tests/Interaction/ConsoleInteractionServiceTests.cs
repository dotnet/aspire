// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Spectre.Console;
using System.Text;

namespace Aspire.Cli.Tests.Interaction;

public class ConsoleInteractionServiceTests
{
    [Fact]
    public async Task PromptForSelectionAsync_EmptyChoices_ThrowsEmptyChoicesException()
    {
        // Arrange
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."));
        var interactionService = new ConsoleInteractionService(AnsiConsole.Console, executionContext);
        var choices = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<EmptyChoicesException>(() => 
            interactionService.PromptForSelectionAsync("Select an item:", choices, x => x, CancellationToken.None));
    }

    [Fact]
    public void DisplayError_WithMarkupCharacters_DoesNotCauseMarkupParsingError()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."));
        var interactionService = new ConsoleInteractionService(console, executionContext);
        var errorMessage = "The JSON value could not be converted to <Type>. Path: $.values[0].Type | LineNumber: 0 | BytePositionInLine: 121.";

        // Act - this should not throw an exception due to markup parsing
        var exception = Record.Exception(() => interactionService.DisplayError(errorMessage));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        Assert.Contains("The JSON value could not be converted to", outputString);
    }

    [Fact]
    public void DisplaySubtleMessage_WithMarkupCharacters_DoesNotCauseMarkupParsingError()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."));
        var interactionService = new ConsoleInteractionService(console, executionContext);
        var message = "Path with <brackets> and [markup] characters";

        // Act - this should not throw an exception due to markup parsing
        var exception = Record.Exception(() => interactionService.DisplaySubtleMessage(message));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        Assert.Contains("Path with <brackets> and [markup] characters", outputString);
    }

    [Fact]
    public void DisplayLines_WithMarkupCharacters_DoesNotCauseMarkupParsingError()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."));
        var interactionService = new ConsoleInteractionService(console, executionContext);
        var lines = new[] 
        {
            ("stdout", "Command output with <angle> brackets"),
            ("stderr", "Error output with [square] brackets")
        };

        // Act - this should not throw an exception due to markup parsing
        var exception = Record.Exception(() => interactionService.DisplayLines(lines));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        Assert.Contains("Command output with <angle> brackets", outputString);
        // Square brackets get escaped to [[square]] when using EscapeMarkup()
        Assert.Contains("Error output with [[square]] brackets", outputString);
    }

    [Fact]
    public void DisplayMarkdown_WithBasicMarkdown_ConvertsToSpectreMarkup()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."));
        var interactionService = new ConsoleInteractionService(console, executionContext);
        var markdown = "# Header\nThis is **bold** and *italic* text with `code`.";

        // Act
        var exception = Record.Exception(() => interactionService.DisplayMarkdown(markdown));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        // Should contain converted markup, but due to Ansi = No, the actual markup tags won't appear in output
        // Just verify it doesn't throw and produces some output
        Assert.NotEmpty(outputString.Trim());
    }

    [Fact]
    public void DisplayMarkdown_WithPlainText_DoesNotThrow()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."));
        var interactionService = new ConsoleInteractionService(console, executionContext);
        var plainText = "This is just plain text without any markdown.";

        // Act
        var exception = Record.Exception(() => interactionService.DisplayMarkdown(plainText));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        Assert.Contains("This is just plain text without any markdown.", outputString);
    }

    [Fact]
    public async Task ShowStatusAsync_InDebugMode_DisplaysSubtleMessageInsteadOfSpinner()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), debugMode: true);
        var interactionService = new ConsoleInteractionService(console, executionContext);
        var statusText = "Processing request...";
        var result = "test result";

        // Act
        var actualResult = await interactionService.ShowStatusAsync(statusText, () => Task.FromResult(result));

        // Assert
        Assert.Equal(result, actualResult);
        var outputString = output.ToString();
        Assert.Contains(statusText, outputString);
        // In debug mode, should use DisplaySubtleMessage instead of spinner
    }

    [Fact]
    public void ShowStatus_InDebugMode_DisplaysSubtleMessageInsteadOfSpinner()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), debugMode: true);
        var interactionService = new ConsoleInteractionService(console, executionContext);
        var statusText = "Processing synchronous request...";
        var actionCalled = false;

        // Act
        interactionService.ShowStatus(statusText, () => actionCalled = true);

        // Assert
        Assert.True(actionCalled);
        var outputString = output.ToString();
        Assert.Contains(statusText, outputString);
        // In debug mode, should use DisplaySubtleMessage instead of spinner
    }
}