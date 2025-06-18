// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using Aspire.Cli.Resources;
using Aspire.Hosting;
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
}

internal sealed class ExtensionBackchannel(ILogger<ExtensionBackchannel> logger, ExtensionRpcTarget target) : IExtensionBackchannel
{
    private const string Name = "Aspire Extension";
    private const string BaselineCapability = "baseline.v1";

    private readonly ActivitySource _activitySource = new(nameof(ExtensionBackchannel));
    private readonly TaskCompletionSource<JsonRpc> _rpcTaskCompletionSource = new();
    private readonly string _token = Environment.GetEnvironmentVariable(KnownConfigNames.ExtensionToken)
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
}

class ExtensionRpcTarget
{
}
