// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestExtensionBackchannel : IExtensionBackchannel
{
    public TaskCompletionSource? ConnectAsyncCalled { get; set; }
    public Action? ConnectAsyncCallback { get; set; }

    public TaskCompletionSource? PingAsyncCalled { get; set; }
    public Func<long, Task<long>>? PingAsyncCallback { get; set; }

    public TaskCompletionSource? DisplayMessageAsyncCalled { get; set; }
    public Func<string, string, Task>? DisplayMessageAsyncCallback { get; set; }

    public TaskCompletionSource? DisplaySuccessAsyncCalled { get; set; }
    public Func<string, Task>? DisplaySuccessAsyncCallback { get; set; }

    public TaskCompletionSource? DisplaySubtleMessageAsyncCalled { get; set; }
    public Func<string, Task>? DisplaySubtleMessageAsyncCallback { get; set; }

    public TaskCompletionSource? DisplayErrorAsyncCalled { get; set; }
    public Func<string, Task>? DisplayErrorAsyncCallback { get; set; }

    public TaskCompletionSource? DisplayEmptyLineAsyncCalled { get; set; }
    public Func<Task>? DisplayEmptyLineAsyncCallback { get; set; }

    public TaskCompletionSource? DisplayIncompatibleVersionErrorAsyncCalled { get; set; }
    public Func<string, string, Task>? DisplayIncompatibleVersionErrorAsyncCallback { get; set; }

    public TaskCompletionSource? DisplayCancellationMessageAsyncCalled { get; set; }
    public Func<Task>? DisplayCancellationMessageAsyncCallback { get; set; }

    public TaskCompletionSource? DisplayLinesAsyncCalled { get; set; }
    public Func<IEnumerable<DisplayLineState>, Task>? DisplayLinesAsyncCallback { get; set; }

    public TaskCompletionSource? DisplayDashboardUrlsAsyncCalled { get; set; }
    public Func<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken), Task>? DisplayDashboardUrlsAsyncCallback { get; set; }

    public TaskCompletionSource? ShowStatusAsyncCalled { get; set; }
    public Func<string?, Task>? ShowStatusAsyncCallback { get; set; }

    public TaskCompletionSource? PromptForSelectionAsyncCalled { get; set; }

    public TaskCompletionSource? ConfirmAsyncCalled { get; set; }
    public Func<string, bool, Task<bool>>? ConfirmAsyncCallback { get; set; }

    public TaskCompletionSource? PromptForStringAsyncCalled { get; set; }
    public Func<string, string?, Func<string, ValidationResult>?, bool, Task<string>>? PromptForStringAsyncCallback { get; set; }

    public TaskCompletionSource? OpenProjectAsyncCalled { get; set; }
    public Func<string, Task>? OpenProjectAsyncCallback { get; set; }

    public TaskCompletionSource? LogMessageAsyncCalled { get; set; }
    public Func<LogLevel, string, Task>? LogMessageAsyncCallback { get; set; }

    public TaskCompletionSource? GetCapabilitiesAsyncCalled { get; set; }
    public Func<Task<string[]>>? GetCapabilitiesAsyncCallback { get; set; }

    public TaskCompletionSource? LaunchAppHostAsyncCalled { get; set; }
    public Func<string, string, List<string>, List<EnvVar>, Task>? LaunchAppHostAsyncCallback { get; set; }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        ConnectAsyncCalled?.SetResult();
        ConnectAsyncCallback?.Invoke();
        return Task.CompletedTask;
    }

    public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        PingAsyncCalled?.SetResult();
        return PingAsyncCallback?.Invoke(timestamp) ?? Task.FromResult(timestamp);
    }

    public Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken)
    {
        DisplayMessageAsyncCalled?.SetResult();
        return DisplayMessageAsyncCallback?.Invoke(emoji, message) ?? Task.CompletedTask;
    }

    public Task DisplaySuccessAsync(string message, CancellationToken cancellationToken)
    {
        DisplaySuccessAsyncCalled?.SetResult();
        return DisplaySuccessAsyncCallback?.Invoke(message) ?? Task.CompletedTask;
    }

    public Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken)
    {
        DisplaySubtleMessageAsyncCalled?.SetResult();
        return DisplaySubtleMessageAsyncCallback?.Invoke(message) ?? Task.CompletedTask;
    }

    public Task DisplayErrorAsync(string errorMessage, CancellationToken cancellationToken)
    {
        DisplayErrorAsyncCalled?.SetResult();
        return DisplayErrorAsyncCallback?.Invoke(errorMessage) ?? Task.CompletedTask;
    }

    public Task DisplayEmptyLineAsync(CancellationToken cancellationToken)
    {
        DisplayEmptyLineAsyncCalled?.SetResult();
        return DisplayEmptyLineAsyncCallback?.Invoke() ?? Task.CompletedTask;
    }

    public Task DisplayIncompatibleVersionErrorAsync(string appHostHostingVersion, string errorMessage, CancellationToken cancellationToken)
    {
        DisplayIncompatibleVersionErrorAsyncCalled?.SetResult();
        return DisplayIncompatibleVersionErrorAsyncCallback?.Invoke(appHostHostingVersion, errorMessage) ?? Task.CompletedTask;
    }

    public Task DisplayCancellationMessageAsync(CancellationToken cancellationToken)
    {
        DisplayCancellationMessageAsyncCalled?.SetResult();
        return DisplayCancellationMessageAsyncCallback?.Invoke() ?? Task.CompletedTask;
    }

    public Task DisplayLinesAsync(IEnumerable<DisplayLineState> lines, CancellationToken cancellationToken)
    {
        DisplayLinesAsyncCalled?.SetResult();
        return DisplayLinesAsyncCallback?.Invoke(lines) ?? Task.CompletedTask;
    }

    public Task DisplayDashboardUrlsAsync((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls, CancellationToken cancellationToken)
    {
        DisplayDashboardUrlsAsyncCalled?.SetResult();
        return DisplayDashboardUrlsAsyncCallback?.Invoke(dashboardUrls) ?? Task.CompletedTask;
    }

    public Task ShowStatusAsync(string? status, CancellationToken cancellationToken)
    {
        ShowStatusAsyncCalled?.SetResult();
        return ShowStatusAsyncCallback?.Invoke(status) ?? Task.CompletedTask;
    }

    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken) where T : notnull
    {
        PromptForSelectionAsyncCalled?.SetResult();

        if (!choices.Any())
        {
            throw new InvalidOperationException($"No items available for selection: {promptText}");
        }

        return Task.FromResult(choices.First());
    }

    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        ConfirmAsyncCalled?.SetResult();
        return ConfirmAsyncCallback != null
            ? ConfirmAsyncCallback.Invoke(promptText, defaultValue)
            : Task.FromResult(true);
    }

    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, CancellationToken cancellationToken = default)
    {
        PromptForStringAsyncCalled?.SetResult();
        return PromptForStringAsyncCallback != null
            ? PromptForStringAsyncCallback.Invoke(promptText, defaultValue, validator, isSecret)
            : Task.FromResult(defaultValue ?? string.Empty);
    }

    public Task OpenProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        OpenProjectAsyncCalled?.SetResult();
        return OpenProjectAsyncCallback != null
            ? OpenProjectAsyncCallback.Invoke(projectPath)
            : Task.CompletedTask;
    }

    public Task LogMessageAsync(LogLevel logLevel, string message, CancellationToken cancellationToken)
    {
        LogMessageAsyncCalled?.SetResult();
        return LogMessageAsyncCallback != null
            ? LogMessageAsyncCallback.Invoke(logLevel, message)
            : Task.CompletedTask;
    }

    public Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        GetCapabilitiesAsyncCalled?.SetResult();
        return GetCapabilitiesAsyncCallback != null
            ? GetCapabilitiesAsyncCallback.Invoke()
            : Task.FromResult(Array.Empty<string>());
    }

    public Task LaunchAppHostAsync(string projectPath, string targetFramework, List<string> arguments, List<EnvVar> envVars, CancellationToken cancellationToken)
    {
        LaunchAppHostAsyncCalled?.SetResult();
        return LaunchAppHostAsyncCallback != null
            ? LaunchAppHostAsyncCallback.Invoke(projectPath, targetFramework, arguments, envVars)
            : Task.CompletedTask;
    }
}
