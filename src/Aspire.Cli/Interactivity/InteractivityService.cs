// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;

namespace Aspire.Cli.Interactivity;

internal interface IInteractivityService
{
    Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T: notnull;
    Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, CancellationToken cancellationToken = default);
    Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action);
    void WriteLine(string line);
}

internal sealed class InteractivityService : IInteractivityService
{
    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync(statusText, (context) => action());
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, CancellationToken cancellationToken = default)
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

    public async Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T: notnull
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

    public void WriteLine(string line)
    {
        AnsiConsole.MarkupLine(line);
    }
}