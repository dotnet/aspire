// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// An annotation that creates a pipeline step for a resource during deployment.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PipelineStepAnnotation"/> class.
/// </remarks>
/// <param name="factory">A factory function that creates the pipeline step.</param>
public class PipelineStepAnnotation(Func<DeployingContext, PipelineStep> factory) : IResourceAnnotation
{
    private readonly Func<DeployingContext, PipelineStep> _factory = factory;

    /// <summary>
    /// Creates a pipeline step using the provided deploying context.
    /// </summary>
    /// <param name="context">The deploying context.</param>
    /// <returns>The created pipeline step.</returns>
    public PipelineStep CreateStep(DeployingContext context) => _factory(context);
}
