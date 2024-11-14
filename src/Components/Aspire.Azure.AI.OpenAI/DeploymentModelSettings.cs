// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Azure.AI.OpenAI;

/// <summary>
/// Helper class to bind the deployment models from configuration (deployment names and model names).
/// More specifically, it binds the "Aspire:Azure:AI:OpenAI:{resourceName}:Models" section.
/// </summary>
internal sealed class DeploymentModelSettings
{
    /// <summary>
    /// Gets or sets the dictionary of deployment names and model names.
    /// </summary>
    /// <remarks>
    /// For instance <code>{ ["chat"] = "gpt-4o" }</code>.
    /// </remarks>
    public Dictionary<string, string> Models { get; set; } = [];
}
