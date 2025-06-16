// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Extension;

internal interface IExtensionBackchannel : IBackchannel
{
    Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken);
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

    public override void RaiseIncompatibilityException(string missingCapability)
    {
        throw new ExtensionIncompatibleException(
            $"The {Name} is incompatible with the CLI. The {Name} must be updated to a version that supports the {missingCapability} capability.",
            missingCapability
        );
    }
}
