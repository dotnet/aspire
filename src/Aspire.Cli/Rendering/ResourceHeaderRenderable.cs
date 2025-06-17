// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal class ResourceHeaderRenderable(ConsoleDashboardState state) : FocusableRenderable
{
    public override Task FocusAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task<FocusableRenderable> ProcessInputAsync(ConsoleKey key, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override IRenderable Build()
    {
        _ = state;
        var panel = new Panel("Resource Name").RoundedBorder();
        panel.Expand = true;
        return panel;
    }
}