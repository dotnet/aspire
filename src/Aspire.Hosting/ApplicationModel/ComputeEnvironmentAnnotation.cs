// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that specifies which compute environment a resource should be deployed to.
/// </summary>
public sealed class ComputeEnvironmentAnnotation(IComputeEnvironmentResource computeEnvironment) : IResourceAnnotation
{
    /// <summary>
    /// Gets the compute environment that the resource should be deployed to.
    /// </summary>
    public IComputeEnvironmentResource ComputeEnvironment { get; } = computeEnvironment;
}
