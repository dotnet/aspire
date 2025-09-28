// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel.Docker;

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
    /// <param name="builder">The Dockerfile builder instance.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    public DockerfileBuildCallbackContext(string baseStageRepository, string? baseStageTag, string defaultContextPath, string? targetStage, DockerfileBuilder builder, IServiceProvider services)
    {
        BaseStageRepository = baseStageRepository;
        BaseStageTag = baseStageTag;
        DefaultContextPath = defaultContextPath;
        TargetStage = targetStage;
        Builder = builder;
        Services = services;
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

    /// <summary>
    /// Gets the Dockerfile builder instance.
    /// </summary>
    public DockerfileBuilder Builder { get; }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider Services { get; }
}