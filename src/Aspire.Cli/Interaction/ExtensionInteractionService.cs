// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Interaction;

internal class ExtensionInteractionService : IInteractionService
{
    private readonly ConsoleInteractionService _consoleInteractionService;
    private readonly bool _extensionPromptEnabled;
    private readonly Channel<Func<IExtensionBackchannel, Task>> _extensionTaskChannel;

    public ExtensionInteractionService(ConsoleInteractionService consoleInteractionService, ExtensionBackchannelConnector backchannelConnector, bool extensionPromptEnabled, CancellationToken? cancellationToken = null)
    {
        _consoleInteractionService = consoleInteractionService;
        _extensionPromptEnabled = extensionPromptEnabled;
        _extensionTaskChannel = Channel.CreateUnbounded<Func<IExtensionBackchannel, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _ = Task.Run(async () =>
        {
            while (await _extensionTaskChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                var extensionBackchannel = await backchannelConnector.WaitForConnectionAsync(cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                var taskFunction = await _extensionTaskChannel.Reader.ReadAsync().ConfigureAwait(false);
                await taskFunction.Invoke(extensionBackchannel);
            }
        });
    }

    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        var task = action();
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.ShowStatusAsync(statusText, CancellationToken.None)));

        var value = await task.ConfigureAwait(false);
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.ShowStatusAsync(null, CancellationToken.None)));
        return value;
    }

    public void ShowStatus(string statusText, Action action)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.ShowStatusAsync(statusText, CancellationToken.None)));
        _consoleInteractionService.ShowStatus(statusText, action);
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.ShowStatusAsync(null, CancellationToken.None)));
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null,
        CancellationToken cancellationToken = default)
    {
        if (_extensionPromptEnabled)
        {
            // TODO implement extension-specific handling
            return await _consoleInteractionService.PromptForStringAsync(promptText, defaultValue, validator, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await _consoleInteractionService.PromptForStringAsync(promptText, defaultValue, validator, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        if (_extensionPromptEnabled)
        {
            // TODO implement extension-specific handling
            return _consoleInteractionService.ConfirmAsync(promptText, defaultValue, cancellationToken);
        }
        else
        {
            return _consoleInteractionService.ConfirmAsync(promptText, defaultValue, cancellationToken);
        }
    }

    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter,
        CancellationToken cancellationToken = default) where T : notnull
    {
        if (_extensionPromptEnabled)
        {
            // TODO implement extension-specific handling
            return _consoleInteractionService.PromptForSelectionAsync(promptText, choices, choiceFormatter, cancellationToken);
        }
        else
        {
            return _consoleInteractionService.PromptForSelectionAsync(promptText, choices, choiceFormatter, cancellationToken);
        }
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayIncompatibleVersionErrorAsync(ex.RequiredCapability, appHostHostingSdkVersion, CancellationToken.None)));
        return _consoleInteractionService.DisplayIncompatibleVersionError(ex, appHostHostingSdkVersion);
    }

    public void DisplayError(string errorMessage)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayErrorAsync(errorMessage, CancellationToken.None)));
        _consoleInteractionService.DisplayError(errorMessage);
    }

    public void DisplayMessage(string emoji, string message)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayMessageAsync(emoji, message, CancellationToken.None)));
        _consoleInteractionService.DisplayMessage(emoji, message);
    }

    public void DisplaySuccess(string message)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplaySuccessAsync(message, CancellationToken.None)));
        _consoleInteractionService.DisplaySuccess(message);
    }

    public void DisplaySubtleMessage(string message)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplaySubtleMessageAsync(message, CancellationToken.None)));
        _consoleInteractionService.DisplaySubtleMessage(message);
    }

    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayDashboardUrlsAsync(dashboardUrls, CancellationToken.None)));
        _consoleInteractionService.DisplayDashboardUrls(dashboardUrls);
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayLinesAsync(lines, CancellationToken.None)));
        _consoleInteractionService.DisplayLines(lines);
    }

    public void DisplayCancellationMessage()
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayCancellationMessageAsync(CancellationToken.None)));
        _consoleInteractionService.DisplayCancellationMessage();
    }

    public void DisplayEmptyLine()
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayEmptyLineAsync(CancellationToken.None)));
        _consoleInteractionService.DisplayEmptyLine();
    }
}
