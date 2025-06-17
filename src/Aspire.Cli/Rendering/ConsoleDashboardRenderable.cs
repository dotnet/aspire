// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal class ConsoleDashboardRenderable : FocusableRenderable
{
    private readonly ConsoleDashboardState _state;

    public ConsoleDashboardRenderable(ConsoleDashboardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    public override async Task FocusAsync(CancellationToken cancellationToken)
    {
        await _state.Updated.Writer.WriteAsync(true, cancellationToken);
    }

    public override async Task<FocusableRenderable> ProcessInputAsync(ConsoleKey key, CancellationToken cancellationToken)
    {
        if (key == ConsoleKey.UpArrow)
        {
            await _state.SelectPreviousResourceAsync(cancellationToken);
        }
        else if (key == ConsoleKey.DownArrow)
        {
            await _state.SelectNextResourceAsync(cancellationToken);
        }
        else if (key == ConsoleKey.Tab)
        {

        }

        return this;
    }

    public void MakeDirty()
    {
        MarkAsDirty();
    }

    protected override IRenderable Build()
    {
        var root = new Layout();

        var content = new Layout();

        var resourceListPanel = new ResourceListRenderable(_state);
        var resourceList = new Layout(resourceListPanel).Ratio(1);

        var detail = new Layout().Ratio(5);

        detail.SplitRows(
            new Layout(),
            new Layout()
        );

        // var resourceLogPanel = new ResourceLogRenderable(_state);
        // var resourceLog = new Layout(resourceLogPanel).Ratio(5);

        content.SplitColumns(resourceList, detail);

        var status = new Layout(new Text("status")).Size(1);

        root.SplitRows(content, status);

        return root;
    }
}