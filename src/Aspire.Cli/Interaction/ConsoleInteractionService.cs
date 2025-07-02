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
    private readonly IAnsiConsole _ansiConsole;

    public ConsoleInteractionService(IAnsiConsole ansiConsole)
    {
        ArgumentNullException.ThrowIfNull(ansiConsole);
        _ansiConsole = ansiConsole;
    }

    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return await _ansiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .StartAsync(statusText, (context) => action());
    }

    public void ShowStatus(string statusText, Action action)
    {
        _ansiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .Start(statusText, (context) => action());
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        var prompt = new TextPrompt<string>(promptText)
        {
            IsSecret = isSecret,
            AllowEmpty = !required
        };

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue);
            prompt.ShowDefaultValue();
        }

        if (validator is not null)
        {
            prompt.Validate(validator);
        }

        return await _ansiConsole.PromptAsync(prompt, cancellationToken);
    }

    public async Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        ArgumentNullException.ThrowIfNull(choices, nameof(choices));
        ArgumentNullException.ThrowIfNull(choiceFormatter, nameof(choiceFormatter));

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

        return await _ansiConsole.PromptAsync(prompt, cancellationToken);
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion)
    {
        var cliInformationalVersion = VersionHelper.GetDefaultTemplateVersion();

        DisplayError(InteractionServiceStrings.AppHostNotCompatibleConsiderUpgrading);
        Console.WriteLine();
        _ansiConsole.MarkupLine(
            $"\t[bold]{InteractionServiceStrings.AspireHostingSDKVersion}[/]: {appHostHostingVersion}");
        _ansiConsole.MarkupLine($"\t[bold]{InteractionServiceStrings.AspireCLIVersion}[/]: {cliInformationalVersion}");
        _ansiConsole.MarkupLine($"\t[bold]{InteractionServiceStrings.RequiredCapability}[/]: {ex.RequiredCapability}");
        Console.WriteLine();
        return ExitCodeConstants.AppHostIncompatible;
    }

    public void DisplayError(string errorMessage)
    {
        DisplayMessage("thumbs_down", $"[red bold]{errorMessage}[/]");
    }

    public void DisplayMessage(string emoji, string message)
    {
        _ansiConsole.MarkupLine($":{emoji}:  {message}");
    }

    public void DisplayPlainText(string message)
    {
        _ansiConsole.WriteLine(message);
    }

    public void DisplaySuccess(string message)
    {
        DisplayMessage("thumbs_up", message);
    }

    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
        _ansiConsole.WriteLine();
        _ansiConsole.MarkupLine($"[green bold]{InteractionServiceStrings.Dashboard}[/]:");
        if (dashboardUrls.CodespacesUrlWithLoginToken is not null)
        {
            _ansiConsole.MarkupLine(
                $":chart_increasing:  {InteractionServiceStrings.DirectLink}: [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
            _ansiConsole.MarkupLine(
                $":chart_increasing:  {InteractionServiceStrings.CodespacesLink}: [link={dashboardUrls.CodespacesUrlWithLoginToken}]{dashboardUrls.CodespacesUrlWithLoginToken}[/]");
        }
        else
        {
            _ansiConsole.MarkupLine($":chart_increasing:  [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
        }
        _ansiConsole.WriteLine();
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
        foreach (var (stream, line) in lines)
        {
            if (stream == "stdout")
            {
                _ansiConsole.MarkupLineInterpolated($"{line}");
            }
            else
            {
                _ansiConsole.MarkupLineInterpolated($"[red]{line}[/]");
            }
        }
    }

    public void DisplayCancellationMessage()
    {
        _ansiConsole.WriteLine();
        _ansiConsole.WriteLine();
        DisplayMessage("stop_sign", $"[teal bold]{InteractionServiceStrings.StoppingAspire}[/]");
    }

    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        return _ansiConsole.ConfirmAsync(promptText, defaultValue, cancellationToken);
    }

    public void DisplaySubtleMessage(string message)
    {
        _ansiConsole.MarkupLine($"[dim]{message}[/]");
    }

    public void DisplayEmptyLine()
    {
        _ansiConsole.WriteLine();
    }

    private const string UpdateUrl = "https://aka.ms/aspire/update";

    public void DisplayVersionUpdateNotification(string newerVersion)
    {
        _ansiConsole.WriteLine();
        _ansiConsole.MarkupLine($"[yellow]A new version of the Aspire CLI is available: {newerVersion}[/]");
        _ansiConsole.MarkupLine($"[dim]For more information, see: [link]{UpdateUrl}[/][/]");
        _ansiConsole.WriteLine();
    }
}
