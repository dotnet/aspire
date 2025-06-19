// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering.Dashboard;

internal sealed class DashboardLinksRenderable(DashboardState state) : JustInTimeRenderable
{
    protected override IRenderable Build()
    {
        _ = state;
        var rows = new List<IRenderable>();
        rows.Add(new Markup($" [green bold]{InteractionServiceStrings.Dashboard}[/]:"));

        rows.Add(state.DirectDashboardUrl switch
        {
            { } directUrl => new Markup($" :chart_increasing:  {InteractionServiceStrings.DirectLink}: [link={directUrl}]{directUrl}[/]"),
            null => new Markup($" :chart_increasing:  {InteractionServiceStrings.DirectLink}: (pending)")
        });

        if (state.CodespacesDashboardUrl is { } codespacesUrl)
        {
            rows.Add(new Markup($" :chart_increasing:  {InteractionServiceStrings.CodespacesLink}: [link={codespacesUrl}]{codespacesUrl}[/]"));
        }

        return new Rows(rows.ToArray());
    }
}