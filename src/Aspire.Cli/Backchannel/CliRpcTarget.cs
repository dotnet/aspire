// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;

namespace Aspire.Cli.Backchannel;

internal class CliRpcTarget(IInteractionService interactionService)
{
    public void ReceiveCommandOutput(string output)
    {
        interactionService.WriteConsoleLog(output, isError: false);
    }

    public void ReceiveCommandError(string output)
    {
        interactionService.WriteConsoleLog(output, isError: true);
    }
}
