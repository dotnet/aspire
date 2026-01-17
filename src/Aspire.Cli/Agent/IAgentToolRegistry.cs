// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Aspire.Cli.Agent;

/// <summary>
/// Registry for agent tools that can be exposed to the Copilot SDK.
/// </summary>
internal interface IAgentToolRegistry
{
    /// <summary>
    /// Gets all available tools for the current context.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <returns>List of AI functions that can be invoked by the model.</returns>
    IList<AIFunction> GetTools(AgentContext context);
}
