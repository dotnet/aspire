// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A URL that should be displayed for a resource.
/// </summary>
public sealed class ResourceUrlAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The URL. When rendered as a link this will be used as the link target.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// The name of the URL. When rendered as a link this will be used as the linked text.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The endpoint associated with this URL. Can be <c>null</c> if this URL is not associated with an endpoint.
    /// </summary>
    public EndpointReference? Endpoint { get; set; }

    /// <summary>
    /// The display order the URL. Higher values mean sort higher in the list.
    /// </summary>
    public int? DisplayOrder;

    /// <summary>
    /// Converts a URL string into a <see cref="ResourceUrlAnnotation"/> instance.
    /// </summary>
    /// <param name="url">The URL.</param>
    public static implicit operator ResourceUrlAnnotation(string url) => new() { Url = url };

    /// <summary>
    /// Converts a <see cref="Uri"/> into a <see cref="ResourceUrlAnnotation"/> instance.
    /// </summary>
    /// <param name="uri">The URI.</param>
    public static implicit operator ResourceUrlAnnotation(Uri uri) => new() { Url = uri.ToString() };
}
