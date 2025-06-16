// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal class RunExperimentalRenderable : FocusableRenderable
{
    private readonly RunExperimentalState _state;

    public RunExperimentalRenderable(RunExperimentalState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }
    public override void Focus()
    {
    }

    public override async Task ProcessInputAsync(ConsoleKey key, CancellationToken cancellationToken)
    {
        if (key == ConsoleKey.UpArrow)
        {
            await _state.SelectPreviousResourceAsync(cancellationToken);
        }
        else if (key == ConsoleKey.DownArrow)
        {
            await _state.SelectNextResourceAsync(cancellationToken);
        }
    }

    public void MakeDirty()
    {
        MarkAsDirty();
    }

    protected override IRenderable Build()
    {
        if (_state.StatusMessage is not null)
        {
            var resourcesTree = new Tree("Resources");

            foreach (var resource in _state.CliResources)
            {
                if (resource == _state.SelectedResource)
                {
                    resourcesTree.AddNode(new Markup($"[bold red]{resource.Name}[/]"));
                }
                else
                {
                    resourcesTree.AddNode(resource.Name);
                }
            }

            var logsPanel = new Panel("Logs");
            logsPanel.Expand();

            var layoutRoot = new Layout("Root")
                .SplitRows(
                    new Layout("Content")
                        .SplitColumns(
                            new Layout("Resources", resourcesTree),
                            new Layout("Logs", logsPanel)),
                    new Layout("Bottom", new StatusBarRenderable(_state.StatusMessage))
                );
            return layoutRoot;
        }
        else
        {
            return new RunSplashRenderable();
        }
    }
}