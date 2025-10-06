// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a step in a deployment pipeline.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelineStep
{
    /// <summary>
    /// Gets or sets the name of the pipeline step.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of step names that this step depends on.
    /// </summary>
    public List<string> Dependencies { get; } = [];

    /// <summary>
    /// Gets or sets the action to execute for this step.
    /// </summary>
    public Func<DeployingContext, Task>? Action { get; set; }
}
