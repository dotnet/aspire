// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    public override bool ProcessInput(ConsoleKey key)
    {
        throw new NotImplementedException();
    }

    protected override IRenderable Build()
    {
        return new RunSplashRenderable();
    }
}