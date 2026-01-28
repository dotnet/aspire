// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Interaction;

internal class ConsoleInteractionService : IInteractionService
{
    private static readonly Style s_exitCodeMessageStyle = new Style(foreground: Color.RoyalBlue1, background: null, decoration: Decoration.None);
    private static readonly Style s_infoMessageStyle = new Style(foreground: Color.Green, background: null, decoration: Decoration.None);
    private static readonly Style s_waitingMessageStyle = new Style(foreground: Color.Yellow, background: null, decoration: Decoration.None);
    private static readonly Style s_errorMessageStyle = new Style(foreground: Color.Red, background: null, decoration: Decoration.Bold);
    private static readonly Style s_searchHighlightStyle = new Style(foreground: Color.Black, background: Color.Cyan1, decoration: Decoration.None);

    private readonly IAnsiConsole _outConsole;
    private readonly IAnsiConsole _errorConsole;
    private readonly CliExecutionContext _executionContext;
    private readonly ICliHostEnvironment _hostEnvironment;
    private int _inStatus;

    public ConsoleInteractionService(ConsoleEnvironment consoleEnvironment, CliExecutionContext executionContext, ICliHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(consoleEnvironment);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        _outConsole = consoleEnvironment.Out;
        _errorConsole = consoleEnvironment.Error;
        _executionContext = executionContext;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        // Use atomic check-and-set to prevent nested Spectre.Console Status operations.
        // Spectre.Console throws if multiple interactive operations run concurrently.
        // If already in a status, or in debug/non-interactive mode, fall back to subtle message.
        // Also skip status display if statusText is empty (e.g., when outputting JSON)
        if (Interlocked.CompareExchange(ref _inStatus, 1, 0) != 0 ||
            _executionContext.DebugMode ||
            !_hostEnvironment.SupportsInteractiveOutput ||
            string.IsNullOrEmpty(statusText))
        {
            // Skip displaying if status text is empty (e.g., when outputting JSON)
            if (!string.IsNullOrEmpty(statusText))
            {
                DisplaySubtleMessage(statusText);
            }
            return await action();
        }

        try
        {
            return await _outConsole.Status()
                .Spinner(Spinner.Known.Dots3)
                .StartAsync(statusText, (context) => action());
        }
        finally
        {
            Interlocked.Exchange(ref _inStatus, 0);
        }
    }

