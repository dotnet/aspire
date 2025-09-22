// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class LoggedOutNotificationManager(IInteractionService interactionService) : CoalescingAsyncOperation
{
    public Task NotifyUserLoggedOutAsync(CancellationToken cancellationToken = default) => RunAsync(cancellationToken);

    protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        if (interactionService.IsAvailable)
        {
            _ = await interactionService.PromptNotificationAsync(
                "Dev tunnels",
                $"Dev tunnels authentication has expired. Restart the dev tunnel resources to re-authenticate.",
                new() { Intent = MessageIntent.Warning },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
