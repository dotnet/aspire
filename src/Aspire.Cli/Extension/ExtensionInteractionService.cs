// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Spectre.Console;

namespace Aspire.Cli.Extension;

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

        // TODO implement extension-specific handling
        await _consoleInteractionService.ShowStatusAsync(statusText, () => task);

        return await task.ConfigureAwait(false);
    }

    public void ShowStatus(string statusText, Action action)
    {
        // TODO implement extension-specific handling
        _consoleInteractionService.ShowStatus(statusText, action);
    }

    public async Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null,
        CancellationToken cancellationToken = default)
    {
        if (_extensionPromptEnabled)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
        else
        {
            return _consoleInteractionService.PromptForSelectionAsync(promptText, choices, choiceFormatter, cancellationToken);
        }
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion)
    {
        throw new NotImplementedException();
    }

    public void DisplayError(string errorMessage)
    {
        throw new NotImplementedException();
    }

    public void DisplayMessage(string emoji, string message)
    {
        Debug.Assert(_extensionTaskChannel.Writer.TryWrite(backchannel => backchannel.DisplayMessageAsync(emoji, message, CancellationToken.None)));
        _consoleInteractionService.DisplayMessage(emoji, message);
    }

    public void DisplaySuccess(string message)
    {
        throw new NotImplementedException();
    }

    public void DisplaySubtleMessage(string message)
    {
        throw new NotImplementedException();
    }

    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
        throw new NotImplementedException();
    }

    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines)
    {
        throw new NotImplementedException();
    }

    public void DisplayCancellationMessage()
    {
        throw new NotImplementedException();
    }

    public void DisplayEmptyLine()
    {
        throw new NotImplementedException();
    }
}
