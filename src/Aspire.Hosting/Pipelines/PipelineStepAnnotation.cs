// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// An annotation that creates pipeline steps for a resource during deployment.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelineStepAnnotation : IResourceAnnotation
{
    private readonly Func<IEnumerable<PipelineStep>> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStepAnnotation"/> class.
    /// </summary>
    /// <param name="factory">A factory function that creates the pipeline step.</param>
    public PipelineStepAnnotation(Func<PipelineStep> factory)
    {
        _factory = () => [factory()];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStepAnnotation"/> class with a factory that creates multiple pipeline steps.
    /// </summary>
    /// <param name="factory">A factory function that creates multiple pipeline steps.</param>
    public PipelineStepAnnotation(Func<IEnumerable<PipelineStep>> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates pipeline steps.
    /// </summary>
    /// <returns>The created pipeline steps.</returns>
    public IEnumerable<PipelineStep> CreateSteps() => _factory();
}
