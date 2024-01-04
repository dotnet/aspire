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
    /// <param name="binding">The <see cref="EndpointAnnotation"/> object to modify.</param>
    /// <returns>The modified <see cref="EndpointAnnotation"/> object.</returns>
    public static EndpointAnnotation AsHttp2(this EndpointAnnotation binding)
    {
        binding.Transport = "http2";
        binding.UriScheme = "https";
        return binding;
    }

    /// <summary>
    /// Sets the <see cref="EndpointAnnotation.IsExternal"/> property to true for the specified <see cref="EndpointAnnotation"/> object.
    /// </summary>
    /// <param name="binding">The <see cref="EndpointAnnotation"/> object to modify.</param>
    /// <returns>The modified <see cref="EndpointAnnotation"/> object.</returns>
    public static EndpointAnnotation AsExternal(this EndpointAnnotation binding)
    {
        binding.IsExternal = true;
        return binding;
    }
}
