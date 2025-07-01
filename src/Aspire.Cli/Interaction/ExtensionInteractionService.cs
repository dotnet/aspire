// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Interaction;

internal class ExtensionInteractionService : IInteractionService
{
    private readonly ConsoleInteractionService _consoleInteractionService;
    private readonly IExtensionBackchannel _backchannel;
    private readonly bool _extensionPromptEnabled;
    private readonly CancellationToken _cancellationToken;
    private readonly Channel<Func<Task>> _extensionTaskChannel;

    public ExtensionInteractionService(ConsoleInteractionService consoleInteractionService, IExtensionBackchannel backchannel, bool extensionPromptEnabled, CancellationToken? cancellationToken = null)
    {
        _consoleInteractionService = consoleInteractionService;
        _backchannel = backchannel;
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
                var taskFunction = await _extensionTaskChannel.Reader.ReadAsync().ConfigureAwait(false);
                await taskFunction.Invoke();
            }
        });
    }

    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        var task = action();
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(statusText.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);

        var value = await task.ConfigureAwait(false);
        result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(null, _cancellationToken));
        Debug.Assert(result);
        return value;
    }

    public void ShowStatus(string statusText, Action action)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(statusText.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.ShowStatus(statusText, action);
        result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(null, _cancellationToken));
        Debug.Assert(result);
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null,
        bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
    {
        if (_extensionPromptEnabled)
        {
            var tcs = new TaskCompletionSource<string>();

            await _extensionTaskChannel.Writer.WriteAsync(async () =>
            {
                try
                {
                    var result = await _backchannel.PromptForStringAsync(promptText.RemoveSpectreFormatting(), defaultValue, validator, required, _cancellationToken).ConfigureAwait(false);
                    if (result is null)
                    {
                        throw new ExtensionOperationCanceledException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.NoSelectionMade, promptText));
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
                    var result = await _backchannel.ConfirmAsync(promptText.RemoveSpectreFormatting(), defaultValue, _cancellationToken).ConfigureAwait(false);
                    if (result is null)
                    {
                        throw new ExtensionOperationCanceledException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.NoSelectionMade, promptText));
                    }

                    tcs.SetResult(result.Value);
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
                    var result = await _backchannel.PromptForSelectionAsync(promptText.RemoveSpectreFormatting(), choices, choiceFormatter, _cancellationToken).ConfigureAwait(false);
                    if (result is null)
                    {
                        throw new ExtensionOperationCanceledException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.NoSelectionMade, promptText));
                    }

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

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayIncompatibleVersionErrorAsync(ex.RequiredCapability, appHostHostingSdkVersion, _cancellationToken));
        Debug.Assert(result);
        return _consoleInteractionService.DisplayIncompatibleVersionError(ex, appHostHostingSdkVersion);
    }

    public void DisplayError(string errorMessage)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayErrorAsync(errorMessage.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayError(errorMessage);
    }

    public void DisplayMessage(string emoji, string message)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayMessageAsync(emoji, message.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayMessage(emoji, message);
    }

    public void DisplaySuccess(string message)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplaySuccessAsync(message.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplaySuccess(message);
    }

    public void DisplaySubtleMessage(string message)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplaySubtleMessageAsync(message.RemoveSpectreFormatting(), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplaySubtleMessage(message);
    }

    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayDashboardUrlsAsync(dashboardUrls, _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayDashboardUrls(dashboardUrls);
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayLinesAsync(lines.Select(line => new DisplayLineState(line.Stream.RemoveSpectreFormatting(), line.Line.RemoveSpectreFormatting())), _cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayLines(lines);
    }

    public void DisplayCancellationMessage()
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayCancellationMessageAsync(_cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayCancellationMessage();
    }

    public void DisplayEmptyLine()
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayEmptyLineAsync(_cancellationToken));
        Debug.Assert(result);
        _consoleInteractionService.DisplayEmptyLine();
    }

    public void OpenNewProject(string projectPath)
    {
        var result = _extensionTaskChannel.Writer.TryWrite(() => _backchannel.OpenProjectAsync(projectPath, _cancellationToken));
        Debug.Assert(result);
    }

    public void DisplayPlainText(string text)
    {
        _consoleInteractionService.DisplayPlainText(text);
    }

    public void DisplayVersionUpdateNotification(string newerVersion)
    {
        _consoleInteractionService.DisplayVersionUpdateNotification(newerVersion);
    }
}
