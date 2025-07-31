// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestExtensionInteractionService(IServiceProvider serviceProvider) : IExtensionInteractionService
{
    public Action<string>? DisplayErrorCallback { get; set; }
    public Action<string>? DisplaySubtleMessageCallback { get; set; }
    public Action<string>? DisplayConsoleWriteLineMessage { get; set; }
    public Action? LaunchAppHostCallback { get; set; }

    public IExtensionBackchannel Backchannel { get; } = serviceProvider.GetRequiredService<IExtensionBackchannel>();

    public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return action();
    }

    public void ShowStatus(string statusText, Action action)
    {
        action();
    }

    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
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
        DisplaySubtleMessageCallback?.Invoke(message);
    }

    public void DisplayEmptyLine()
    {
    }

    public void DisplayPlainText(string text)
    {
    }

    public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false)
    {
        var output = $"[{(isErrorMessage ? "Error" : type ?? "Info")}] {message} (Line: {lineNumber})";
        DisplayConsoleWriteLineMessage?.Invoke(output);
    }

    public Action<string>? DisplayVersionUpdateNotificationCallback { get; set; }

    public void DisplayVersionUpdateNotification(string newerVersion)
    {
        DisplayVersionUpdateNotificationCallback?.Invoke(newerVersion);
    }

    public Action<string>? OpenNewProjectCallback { get; set; }

    public void OpenNewProject(string projectPath)
    {
        OpenNewProjectCallback?.Invoke(projectPath);
    }

    public Action<int, string>? RequestAppHostAttachCallback { get; set; }

    public Task RequestAppHostAttachAsync(int processId, string projectName)
    {
        RequestAppHostAttachCallback?.Invoke(processId, projectName);
        return Task.CompletedTask;
    }

    public Action<LogLevel, string>? LogMessageCallback { get; set; }

    public void LogMessage(LogLevel logLevel, string message)
    {
        LogMessageCallback?.Invoke(logLevel, message);
    }

    public Task LaunchAppHostAsync(string projectFile, string workingDirectory, List<string> arguments, List<EnvVar> environment, bool debug)
    {
        LaunchAppHostCallback?.Invoke();
        return Task.CompletedTask;
    }
}
