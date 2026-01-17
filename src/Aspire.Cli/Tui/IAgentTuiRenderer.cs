// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agent;

namespace Aspire.Cli.Tui;

/// <summary>
/// Renderer for the agent TUI experience.
/// </summary>
internal interface IAgentTuiRenderer
{
    /// <summary>
    /// Runs the TUI main loop.
    /// </summary>
    /// <param name="session">The agent session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RunAsync(IAgentSession session, CancellationToken cancellationToken);
}
