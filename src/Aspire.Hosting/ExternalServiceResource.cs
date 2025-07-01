// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an external service resource with service discovery capabilities.
/// </summary>
public sealed class ExternalServiceResource : Resource, IResourceWithServiceDiscovery, IResourceWithoutLifetime
{
    /// <summary>
    /// Creates a new instance of <see cref="ExternalServiceResource"/> with a specified name and URL expression.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="urlExpression">The URL expression for the external service.</param>
    public ExternalServiceResource(string name, ReferenceExpression urlExpression) : base(name)
    {
        UrlExpression = urlExpression ?? throw new ArgumentNullException(nameof(urlExpression), "The URL expression for the external service cannot be null.");
    }

    /// <summary>
    /// Gets the URL expression for the external service.
    /// </summary>
    public ReferenceExpression UrlExpression { get; }

    internal static bool UrlIsValidForExternalService(string? url, [NotNullWhen(true)] out Uri? uri, [NotNullWhen(false)] out string? message)
    {
        if (url is null || !Uri.TryCreate(url, UriKind.Absolute, out uri))
        {
            uri = null;
            message = "The URL for the external service must be an absolute URI.";
            return false;
        }

        if (GetUriValidationException(uri) is { } exception)
        {
            message = exception.Message;
            uri = null;
            return false;
        }

        message = null;

        return true;
    }

    private static ArgumentException? GetUriValidationException(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
        {
            return new ArgumentException("The URI for the external service must be absolute.", nameof(uri));
        }
        if (uri.AbsolutePath != "/")
        {
            return new ArgumentException("The URI absolute path must be \"/\".", nameof(uri));
        }
        return null;
    }
}
