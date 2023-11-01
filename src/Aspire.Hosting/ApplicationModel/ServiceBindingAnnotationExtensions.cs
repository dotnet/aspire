// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="ServiceBindingAnnotation"/>.
/// </summary>
public static class ServiceBindingAnnotationExtensions
{
    /// <summary>
    /// Sets the transport to HTTP/2 and the URI scheme to HTTPS for the specified <see cref="ServiceBindingAnnotation"/> object.
    /// </summary>
    /// <param name="binding">The <see cref="ServiceBindingAnnotation"/> object to modify.</param>
    /// <returns>The modified <see cref="ServiceBindingAnnotation"/> object.</returns>
    public static ServiceBindingAnnotation AsHttp2(this ServiceBindingAnnotation binding)
    {
        binding.Transport = "http2";
        binding.UriScheme = "https";
        return binding;
    }

    /// <summary>
    /// Sets the <see cref="ServiceBindingAnnotation.IsExternal"/> property to true for the specified <see cref="ServiceBindingAnnotation"/> object.
    /// </summary>
    /// <param name="binding">The <see cref="ServiceBindingAnnotation"/> object to modify.</param>
    /// <returns>The modified <see cref="ServiceBindingAnnotation"/> object.</returns>
    public static ServiceBindingAnnotation AsExternal(this ServiceBindingAnnotation binding)
    {
        binding.IsExternal = true;
        return binding;
    }
}
