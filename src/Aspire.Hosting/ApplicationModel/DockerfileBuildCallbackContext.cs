// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides context information for Dockerfile build callbacks.
/// </summary>
public class DockerfileBuildCallbackContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DockerfileBuildCallbackContext"/> class.
    /// </summary>
    /// <param name="baseStageRepository">The repository of the base stage.</param>
    /// <param name="baseStageTag">The tag of the base stage.</param>
    /// <param name="defaultContextPath">The default context path for the build.</param>
    /// <param name="targetStage">The target stage for the build.</param>
    public DockerfileBuildCallbackContext(string baseStageRepository, string? baseStageTag, string defaultContextPath, string? targetStage)
    {
        BaseStageRepository = baseStageRepository;
        BaseStageTag = baseStageTag;
        DefaultContextPath = defaultContextPath;
        TargetStage = targetStage;
    }

    /// <summary>
    /// Gets the repository of the base stage.
    /// </summary>
    public string BaseStageRepository { get; }

    /// <summary>
    /// Gets the tag of the base stage.
    /// </summary>
    public string? BaseStageTag { get; }

    /// <summary>
    /// Gets the default context path for the build.
    /// </summary>
    public string DefaultContextPath { get; }

    /// <summary>
    /// Gets the target stage for the build.
    /// </summary>
    public string? TargetStage { get; }
}