    public void ShowStatus(string statusText, Action action)
    {
        // Use atomic check-and-set to prevent nested Spectre.Console Status operations.
        // Spectre.Console throws if multiple interactive operations run concurrently.
        // If already in a status, or in debug/non-interactive mode, fall back to subtle message.
        // Also skip status display if statusText is empty (e.g., when outputting JSON)
        if (Interlocked.CompareExchange(ref _inStatus, 1, 0) != 0 ||
            _executionContext.DebugMode ||
            !_hostEnvironment.SupportsInteractiveOutput ||
            string.IsNullOrEmpty(statusText))
        {
            if (!string.IsNullOrEmpty(statusText))
            {
                DisplaySubtleMessage(statusText);
            }
            action();
            return;
        }

        try
        {
            _outConsole.Status()
                .Spinner(Spinner.Known.Dots3)
                .Start(statusText, (context) => action());
        }
        finally
        {
            Interlocked.Exchange(ref _inStatus, 0);
        }
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));

        if (!_hostEnvironment.SupportsInteractiveInput)
        {
            throw new InvalidOperationException(InteractionServiceStrings.InteractiveInputNotSupported);
        }

        var prompt = new TextPrompt<string>(promptText)
        {
            IsSecret = isSecret,
            AllowEmpty = !required
        };

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue);
            prompt.ShowDefaultValue();
            prompt.DefaultValueStyle(new Style(Color.Fuchsia));
        }

        if (validator is not null)
        {
            prompt.Validate(validator);
        }

        return await _outConsole.PromptAsync(prompt, cancellationToken);
    }

    public async Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        ArgumentNullException.ThrowIfNull(choices, nameof(choices));
        ArgumentNullException.ThrowIfNull(choiceFormatter, nameof(choiceFormatter));

        if (!_hostEnvironment.SupportsInteractiveInput)
        {
            throw new InvalidOperationException(InteractionServiceStrings.InteractiveInputNotSupported);
        }

        // Check if the choices collection is empty to avoid throwing an InvalidOperationException
        if (!choices.Any())
        {
            throw new EmptyChoicesException(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.NoItemsAvailableForSelection, promptText));
        }

        var prompt = new SelectionPrompt<T>()
            .Title(promptText)
            .UseConverter(choiceFormatter)
            .AddChoices(choices)
            .PageSize(10)
            .EnableSearch();
        
        prompt.SearchHighlightStyle = s_searchHighlightStyle;

        return await _outConsole.PromptAsync(prompt, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> PromptForSelectionsAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        ArgumentNullException.ThrowIfNull(choices, nameof(choices));
        ArgumentNullException.ThrowIfNull(choiceFormatter, nameof(choiceFormatter));

        if (!_hostEnvironment.SupportsInteractiveInput)
        {
            throw new InvalidOperationException(InteractionServiceStrings.InteractiveInputNotSupported);
        }

        // Check if the choices collection is empty to avoid throwing an InvalidOperationException
        if (!choices.Any())
        {
            throw new EmptyChoicesException(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.NoItemsAvailableForSelection, promptText));
        }

        var prompt = new MultiSelectionPrompt<T>()
            .Title(promptText)
            .UseConverter(choiceFormatter)
            .AddChoices(choices)
            .PageSize(10);

        var result = await _outConsole.PromptAsync(prompt, cancellationToken);
        return result;
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion)
    {
        var cliInformationalVersion = VersionHelper.GetDefaultTemplateVersion();

        DisplayError(InteractionServiceStrings.AppHostNotCompatibleConsiderUpgrading);
        Console.WriteLine();
        _outConsole.MarkupLine(
            $"\t[bold]{InteractionServiceStrings.AspireHostingSDKVersion}[/]: {appHostHostingVersion}");
        _outConsole.MarkupLine($"\t[bold]{InteractionServiceStrings.AspireCLIVersion}[/]: {cliInformationalVersion}");
        _outConsole.MarkupLine($"\t[bold]{InteractionServiceStrings.RequiredCapability}[/]: {ex.RequiredCapability}");
        Console.WriteLine();
        return ExitCodeConstants.AppHostIncompatible;
    }

    public void DisplayError(string errorMessage)
    {
        DisplayMessage("cross_mark", $"[red bold]{errorMessage.EscapeMarkup()}[/]");
    }

    public void DisplayMessage(string emoji, string message)
    {
        // This is a hack to deal with emoji of different size. We write the emoji then move the cursor to aboslute column 4
        // on the same line before writing the message. This ensures that the message starts at the same position regardless
        // of the emoji used. I'm not OCD .. you are!
        _outConsole.Markup($":{emoji}:");
        _outConsole.Write("\u001b[4G");
        _outConsole.MarkupLine(message);
    }

    public void DisplayPlainText(string message)
    {
        // Write directly to avoid Spectre.Console line wrapping
        _outConsole.Profile.Out.Writer.WriteLine(message);
    }

    public void DisplayRawText(string text)
    {
        // Write raw text directly to avoid console wrapping
        _outConsole.Profile.Out.Writer.WriteLine(text);
    }

    public void DisplayMarkdown(string markdown)
    {
        var spectreMarkup = MarkdownToSpectreConverter.ConvertToSpectre(markdown);
        _outConsole.MarkupLine(spectreMarkup);
    }

    public void DisplayMarkupLine(string markup)
    {
        _outConsole.MarkupLine(markup);
    }

    public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false)
    {
        var style = isErrorMessage ? s_errorMessageStyle
            : type switch
            {
                "waiting" => s_waitingMessageStyle,
                "running" => s_infoMessageStyle,
                "exitCode" => s_exitCodeMessageStyle,
                "failedToStart" => s_errorMessageStyle,
                _ => s_infoMessageStyle
            };

        var prefix = lineNumber.HasValue ? $"#{lineNumber.Value}: " : "";
        _outConsole.WriteLine($"{prefix}{message}", style);
    }

    public void DisplaySuccess(string message)
    {
        DisplayMessage("check_mark", message);
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
        foreach (var (stream, line) in lines)
        {
            if (stream == "stdout")
            {
                _outConsole.MarkupLineInterpolated($"{line.EscapeMarkup()}");
            }
            else
            {
                _outConsole.MarkupLineInterpolated($"[red]{line.EscapeMarkup()}[/]");
            }
        }
    }

    public void DisplayCancellationMessage()
    {
        _outConsole.WriteLine();
        DisplayMessage("stop_sign", $"[teal bold]{InteractionServiceStrings.StoppingAspire}[/]");
    }

    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        if (!_hostEnvironment.SupportsInteractiveInput)
        {
            throw new InvalidOperationException(InteractionServiceStrings.InteractiveInputNotSupported);
        }

        return _outConsole.ConfirmAsync(promptText, defaultValue, cancellationToken);
    }

    public void DisplaySubtleMessage(string message, bool escapeMarkup = true)
    {
        var displayMessage = escapeMarkup ? message.EscapeMarkup() : message;
        _outConsole.MarkupLine($"[dim]{displayMessage}[/]");
    }

    public void DisplayEmptyLine()
    {
        _outConsole.WriteLine();
    }

    private const string UpdateUrl = "https://aka.ms/aspire/update";

    public void DisplayVersionUpdateNotification(string newerVersion, string? updateCommand = null)
    {
        // Write to stderr to avoid corrupting stdout when JSON output is used
        _errorConsole.WriteLine();
        _errorConsole.MarkupLine(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.NewCliVersionAvailable, newerVersion));
        
        if (!string.IsNullOrEmpty(updateCommand))
        {
            _errorConsole.MarkupLine(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ToUpdateRunCommand, updateCommand));
        }
        
        _errorConsole.MarkupLine(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.MoreInfoNewCliVersion, UpdateUrl));
    }
}
