// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal class ResourceLogRenderable(ConsoleDashboardState state) : FocusableRenderable
{
    public override async Task<FocusableRenderable> ProcessInputAsync(ConsoleKey key, CancellationToken cancellationToken)
    {
        await state.UpdateStatusAsync("Processed input in resource log", cancellationToken);
        return this;
    }

    public override async Task FocusAsync(CancellationToken cancellationToken)
    {
        await state.UpdateStatusAsync("Focused on resource log", cancellationToken);
    }

    protected override IRenderable Build()
    {
        var logsPanel = new Panel("Logs");
        logsPanel.Expand = true;
        return logsPanel;
    }
}