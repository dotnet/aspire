// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rendering;

internal class RunExperimentalState : RenderableState
{
    public string? StatusMessage { get; set; }

    public async Task UpdateStatusAsync(string message, CancellationToken cancellationToken)
    {
        StatusMessage = message;
        await Updated.Writer.WriteAsync(true, cancellationToken);
    }
}