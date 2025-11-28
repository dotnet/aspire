// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents options for pushing container images to a registry.
/// </summary>
/// <remarks>
/// This class allows customization of how container images are named and tagged when pushed to a container registry.
/// The <see cref="RemoteImageName"/> specifies the repository path (without registry endpoint or tag),
/// and <see cref="RemoteImageTag"/> specifies the tag to apply. Use <see cref="GetFullRemoteImageNameAsync"/>
/// to construct the complete image reference including registry endpoint and tag.
/// </remarks>
[Experimental("ASPIRECOMPUTE002", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
public sealed class ContainerImagePushOptions
{
    /// <summary>
    /// Gets or sets the remote image name (repository path without registry endpoint or tag).
    /// </summary>
    /// <value>
    /// The repository path for the image, such as "myapp" or "myorg/myapp".
    /// This should not include the registry endpoint (e.g., "mcr.microsoft.com") or the tag (e.g., ":latest").
    /// If not set explicitly, defaults to the resource name in lowercase.
    /// </value>
    public string? RemoteImageName { get; set; }

    /// <summary>
    /// Gets or sets the remote image tag.
    /// </summary>
    /// <value>
    /// The tag to apply to the image when pushed to the registry, such as "latest", "v1.0.0", or "dev".
    /// If not set explicitly, defaults to "latest".
    /// </value>
    public string? RemoteImageTag { get; set; }

    /// <summary>
    /// Gets the full remote image name including registry endpoint and tag.
    /// </summary>
    /// <param name="registry">The container registry to use for constructing the full image name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The fully qualified image name in the format "{registryEndpoint}/{RemoteImageName}:{RemoteImageTag}".
    /// For example: "myregistry.azurecr.io/myapp:v1.0.0".
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="RemoteImageName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> is <c>null</c>.</exception>
    /// <remarks>
    /// This method retrieves the registry endpoint asynchronously and combines it with the remote image name and tag.
    /// If <see cref="RemoteImageTag"/> is <c>null</c> or empty, "latest" is used as the default tag.
    /// </remarks>
    public async Task<string> GetFullRemoteImageNameAsync(
        IContainerRegistry registry,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(RemoteImageName))
        {
            throw new InvalidOperationException("RemoteImageName must be set.");
        }

        ArgumentNullException.ThrowIfNull(registry);

        var registryEndpoint = await registry.Endpoint
            .GetValueAsync(cancellationToken)
            .ConfigureAwait(false);

        var tag = string.IsNullOrEmpty(RemoteImageTag) ? "latest" : RemoteImageTag;
        return $"{registryEndpoint}/{RemoteImageName}:{tag}";
    }
}
