// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for providing a callback to programmatically modify Dockerfile builds.
/// </summary>
/// <param name="callback">The callback function that will be invoked during the Dockerfile build process.</param>
public class DockerfileBuildCallbackAnnotation(Func<DockerfileBuildCallbackContext, Task> callback) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback function that will be invoked during the Dockerfile build process.
    /// </summary>
    public Func<DockerfileBuildCallbackContext, Task> Callback => callback;
}