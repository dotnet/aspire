// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Utils;

internal static class InteractionUtils
{
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
        
        AnsiConsole.MarkupLine($"[red bold]:thumbs_down:  The app host is not compatible. Consider upgrading the app host or Aspire CLI.[/]");
        Console.WriteLine();
        AnsiConsole.MarkupLine($"\t[bold]Aspire Hosting SDK Version[/]: {appHostHostingSdkVersion}");
        AnsiConsole.MarkupLine($"\t[bold]Aspire CLI Version[/]: {cliInformationalVersion}");
        AnsiConsole.MarkupLine($"\t[bold]Required Capability[/]: {ex.RequiredCapability}");
        Console.WriteLine();
        return ExitCodeConstants.AppHostIncompatible;
    }
}