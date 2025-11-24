// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Options for pushing container images to a registry.
/// </summary>
[Experimental("ASPIRECOMPUTE002", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
public sealed class ContainerImagePushOptions
{
    /// <summary>
    /// Gets or sets the remote image name (repository path without registry endpoint or tag).
    /// </summary>
    public string? RemoteImageName { get; set; }

    /// <summary>
    /// Gets or sets the remote image tag.
    /// </summary>
    public string? RemoteImageTag { get; set; }

    /// <summary>
    /// Gets the full remote image name including registry endpoint and tag.
    /// </summary>
    /// <param name="registry">The container registry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The full remote image name.</returns>
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
