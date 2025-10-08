// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Utils;

internal static class DockerfileHelper
{
    /// <summary>
    /// Executes the dockerfile factory if present and writes the generated content to the specified path.
    /// </summary>
    /// <param name="annotation">The dockerfile build annotation containing the factory.</param>
    /// <param name="resource">The resource for which the dockerfile is being generated.</param>
    /// <param name="serviceProvider">The service provider to be passed to the factory context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ExecuteDockerfileFactoryAsync(
        DockerfileBuildAnnotation annotation,
        IResource resource,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (annotation.DockerfileFactory is not null)
        {
            var context = new DockerfileFactoryContext
            {
                Services = serviceProvider,
                Resource = resource,
                CancellationToken = cancellationToken
            };
            var dockerfileContent = await annotation.DockerfileFactory(context).ConfigureAwait(false);
            await File.WriteAllTextAsync(annotation.DockerfilePath, dockerfileContent, cancellationToken).ConfigureAwait(false);
        }
    }
}
