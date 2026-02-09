// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Spectre.Console;
using System.Text;

namespace Aspire.Cli.Tests.Interaction;

public class ConsoleInteractionServiceTests
{
    private static ConsoleInteractionService CreateInteractionService(IAnsiConsole console, CliExecutionContext executionContext, ICliHostEnvironment? hostEnvironment = null)
    {
        var consoleEnvironment = new ConsoleEnvironment(console, console);
        return new ConsoleInteractionService(consoleEnvironment, executionContext, hostEnvironment ?? TestHelpers.CreateInteractiveHostEnvironment());
    }

    [Fact]
    public async Task PromptForSelectionAsync_EmptyChoices_ThrowsEmptyChoicesException()
    {
        // Arrange
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(AnsiConsole.Console, executionContext);
        var choices = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<EmptyChoicesException>(() =>
            interactionService.PromptForSelectionAsync("Select an item:", choices, x => x, CancellationToken.None));
    }

    [Fact]
    public async Task PromptForSelectionsAsync_EmptyChoices_ThrowsEmptyChoicesException()
    {
        // Arrange
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(AnsiConsole.Console, executionContext);
        var choices = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<EmptyChoicesException>(() =>
            interactionService.PromptForSelectionsAsync("Select items:", choices, x => x, CancellationToken.None));
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
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);
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
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);
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
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);
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
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);
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
        
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);
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

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log", debugMode: true);
        var interactionService = CreateInteractionService(console, executionContext);
        var statusText = "Processing request...";
        var result = "test result";

        // Act
        var actualResult = await interactionService.ShowStatusAsync(statusText, () => Task.FromResult(result)).DefaultTimeout();

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

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log", debugMode: true);
        var interactionService = CreateInteractionService(console, executionContext);
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

    [Fact]
    public async Task PromptForStringAsync_WhenInteractiveInputNotSupported_ThrowsInvalidOperationException()
    {
        // Arrange
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var hostEnvironment = TestHelpers.CreateNonInteractiveHostEnvironment();
        var interactionService = CreateInteractionService(AnsiConsole.Console, executionContext, hostEnvironment);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            interactionService.PromptForStringAsync("Enter value:", null, null, false, false, CancellationToken.None));
        Assert.Contains(InteractionServiceStrings.InteractiveInputNotSupported, exception.Message);
    }

    [Fact]
    public async Task PromptForSelectionAsync_WhenInteractiveInputNotSupported_ThrowsInvalidOperationException()
    {
        // Arrange
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var hostEnvironment = TestHelpers.CreateNonInteractiveHostEnvironment();
        var interactionService = CreateInteractionService(AnsiConsole.Console, executionContext, hostEnvironment);
        var choices = new[] { "option1", "option2" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            interactionService.PromptForSelectionAsync("Select an item:", choices, x => x, CancellationToken.None));
        Assert.Contains(InteractionServiceStrings.InteractiveInputNotSupported, exception.Message);
    }

    [Fact]
    public async Task PromptForSelectionsAsync_WhenInteractiveInputNotSupported_ThrowsInvalidOperationException()
    {
        // Arrange
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var hostEnvironment = TestHelpers.CreateNonInteractiveHostEnvironment();
        var interactionService = CreateInteractionService(AnsiConsole.Console, executionContext, hostEnvironment);
        var choices = new[] { "option1", "option2" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            interactionService.PromptForSelectionsAsync("Select items:", choices, x => x, CancellationToken.None));
        Assert.Contains(InteractionServiceStrings.InteractiveInputNotSupported, exception.Message);
    }

    [Fact]
    public async Task ConfirmAsync_WhenInteractiveInputNotSupported_ThrowsInvalidOperationException()
    {
        // Arrange
        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var hostEnvironment = TestHelpers.CreateNonInteractiveHostEnvironment();
        var interactionService = CreateInteractionService(AnsiConsole.Console, executionContext, hostEnvironment);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            interactionService.ConfirmAsync("Confirm?", true, CancellationToken.None));
        Assert.Contains(InteractionServiceStrings.InteractiveInputNotSupported, exception.Message);
    }

    [Fact]
    public async Task ShowStatusAsync_NestedCall_DoesNotThrowException()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);
        
        var outerStatusText = "Outer operation...";
        var innerStatusText = "Inner operation...";
        var expectedResult = 42;

        // Act
        var actualResult = await interactionService.ShowStatusAsync(outerStatusText, async () =>
        {
            // This nested call should not throw - it should fall back to DisplaySubtleMessage
            return await interactionService.ShowStatusAsync(innerStatusText, () => Task.FromResult(expectedResult));
        });

        // Assert
        Assert.Equal(expectedResult, actualResult);
        var outputString = output.ToString();
        Assert.Contains(outerStatusText, outputString);
        Assert.Contains(innerStatusText, outputString);
    }

    [Fact]
    public void ShowStatus_NestedCall_DoesNotThrowException()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);
        
        var outerStatusText = "Outer synchronous operation...";
        var innerStatusText = "Inner synchronous operation...";
        var actionExecuted = false;

        // Act
        interactionService.ShowStatus(outerStatusText, () =>
        {
            // This nested call should not throw - it should fall back to DisplaySubtleMessage
            interactionService.ShowStatus(innerStatusText, () => actionExecuted = true);
        });

        // Assert
        Assert.True(actionExecuted);
        var outputString = output.ToString();
        Assert.Contains(outerStatusText, outputString);
        Assert.Contains(innerStatusText, outputString);
    }

    [Fact]
    public void DisplayIncompatibleVersionError_WithMarkupCharactersInVersion_DoesNotThrow()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);

        var ex = new AppHostIncompatibleException("Incompatible [version]", "capability [Prod]");

        // Act - should not throw due to unescaped markup characters
        var exception = Record.Exception(() => interactionService.DisplayIncompatibleVersionError(ex, "9.0.0-preview.1 [rc]"));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        Assert.Contains("capability [Prod]", outputString);
        Assert.Contains("9.0.0-preview.1 [rc]", outputString);
    }

    [Fact]
    public void DisplayMessage_WithMarkupCharactersInMessage_DoesNotThrow()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);

        // DisplayMessage passes its message directly to MarkupLine.
        // Callers that embed external data must escape it first.
        var message = "See logs at C:\\Users\\test [Dev]\\logs\\aspire.log";

        // Act - should not throw due to unescaped markup characters
        var exception = Record.Exception(() => interactionService.DisplayMessage("page_facing_up", message.EscapeMarkup()));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        Assert.Contains("C:\\Users\\test [Dev]\\logs\\aspire.log", outputString);
    }

    [Fact]
    public void DisplayVersionUpdateNotification_WithMarkupCharactersInVersion_DoesNotThrow()
    {
        // Arrange
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var executionContext = new CliExecutionContext(new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo("."), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")), new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-logs")), "test.log");
        var interactionService = CreateInteractionService(console, executionContext);

        // Version strings are unlikely to have brackets, but the method should handle it
        var version = "13.2.0-preview [beta]";
        var updateCommand = "aspire update --channel [stable]";

        // Act - should not throw due to unescaped markup characters
        var exception = Record.Exception(() => interactionService.DisplayVersionUpdateNotification(version, updateCommand));

        // Assert
        Assert.Null(exception);
        var outputString = output.ToString();
        Assert.Contains("13.2.0-preview [beta]", outputString);
        Assert.Contains("aspire update --channel [stable]", outputString);
    }
}
