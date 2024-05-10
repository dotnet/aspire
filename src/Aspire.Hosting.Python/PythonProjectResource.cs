// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Python;

/// <summary>
/// A resource that represents a Python project.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="executablePath">The path to the executable used to run the python project.</param>
/// <param name="projectDirectory">The path to the directory containing the python project.</param>
public class PythonProjectResource(string name, string executablePath, string projectDirectory)
    : ExecutableResource(name, executablePath, projectDirectory), IResourceWithServiceDiscovery
{
    /// <summary>
    /// Writes out the publishing manifest for the python project.
    /// </summary>
    /// <param name="context">The manifest publishing context to extend.</param>
    internal async Task WriteDockerFileManifestAsync(ManifestPublishingContext context)
    {
        var dockerFilePath = Path.Combine(WorkingDirectory, "Dockerfile");
        var manifestRelativeDockerFilePath = context.GetManifestRelativePath(dockerFilePath);
        var manifestRelativeWorkingDirectory = context.GetManifestRelativePath(WorkingDirectory);

        if (!File.Exists(dockerFilePath))
        {
            throw new InvalidOperationException(
                "Dockerfile not found in project directory. Please provide a Dockerfile in the project directory.");
        }

        context.Writer.WriteString("type", "dockerfile.v0");
        context.Writer.WriteString("path", manifestRelativeDockerFilePath);
        context.Writer.WriteString("context", manifestRelativeWorkingDirectory);

        await context.WriteEnvironmentVariablesAsync(this).ConfigureAwait(false);

        context.WriteBindings(this);
    }
}
