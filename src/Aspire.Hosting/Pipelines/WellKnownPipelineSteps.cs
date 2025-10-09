// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Defines well-known pipeline step names used in the deployment process.
/// </summary>
public static class WellKnownPipelineSteps
{
    /// <summary>
    /// The step that provisions infrastructure resources.
    /// </summary>
    public const string ProvisionInfrastructure = "provision-infra";

    /// <summary>
    /// The step that builds container images.
    /// </summary>
    public const string BuildImages = "build-images";

    /// <summary>
    /// The step that deploys to compute infrastructure.
    /// </summary>
    public const string DeployCompute = "deploy-compute";
}
