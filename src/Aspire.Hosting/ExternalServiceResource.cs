// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an external service resource with service discovery capabilities.
/// </summary>
public sealed class ExternalServiceResource : Resource, IResourceWithoutLifetime
{
    private readonly Uri? _uri;
    private readonly ParameterResource? _urlParameter;

    /// <summary>
    /// Creates a new instance of <see cref="ExternalServiceResource"/> with a specified name and URI.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="uri">The URI for the external service.</param>
    public ExternalServiceResource(string name, Uri uri) : base(name)
    {
        _uri = uri ?? throw new ArgumentNullException(nameof(uri), "The URI for the external service cannot be null.");

        if (!_uri.IsAbsoluteUri)
        {
            throw new ArgumentException("The URI for the external service must be absolute.", nameof(uri));
        }

        if (_uri.AbsolutePath != "/")
        {
            throw new ArgumentException("The URI absolute path must be \"/\".", nameof(uri));
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="ExternalServiceResource"/> with a specified name and URL parameter.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="urlParameter"></param>
    public ExternalServiceResource(string name, ParameterResource urlParameter) : base(name)
    {
        _urlParameter = urlParameter ?? throw new ArgumentNullException(nameof(urlParameter), "The URL parameter for the external service cannot be null.");
    }

    /// <summary>
    /// Gets the URI for the external service, if defined.
    /// </summary>
    /// <remarks>
    /// If <see cref="Uri"/> is <c>null</c>, the external service URL is parameterized and can be accessed via <see cref="UrlParameter"/>."/>
    /// </remarks>
    public Uri? Uri => _uri;

    /// <summary>
    /// Gets the URL parameter for the external service, if defined.
    /// </summary>
    /// <remarks>
    /// If <see cref="UrlParameter"/> is <c>null</c>, the external service URL is not parameterized and can be accessed directly via <see cref="Uri"/>.
    /// </remarks>
    public ParameterResource? UrlParameter => _urlParameter;
}
