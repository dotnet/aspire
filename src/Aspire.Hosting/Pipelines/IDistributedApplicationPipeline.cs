// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Represents a pipeline for executing deployment steps in a distributed application.
/// </summary>
public interface IDistributedApplicationPipeline
{
    /// <summary>
    /// Adds a deployment step to the pipeline.
    /// </summary>
    /// <param name="name">The unique name of the step.</param>
    /// <param name="action">The action to execute for this step.</param>
    /// <param name="dependsOn">The name of the step this step depends on, if any.</param>
    /// <param name="requiredBy">The name of the step that requires this step, if any.</param>
    void AddStep(string name,
                 Func<DeployingContext, Task> action,
                 string? dependsOn = null,
                 string? requiredBy = null);

    /// <summary>
    /// Adds a deployment step to the pipeline.
    /// </summary>
    /// <param name="step">The pipeline step to add.</param>
    void AddStep(PipelineStep step);

    /// <summary>
    /// Executes all steps in the pipeline in dependency order.
    /// </summary>
    /// <param name="context">The deploying context for the execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(DeployingContext context);
}
