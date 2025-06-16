// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Extension;

internal interface IExtensionBackchannel : IBackchannel
{
    Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken);
    Task DisplaySuccessAsync(string message, CancellationToken cancellationToken);
    Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken);
    Task DisplayErrorAsync(string error, CancellationToken cancellationToken);
    Task DisplayEmptyLineAsync(CancellationToken cancellationToken);
}

internal sealed class ExtensionBackchannel(ILogger<ExtensionBackchannel> logger, CliRpcTarget target) : BaseBackchannel<ExtensionBackchannel>(Name, logger, target), IExtensionBackchannel
{
    private const string Name = "Aspire Extension";

    public override string BaselineCapability => "baseline";
    private readonly ILogger<ExtensionBackchannel> _logger = logger;

    public async Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Sent message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displayMessage",
            [emoji, message],
            cancellationToken);
    }

    public async Task DisplaySuccessAsync(string message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Sent success message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displaySuccess",
            [message],
            cancellationToken);
    }

    public async Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Sent subtle message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displaySubtleMessage",
            [message],
            cancellationToken);
    }

    public async Task DisplayErrorAsync(string error, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Sent error message {Error}", error);

        await rpc.InvokeWithCancellationAsync(
            "displayError",
            [error],
            cancellationToken);
    }

    public async Task DisplayEmptyLineAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Sent empty line");

        await rpc.InvokeWithCancellationAsync(
            "displayEmptyLine",
            Array.Empty<object>(),
            cancellationToken);
    }

    public override void RaiseIncompatibilityException(string missingCapability)
    {
        throw new ExtensionIncompatibleException(
            $"The {Name} is incompatible with the CLI. The {Name} must be updated to a version that supports the {missingCapability} capability.",
            missingCapability
        );
    }
}
