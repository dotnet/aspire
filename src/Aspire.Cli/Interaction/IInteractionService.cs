// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Interaction;

internal interface IInteractionService
{
    Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action);
    void ShowStatus(string statusText, Action action);
    Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default);
    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default);
    Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull;
    int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion);
    void DisplayError(string errorMessage);
    void DisplayMessage(string emoji, string message);
    void DisplayPlainText(string text);
    void DisplaySuccess(string message);
    void DisplaySubtleMessage(string message);
    void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls);
    void DisplayLines(IEnumerable<(string Stream, string Line)> lines);
    void DisplayCancellationMessage();
    void DisplayEmptyLine();
    void OpenNewProject(string projectPath);

    void DisplayVersionUpdateNotification(string newerVersion);
    void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false);
}
