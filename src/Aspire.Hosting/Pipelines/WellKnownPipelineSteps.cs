// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Defines well-known pipeline step names used in the deployment pipeline.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class WellKnownPipelineSteps
{
    /// <summary>
    /// The meta-step that coordinates all publish operations.
    /// All publish steps should be required by this step.
    /// </summary>
    public const string Publish = "publish";

    /// <summary>
    /// The meta-step that coordinates all deploy operations.
    /// All deploy steps should be required by this step.
    /// </summary>
    public const string Deploy = "deploy";

    /// <summary>
    /// The well-known step for prompting for parameters.
    /// </summary>

    public const string ParameterPrompt = "parameter-prompt";
}
