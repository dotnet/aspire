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
    public static readonly string Publish = "publish";

    /// <summary>
    /// The prerequisite step that runs before any publish operations.
    /// </summary>
    public static readonly string PublishPrereq = "publish-prereq";

    /// <summary>
    /// The meta-step that coordinates all deploy operations.
    /// All deploy steps should be required by this step.
    /// </summary>
    public static readonly string Deploy = "deploy";

    /// <summary>
    /// The prerequisite step that runs before any deploy operations.
    /// </summary>
    public static readonly string DeployPrereq = "deploy-prereq";

    /// <summary>
    /// The well-known step for building resources.
    /// </summary>
    public static readonly string Build = "build";

    /// <summary>
    /// The prerequisite step that runs before any build operations.
    /// </summary>
    public static readonly string BuildPrereq = "build-prereq";

    /// <summary>
    /// The diagnostic step that dumps dependency graph information for troubleshooting.
    /// </summary>
    public static readonly string Diagnostics = "diagnostics";
}
