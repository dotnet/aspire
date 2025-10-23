// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Represents the execution status of a pipeline step.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum PipelineStepStatus
{
    /// <summary>
    /// The step is waiting to start.
    /// </summary>
    Pending,

    /// <summary>
    /// The step is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// The step completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The step failed during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// The step was canceled before completion.
    /// </summary>
    Canceled,

    /// <summary>
    /// The step was skipped and not executed.
    /// </summary>
    Skipped
}
