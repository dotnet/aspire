// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Interaction;

internal interface IInteractionService
{
    Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action);
    void ShowStatus(string statusText, Action action);
    Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, CancellationToken cancellationToken = default);
    Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull;
    int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion);
    void DisplayError(string errorMessage);
    void DisplayMessage(string emoji, string message);
    void DisplaySuccess(string message);
    void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls);
    void DisplayLines(IEnumerable<(string Stream, string Line)> lines);
}
