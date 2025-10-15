// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Interaction;

internal interface IExtensionInteractionService : IInteractionService
{
    IExtensionBackchannel Backchannel { get; }
    void OpenEditor(string projectPath);
    void LogMessage(LogLevel logLevel, string message);
    Task LaunchAppHostAsync(string projectFile, List<string> arguments, List<EnvVar> environment, bool debug);
    void DisplayDashboardUrls(DashboardUrlsState dashboardUrls);
    void NotifyAppHostStartupCompleted();
    void DisplayConsolePlainText(string message);
    Task StartDebugSessionAsync(string workingDirectory, string? projectFile, bool debug);
    void WriteDebugSessionMessage(string message, bool stdout);
}

internal class ExtensionInteractionService : IExtensionInteractionService
{
    private readonly ConsoleInteractionService _consoleInteractionService;
    private readonly bool _extensionPromptEnabled;
    private readonly CancellationToken _cancellationToken;
    private readonly Channel<Func<Task>> _extensionTaskChannel;

    public IExtensionBackchannel Backchannel { get; }

    public ExtensionInteractionService(ConsoleInteractionService consoleInteractionService, IExtensionBackchannel backchannel, bool extensionPromptEnabled, CancellationToken? cancellationToken = null)
    {
        _consoleInteractionService = consoleInteractionService;
        Backchannel = backchannel;
        _extensionPromptEnabled = extensionPromptEnabled;
        _cancellationToken = cancellationToken ?? CancellationToken.None;
        _extensionTaskChannel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _ = Task.Run(async () =>
        {
            while (await _extensionTaskChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    var taskFunction = await _extensionTaskChannel.Reader.ReadAsync().ConfigureAwait(false);
                    await taskFunction.Invoke();
                }
                catch (Exception ex)
                {
                    await Backchannel.DisplayErrorAsync(ex.Message.RemoveSpectreFormatting(), _cancellationToken);
                    _consoleInteractionService.DisplayError(ex.Message);
                }
            }
        });
    }

    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.ShowStatusAsync(statusText.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);

        var value = await _consoleInteractionService.ShowStatusAsync(statusText, action).ConfigureAwait(false);
        result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.ShowStatusAsync(null, _cancellationToken));
        Debug.Assert(result);
        return value;
    }

    public void ShowStatus(string statusText, Action action)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.ShowStatusAsync(statusText.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.ShowStatus(statusText, action);

        result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.ShowStatusAsync(null, _cancellationToken));
        Debug.Assert(result);
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
    {
        if (_extensionPromptEnabled)
        {
            var tcs = new TaskCompletionSource<string>();

            await _extensionTaskChannel.Writer.WriteAsync(async () =>
            {
                try
                {
                    string result;
                    if (isSecret)
                    {
                        // Check if extension supports the new secret prompts capability
                        var hasSecretPromptsCapability = await Backchannel.HasCapabilityAsync(ExtensionBackchannel.SecretPromptsCapability, _cancellationToken).ConfigureAwait(false);

                        if (hasSecretPromptsCapability)
                        {
                            // Use the new dedicated secret prompt method (no default value for secrets)
                            result = await Backchannel.PromptForSecretStringAsync(promptText.RemoveSpectreFormatting(), validator, required, _cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // Fallback to regular prompt for older extension versions
                            result = await Backchannel.PromptForStringAsync(promptText.RemoveSpectreFormatting(), defaultValue, validator, required, _cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        result = await Backchannel.PromptForStringAsync(promptText.RemoveSpectreFormatting(), defaultValue, validator, required, _cancellationToken).ConfigureAwait(false);
                    }

                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, cancellationToken).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }
        else
        {
            return await _consoleInteractionService.PromptForStringAsync(promptText, defaultValue, validator, isSecret, required, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        if (_extensionPromptEnabled)
        {
            var tcs = new TaskCompletionSource<bool>();

            await _extensionTaskChannel.Writer.WriteAsync(async () =>
            {
                try
                {
                    var result = await Backchannel.ConfirmAsync(promptText.RemoveSpectreFormatting(), defaultValue, _cancellationToken).ConfigureAwait(false);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    DisplayError(ex.Message);
                }
            }, cancellationToken).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }
        else
        {
            return await _consoleInteractionService.ConfirmAsync(promptText, defaultValue, cancellationToken);
        }
    }

    public async Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter,
        CancellationToken cancellationToken = default) where T : notnull
    {
        if (_extensionPromptEnabled)
        {
            var tcs = new TaskCompletionSource<T>();

            await _extensionTaskChannel.Writer.WriteAsync(async () =>
            {
                try
                {
                    var result = await Backchannel.PromptForSelectionAsync(promptText.RemoveSpectreFormatting(), choices, choiceFormatter, _cancellationToken).ConfigureAwait(false);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    DisplayError(ex.Message);
                }
            }, cancellationToken).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }
        else
        {
            return await _consoleInteractionService.PromptForSelectionAsync(promptText, choices, choiceFormatter, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<T>> PromptForSelectionsAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter,
        CancellationToken cancellationToken = default) where T : notnull
    {
        if (_extensionPromptEnabled)
        {
            var tcs = new TaskCompletionSource<IReadOnlyList<T>>();

            await _extensionTaskChannel.Writer.WriteAsync(async () =>
            {
                try
                {
                    var result = await Backchannel.PromptForSelectionsAsync(promptText.RemoveSpectreFormatting(), choices, choiceFormatter, _cancellationToken).ConfigureAwait(false);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    DisplayError(ex.Message);
                }
            }, cancellationToken).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }
        else
        {
            return await _consoleInteractionService.PromptForSelectionsAsync(promptText, choices, choiceFormatter, cancellationToken);
        }
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayIncompatibleVersionErrorAsync(ex.RequiredCapability, appHostHostingSdkVersion, _cancellationToken));
        Debug.Assert(result);

        return _consoleInteractionService.DisplayIncompatibleVersionError(ex, appHostHostingSdkVersion);
    }

    public void DisplayError(string errorMessage)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayErrorAsync(errorMessage.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayError(errorMessage);
    }

    public void DisplayMessage(string emoji, string message)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayMessageAsync(emoji, message.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayMessage(emoji, message);
    }

    public void DisplaySuccess(string message)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplaySuccessAsync(message.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplaySuccess(message);
    }

    public void DisplaySubtleMessage(string message)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplaySubtleMessageAsync(message.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplaySubtleMessage(message);
    }

    public void DisplayDashboardUrls(DashboardUrlsState dashboardUrls)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayDashboardUrlsAsync(dashboardUrls, _cancellationToken));
        Debug.Assert(result);
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayLinesAsync(lines.Select(line => new DisplayLineState(line.Stream.RemoveSpectreFormatting(), line.Line.RemoveSpectreFormatting())), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayLines(lines);
    }

    public void DisplayCancellationMessage()
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayCancellationMessageAsync(_cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayCancellationMessage();
    }

    public void DisplayEmptyLine()
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayEmptyLineAsync(_cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayEmptyLine();
    }

    public void OpenEditor(string path)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.OpenEditorAsync(path, _cancellationToken));
        Debug.Assert(result);
    }

    public void DisplayPlainText(string text)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.DisplayPlainTextAsync(text, _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayPlainText(text);
    }

    public void DisplayMarkdown(string markdown)
    {
        // Send raw markdown to extension (it can handle markdown natively)
        // Convert to Spectre markup for console display
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.LogMessageAsync(LogLevel.Information, markdown, _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayMarkdown(markdown);
    }

    public void DisplayVersionUpdateNotification(string newerVersion)
    {
        _consoleInteractionService.DisplayVersionUpdateNotification(newerVersion);
    }

    public void LogMessage(LogLevel logLevel, string message)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.LogMessageAsync(logLevel, message.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
    }

    public Task LaunchAppHostAsync(string projectFile, List<string> arguments, List<EnvVar> environment, bool debug)
    {
        return Backchannel.LaunchAppHostAsync(projectFile, arguments, environment, debug, _cancellationToken);
    }

    public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false)
    {
        _consoleInteractionService.WriteConsoleLog(message, lineNumber, type, isErrorMessage);
    }

    public void NotifyAppHostStartupCompleted()
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.NotifyAppHostStartupCompletedAsync(_cancellationToken));
        Debug.Assert(result);
    }

    public void DisplayConsolePlainText(string message)
    {
        _consoleInteractionService.DisplayPlainText(message);
    }

    public Task StartDebugSessionAsync(string workingDirectory, string? projectFile, bool debug)
    {
        return Backchannel.StartDebugSessionAsync(workingDirectory, projectFile, debug, _cancellationToken);
    }

    public void WriteDebugSessionMessage(string message, bool stdout)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => Backchannel.WriteDebugSessionMessageAsync(message.RemoveSpectreFormatting(), stdout, _cancellationToken));
        Debug.Assert(result);
    }
}
