// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Assistant;

/// <summary>
/// Configuration options for the AI agent.
/// </summary>
public sealed class AgentOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Agent";

    /// <summary>
    /// Path to the Copilot CLI executable. If null, uses "copilot" from PATH.
    /// </summary>
    public string? CliPath { get; set; }

    /// <summary>
    /// URL of an external Copilot CLI server to connect to.
    /// When set, the dashboard will connect to this server instead of spawning its own CLI process.
    /// </summary>
    public string? ExternalServerUrl { get; set; }

    /// <summary>
    /// Whether to enable the AI agent. Defaults to true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The default model to use. If null, uses the first available model.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Whether to use an external server (when ExternalServerUrl is set) or spawn a local CLI process.
    /// </summary>
    public bool UseExternalServer => !string.IsNullOrEmpty(ExternalServerUrl);
}
