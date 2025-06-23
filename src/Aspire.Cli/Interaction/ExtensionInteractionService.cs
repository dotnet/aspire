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
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(statusText.RemoveFormatting(), _cancellationToken)));

        var value = await task.ConfigureAwait(false);
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(null, _cancellationToken)));
        return value;
    }

    public void ShowStatus(string statusText, Action action)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(statusText.RemoveFormatting(), _cancellationToken)));
        _consoleInteractionService.ShowStatus(statusText, action);
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.ShowStatusAsync(null, _cancellationToken)));
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null,
        CancellationToken cancellationToken = default)
    {
        if (_extensionPromptEnabled)
        {
            var tcs = new TaskCompletionSource<string>();

            await _extensionTaskChannel.Writer.WriteAsync(async () =>
            {
                try
                {
                    var result = await _backchannel.PromptForStringAsync(promptText.RemoveFormatting(), defaultValue, validator, _cancellationToken).ConfigureAwait(false);
                    if (result is null)
                    {
                        throw new OperationCanceledException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.NoSelectionMade, promptText));
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
            return await _consoleInteractionService.PromptForStringAsync(promptText, defaultValue, validator, cancellationToken).ConfigureAwait(false);
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
                    var result = await _backchannel.ConfirmAsync(promptText.RemoveFormatting(), defaultValue, _cancellationToken).ConfigureAwait(false);
                    if (result is null)
                    {
                        throw new OperationCanceledException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.NoSelectionMade, promptText));
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
                    var result = await _backchannel.PromptForSelectionAsync(promptText.RemoveFormatting(), choices, choiceFormatter, _cancellationToken).ConfigureAwait(false);
                    if (result is null)
                    {
                        throw new OperationCanceledException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.NoSelectionMade, promptText));
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
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayIncompatibleVersionErrorAsync(ex.RequiredCapability, appHostHostingSdkVersion, _cancellationToken)));
        return _consoleInteractionService.DisplayIncompatibleVersionError(ex, appHostHostingSdkVersion);
    }

    public void DisplayError(string errorMessage)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayErrorAsync(errorMessage.RemoveFormatting(), _cancellationToken)));
        _consoleInteractionService.DisplayError(errorMessage);
    }

    public void DisplayMessage(string emoji, string message)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayMessageAsync(emoji, message.RemoveFormatting(), _cancellationToken)));
        _consoleInteractionService.DisplayMessage(emoji, message);
    }

    public void DisplaySuccess(string message)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplaySuccessAsync(message.RemoveFormatting(), _cancellationToken)));
        _consoleInteractionService.DisplaySuccess(message);
    }

    public void DisplaySubtleMessage(string message)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplaySubtleMessageAsync(message.RemoveFormatting(), _cancellationToken)));
        _consoleInteractionService.DisplaySubtleMessage(message);
    }

    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayDashboardUrlsAsync(dashboardUrls, _cancellationToken)));
        _consoleInteractionService.DisplayDashboardUrls(dashboardUrls);
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayLinesAsync(lines.Select(line => (Stream: line.Stream.RemoveFormatting(), Line: line.Line.RemoveFormatting())), _cancellationToken)));
        _consoleInteractionService.DisplayLines(lines);
    }

    public void DisplayCancellationMessage()
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayCancellationMessageAsync(_cancellationToken)));
        _consoleInteractionService.DisplayCancellationMessage();
    }

    public void DisplayEmptyLine()
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.DisplayEmptyLineAsync(_cancellationToken)));
        _consoleInteractionService.DisplayEmptyLine();
    }

    public void OpenNewProject(string projectPath)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(() => _backchannel.OpenProjectAsync(projectPath, _cancellationToken)));
    }
}
