// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering.Dashboard;

internal sealed class AppHostLogRenderable(DashboardState state) : JustInTimeRenderable
{
    protected override IRenderable Build()
    {
        _ = state;
        var table = new Table().NoBorder();
        table.AddColumn("Stream");
        table.AddColumn("Message");

        foreach (var logEntry in state.AppHostLogs)
        {
            var stream = logEntry.Stream switch
            {
                "stdout" => new Markup("[white]stdout[/]"),
                "stderr" => new Markup("[bold red]stderr[/]"),
                _ => throw new InvalidOperationException($"Unknown stream type: {logEntry.Stream}")
            };

            table.AddRow(stream, new Text(logEntry.Message));
        }
        
        return table;
    }
}