// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Provides a service to publishers for building containers that represent a resource.
/// </summary>
public interface IResourceContainerImageBuilder
{
    /// <summary>
    /// Builds a container that represents the specified resource.
    /// </summary>
    /// <param name="resource">The resource to build.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>String representing the container image.</returns>
    Task<string> BuildImageAsync(IResource resource, CancellationToken cancellationToken);
}

internal sealed class ResourceContainerImageBuilder(ILogger<ResourceContainerImageBuilder> logger, IOptions<DcpOptions> dcpOptions, IServiceProvider serviceProvider) : IResourceContainerImageBuilder
{
    public async Task<string> BuildImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        logger.LogInformation("Building container image for resource {Resource}", resource.Name);

        if (resource is IProjectMetadata)
        {
            var image = await BuildProjectContainerImageAsync(resource, cancellationToken).ConfigureAwait(false);
            return image;
        }
        else if (resource is ContainerResource)
        {
            var image = await BuildContainerImageAsync(resource, cancellationToken).ConfigureAwait(false);
            return image;
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private Task<string> BuildProjectContainerImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<string> BuildContainerImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        var containerRuntime = dcpOptions.Value.ContainerRuntime switch
        {
            "podman" => serviceProvider.GetRequiredKeyedService<IContainerRuntime>("podman"),
            _ => serviceProvider.GetRequiredKeyedService<IContainerRuntime>("docker")
        };

        var image = await containerRuntime.BuildAsync(resource, cancellationToken).ConfigureAwait(false);
        return image;
    }
}