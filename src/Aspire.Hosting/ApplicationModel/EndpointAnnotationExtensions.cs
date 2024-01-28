// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="EndpointAnnotation"/>.
/// </summary>
public static class EndpointAnnotationExtensions
{
    /// <summary>
    /// Sets the transport to HTTP/2 and the URI scheme to HTTPS for the specified <see cref="EndpointAnnotation"/> object.
    /// </summary>
    /// <param name="endpoint">The <see cref="EndpointAnnotation"/> object to modify.</param>
    /// <returns>The modified <see cref="EndpointAnnotation"/> object.</returns>
    public static EndpointAnnotation AsHttp2(this EndpointAnnotation endpoint)
    {
        endpoint.Transport = "http2";
        endpoint.UriScheme = "https";
        return endpoint;
    }

    /// <summary>
    /// Sets the <see cref="EndpointAnnotation.IsExternal"/> property to true for the specified <see cref="EndpointAnnotation"/> object.
    /// </summary>
    /// <param name="endpoint">The <see cref="EndpointAnnotation"/> object to modify.</param>
    /// <returns>The modified <see cref="EndpointAnnotation"/> object.</returns>
    public static EndpointAnnotation AsExternal(this EndpointAnnotation endpoint)
    {
        endpoint.IsExternal = true;
        return endpoint;
    }
}
