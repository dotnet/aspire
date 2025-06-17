// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tools;

/// <summary>
/// Represents the options for running the Aspire app host in tool mode.
/// </summary>
public class ToolOptions
{
    /// <summary>
    /// The name of the tool configuration section in the appsettings.json file.
    /// </summary>
    public const string Section = "Tool";

    /// <summary>
    /// Gets or set the target resource name for the tool execution.
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Gets or set the custom args to pass to the tool executable.
    /// </summary>
    public string[]? Args { get; set; }
}
