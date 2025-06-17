// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal class StatusBarRenderable(ConsoleDashboardState state) : JustInTimeRenderable
{
    protected override IRenderable Build()
    {
        var status = new Markup($" {state.StatusMessage ?? "Unknown"}");
        var statusAlign = new Align(status, HorizontalAlignment.Left, VerticalAlignment.Bottom);
        var easterEgg = new Markup(":rocket::womans_boot:");
        var easterEggAlign = new Align(easterEgg, HorizontalAlignment.Right, VerticalAlignment.Bottom);
        var layout = new Layout("Root").SplitColumns(
            new Layout("Status", statusAlign),
            new Layout("EasterEgg", easterEggAlign)
        );
        return layout;
    }
}