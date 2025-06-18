// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal interface IExtensionBackchannel
{
    Task ConnectAsync(string socketPath, CancellationToken cancellationToken);
    Task<long> PingAsync(long timestamp, CancellationToken cancellationToken);
    Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken);
    Task DisplaySuccessAsync(string message, CancellationToken cancellationToken);
    Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken);
    Task DisplayErrorAsync(string error, CancellationToken cancellationToken);
    Task DisplayEmptyLineAsync(CancellationToken cancellationToken);
    Task DisplayIncompatibleVersionErrorAsync(string requiredCapability, string appHostHostingSdkVersion, CancellationToken cancellationToken);
    Task DisplayCancellationMessageAsync(CancellationToken cancellationToken);
    Task DisplayLinesAsync(IEnumerable<(string Stream, string Line)> lines, CancellationToken cancellationToken);
    Task DisplayDashboardUrlsAsync((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls, CancellationToken cancellationToken);
    Task ShowStatusAsync(string? status, CancellationToken cancellationToken);
    Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken) where T : notnull;
}

internal sealed class ExtensionBackchannel(ILogger<ExtensionBackchannel> logger, ExtensionRpcTarget target, IConfiguration configuration) : IExtensionBackchannel
{
    private const string Name = "Aspire Extension";
    private const string BaselineCapability = "baseline.v1";

    private readonly ActivitySource _activitySource = new(nameof(ExtensionBackchannel));
    private readonly TaskCompletionSource<JsonRpc> _rpcTaskCompletionSource = new();
    private readonly string _token = configuration[KnownConfigNames.ExtensionToken]
        ?? throw new InvalidOperationException(ErrorStrings.ExtensionTokenMustBeSet);

    public async Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent ping with timestamp {Timestamp}", timestamp);

        var responseTimestamp = await rpc.InvokeWithCancellationAsync<long>(
            "PingAsync",
            [_token],
            cancellationToken);

        return responseTimestamp;
    }

    public async Task ConnectAsync(string socketPath, CancellationToken cancellationToken)
    {
        try
        {
            using var activity = _activitySource.StartActivity();

            if (_rpcTaskCompletionSource.Task.IsCompleted)
            {
                throw new InvalidOperationException($"Already connected to {Name} backchannel.");
            }

            logger.LogDebug("Connecting to {Name} backchannel at {SocketPath}", Name, socketPath);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var addressParts= socketPath.Split(':');
            if (addressParts.Length != 2 || !int.TryParse(addressParts[1], out var port) || port <= 0 || port > 65535)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, ErrorStrings.InvalidSocketPath, socketPath),
                    nameof(socketPath));
            }

            await socket.ConnectAsync(addressParts[0], port, cancellationToken);
            logger.LogDebug("Connected to {Name} backchannel at {SocketPath}", Name, socketPath);

            var stream = new NetworkStream(socket, true);
            var rpc = JsonRpc.Attach(stream, target);

            var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
                "getCapabilities",
                [_token],
                cancellationToken);

            if (!capabilities.Any(s => s == BaselineCapability))
            {
                throw new ExtensionIncompatibleException(
                    string.Format(CultureInfo.CurrentCulture, ErrorStrings.ExtensionIncompatibleWithCli, BaselineCapability),
                    BaselineCapability
                );
            }

            // start listening for incoming rpc calls in the background
            _ = Task.Run(rpc.StartListening, cancellationToken);
            _rpcTaskCompletionSource.SetResult(rpc);
        }
        catch (RemoteMethodNotFoundException ex)
        {
            logger.LogError(ex,
                "Failed to connect to {Name} backchannel. The connection must be updated to a version that supports the {BaselineCapability} capability.",
                Name,
                BaselineCapability);

            throw new ExtensionIncompatibleException(
                string.Format(CultureInfo.CurrentCulture, ErrorStrings.ExtensionIncompatibleWithCli, BaselineCapability),
                BaselineCapability
            );
        }
    }

    public async Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displayMessage",
            [_token, emoji, message],
            cancellationToken);
    }

    public async Task DisplaySuccessAsync(string message, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent success message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displaySuccess",
            [_token, message],
            cancellationToken);
    }

    public async Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent subtle message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displaySubtleMessage",
            [_token, message],
            cancellationToken);
    }

    public async Task DisplayErrorAsync(string error, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent error message {Error}", error);

        await rpc.InvokeWithCancellationAsync(
            "displayError",
            [_token, error],
            cancellationToken);
    }

    public async Task DisplayEmptyLineAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent empty line");

        await rpc.InvokeWithCancellationAsync(
            "displayEmptyLine",
            [_token],
            cancellationToken);
    }

    public async Task DisplayIncompatibleVersionErrorAsync(string requiredCapability, string appHostHostingSdkVersion, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent incompatible version error for capability {RequiredCapability} with hosting SDK version {AppHostHostingSdkVersion}",
            requiredCapability, appHostHostingSdkVersion);

        await rpc.InvokeWithCancellationAsync(
            "displayIncompatibleVersionError",
            [_token, requiredCapability, appHostHostingSdkVersion],
            cancellationToken);
    }

    public async Task DisplayCancellationMessageAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent cancellation message");

        await rpc.InvokeWithCancellationAsync(
            "displayCancellationMessage",
            [_token],
            cancellationToken);
    }

    public async Task DisplayLinesAsync(IEnumerable<(string Stream, string Line)> lines, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent lines for display");

        await rpc.InvokeWithCancellationAsync(
            "displayLines",
            [_token, lines],
            cancellationToken);
    }

    public async Task DisplayDashboardUrlsAsync((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent dashboard URLs for display");

        await rpc.InvokeWithCancellationAsync(
            "displayDashboardUrls",
            [_token, dashboardUrls],
            cancellationToken);
    }

    public async Task ShowStatusAsync(string? status, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent status update: {Status}", status);

        await rpc.InvokeWithCancellationAsync(
            "showStatus",
            [_token, status],
            cancellationToken);
    }

    public async Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter,
        CancellationToken cancellationToken) where T : notnull
    {
        var choicesList = choices.ToList();
        // this will throw if formatting results in non-distinct values. that should happen because we cannot send the formatter over the wire.
        var choicesByFormattedValue = choicesList.ToDictionary(choice => choiceFormatter(choice).RemoveFormatting(), choice => choice);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Prompting for selection with text: {PromptText}, choices: {Choices}", promptText, choicesByFormattedValue.Keys);

        var result = await rpc.InvokeWithCancellationAsync<string?>(
            "promptForSelection",
            [_token, promptText, choicesByFormattedValue.Keys],
            cancellationToken);

        return result is null
            ? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.NoSelectionMade, promptText))
            : choicesByFormattedValue[result];
    }
}
