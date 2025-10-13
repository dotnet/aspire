// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for customizing a Dockerfile build.
/// </summary>
/// <param name="contextPath">The path to the context directory for the build. </param>
/// <param name="dockerfilePath">The path to the Dockerfile to use for the build.</param>
/// <param name="stage">The name of the build stage to use for the build.</param>
public class DockerfileBuildAnnotation(string contextPath, string dockerfilePath, string? stage) : IResourceAnnotation
{
    /// <summary>
    /// Gets the path to the context directory for the build.
    /// </summary>
    public string ContextPath => contextPath;

    /// <summary>
    /// Gets the path to the Dockerfile to use for the build.
    /// </summary>
    public string DockerfilePath => dockerfilePath;

    /// <summary>
    /// Gets the name of the build stage to use for the build.
    /// </summary>
    public string? Stage => stage;

    /// <summary>
    /// Gets the arguments to pass to the build.
    /// </summary>
    public Dictionary<string, object?> BuildArguments { get; } = [];

    /// <summary>
    /// Gets the secrets to pass to the build.
    /// </summary>
    public Dictionary<string, object> BuildSecrets { get; } = [];

    /// <summary>
    /// Gets or sets the factory function that generates Dockerfile content dynamically.
    /// When set, this factory will be invoked to generate the Dockerfile content at build time,
    /// and the content will be written to a generated file path.
    /// </summary>
    public Func<DockerfileFactoryContext, Task<string>>? DockerfileFactory { get; init; }

    /// <summary>
    /// Gets or sets the image name for the generated container image.
    /// When set, this will be used as the container image name instead of the value from ContainerImageAnnotation.
    /// </summary>
    public string? ImageName { get; set; }

    /// <summary>
    /// Gets or sets the image tag for the generated container image.
    /// When set, this will be used as the container image tag instead of the value from ContainerImageAnnotation.
    /// </summary>
    public string? ImageTag { get; set; }
}
