// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal class RunSplashRenderable : JustInTimeRenderable
{
    protected override IRenderable Build()
    {
        var text = new Markup("Made with [red]:red_heart:[/]\nby Aspire");
        var align = new Align(text, HorizontalAlignment.Center, VerticalAlignment.Middle);
        var layout = new Layout("Root", align);
        return layout;
    }
}