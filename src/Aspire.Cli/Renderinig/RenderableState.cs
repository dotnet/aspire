// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

namespace Aspire.Cli.Rendering;

internal abstract class RenderableState
{
    public Channel<bool> Updated { get; } = Channel.CreateBounded<bool>(1);
}