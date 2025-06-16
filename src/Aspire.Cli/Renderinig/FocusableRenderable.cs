// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering;

internal abstract class FocusableRenderable : JustInTimeRenderable
{
    public abstract Task ProcessInputAsync(ConsoleKey key, CancellationToken cancellationToken);
    public abstract void Focus();
}