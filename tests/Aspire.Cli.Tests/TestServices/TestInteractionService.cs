// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Spectre.Console;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestInteractionService : IInteractionService
{
    public Action<string>? DisplayErrorCallback { get; set; }

    public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return action();
    }

    public void ShowStatus(string statusText, Action action)
    {
        action();
    }

    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(defaultValue ?? string.Empty);
    }

    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        if (!choices.Any())
        {
            throw new EmptyChoicesException($"No items available for selection: {promptText}");
        }

        return Task.FromResult(choices.First());
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion)
    {
        return 0;
    }

    public void DisplayError(string errorMessage)
    {
        DisplayErrorCallback?.Invoke(errorMessage);
    }

    public void DisplayMessage(string emoji, string message)
    {
    }

    public void DisplaySuccess(string message)
    {
    }

    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
    }

    public void DisplayCancellationMessage()
    {
    }

    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public void DisplaySubtleMessage(string message)
    {
    }

    public void DisplayEmptyLine()
    {
    }
}