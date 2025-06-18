// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering.Dashboard;

internal sealed class DashboardRenderable(DashboardState state) : JustInTimeRenderable
{
    protected override bool HasDirtyChildren()
    {
        return true;
    }

    protected override IRenderable Build()
    {
        if (!state.ShowAppHostLogs)
        {
            return new ResourceTableRenderable(state);
        }
        else
        {
            return new AppHostLogRenderable(state);
        }
    }
}