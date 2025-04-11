// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Utils;

internal static class InteractionUtils
{
    public static async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync(statusText, (context) => action());
    }

    public static void ShowStatus(string statusText, Action action)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .Start(statusText, (context) => action());
    }

    public static async Task<NuGetPackage> PromptForTemplatesVersionAsync(IEnumerable<NuGetPackage> candidatePackages, CancellationToken cancellationToken)
    {
        return await PromptForSelectionAsync(
            "Select a template version:",
            candidatePackages,
            (p) => $"{p.Version} ({p.Source})",
            cancellationToken
            );
    }

    public static async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        var prompt = new TextPrompt<string>(promptText);

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue);
            prompt.ShowDefaultValue();
        }

        if (validator is not null)
        {
            prompt.Validate(validator);
        }

        return await AnsiConsole.PromptAsync(prompt, cancellationToken);
    }

    public static async Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T: notnull
    {
        ArgumentNullException.ThrowIfNull(promptText, nameof(promptText));
        ArgumentNullException.ThrowIfNull(choices, nameof(choices));
        ArgumentNullException.ThrowIfNull(choiceFormatter, nameof(choiceFormatter));

        var prompt = new SelectionPrompt<T>()
            .Title(promptText)
            .UseConverter(choiceFormatter)
            .AddChoices(choices)
            .PageSize(10)
            .EnableSearch()
            .HighlightStyle(Style.Parse("darkmagenta"));

        return await AnsiConsole.PromptAsync(prompt, cancellationToken);
    }

    public static int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion)
    {
        var cliInformationalVersion = VersionHelper.GetDefaultTemplateVersion();
        
        InteractionUtils.DisplayError("The app host is not compatible. Consider upgrading the app host or Aspire CLI.");
        Console.WriteLine();
        AnsiConsole.MarkupLine($"\t[bold]Aspire Hosting SDK Version[/]: {appHostHostingSdkVersion}");
        AnsiConsole.MarkupLine($"\t[bold]Aspire CLI Version[/]: {cliInformationalVersion}");
        AnsiConsole.MarkupLine($"\t[bold]Required Capability[/]: {ex.RequiredCapability}");
        Console.WriteLine();
        return ExitCodeConstants.AppHostIncompatible;
    }

    public static void DisplayError(string errorMessage)
    {
        DisplayMessage("thumbs_down", $"[red bold]{errorMessage}[/]");
    }

    public static void DisplayMessage(string emoji, string message)
    {
        AnsiConsole.MarkupLine($":{emoji}:  {message}");
    }

    public static void DisplaySuccess(string message)
    {
        DisplayMessage("thumbs_up", message);
    }

    public static void DisplayDashboardUrls(dynamic dashboardUrls)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green bold]Dashboard[/]:");
        if (dashboardUrls.CodespacesUrlWithLoginToken is not null)
        {
            AnsiConsole.MarkupLine($":chart_increasing:  Direct: [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
            AnsiConsole.MarkupLine($":chart_increasing:  Codespaces: [link={dashboardUrls.CodespacesUrlWithLoginToken}]{dashboardUrls.CodespacesUrlWithLoginToken}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($":chart_increasing:  [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
        }
        AnsiConsole.WriteLine();
    }
}