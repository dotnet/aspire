// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal class ResourceListRenderable(ConsoleDashboardState state) : FocusableRenderable
{
    public override async Task<FocusableRenderable> ProcessInputAsync(ConsoleKey key, CancellationToken cancellationToken)
    {
        await state.UpdateStatusAsync("Porcessed input in resource list", cancellationToken);
        return this;
    }

    public override async Task FocusAsync(CancellationToken cancellationToken)
    {
        await state.UpdateStatusAsync("Focused on resource list", cancellationToken);
    }

    protected override IRenderable Build()
    {
        var resourcesTree = new Tree("AppHost");

        foreach (var resource in state.CliResources.ToList())
        {
            if (resource == state.SelectedResource)
            {
                resourcesTree.AddNode(new Markup($"[bold red]{resource.ResourceName} ({resource.ResourceId})[/]"));
            }
            else
            {
                resourcesTree.AddNode(resource.ResourceName);
            }
        }

        var panel = new Panel(resourcesTree).RoundedBorder();
        panel.Expand = true;
        return panel;
    }
}