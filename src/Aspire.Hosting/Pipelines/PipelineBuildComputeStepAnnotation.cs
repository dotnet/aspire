// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// An annotation that can be applied to a resource to indicate that it has its own
/// pipeline build step that produces a compute image.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelineBuildComputeStepAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the step that builds the compute image.
    /// </summary>
    public required string StepName { get; init; }
}
