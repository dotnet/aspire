// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Yarp.ReverseProxy.Transforms;

namespace Aspire.Hosting.Yarp.Transforms;

/// <summary>
/// Extensions for adding request header transforms.
/// </summary>
public static class RequestHeadersTransformExtensions
{
    /// <summary>
    /// Adds the transform which will enable or suppress copying request headers to the proxy request.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformCopyRequestHeaders", Description = "Exports WithTransformCopyRequestHeaders for polyglot app hosts.")]
    public static YarpRoute WithTransformCopyRequestHeaders(this YarpRoute route, bool copy = true)
    {
        route.Configure(r => r.WithTransformCopyRequestHeaders(copy));
        return route;
    }

    /// <summary>
    /// Adds the transform which will copy the incoming request Host header to the proxy request.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformUseOriginalHostHeader", Description = "Exports WithTransformUseOriginalHostHeader for polyglot app hosts.")]
    public static YarpRoute WithTransformUseOriginalHostHeader(this YarpRoute route, bool useOriginal = true)
    {
        route.Configure(r => r.WithTransformUseOriginalHostHeader(useOriginal));
        return route;
    }

    /// <summary>
    /// Adds the transform which will append or set the request header.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformRequestHeader", Description = "Exports WithTransformRequestHeader for polyglot app hosts.")]
    public static YarpRoute WithTransformRequestHeader(this YarpRoute route, string headerName, string value, bool append = true)
    {
        route.Configure(r => r.WithTransformRequestHeader(headerName, value, append));
        return route;
    }

    /// <summary>
    /// Adds the transform which will append or set the request header from a route value.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformRequestHeaderRouteValue", Description = "Exports WithTransformRequestHeaderRouteValue for polyglot app hosts.")]
    public static YarpRoute WithTransformRequestHeaderRouteValue(this YarpRoute route, string headerName, string routeValueKey, bool append = true)
    {
        route.Configure(r => r.WithTransformRequestHeaderRouteValue(headerName, routeValueKey, append));
        return route;
    }

    /// <summary>
    /// Adds the transform which will remove the request header.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformRequestHeaderRemove", Description = "Exports WithTransformRequestHeaderRemove for polyglot app hosts.")]
    public static YarpRoute WithTransformRequestHeaderRemove(this YarpRoute route, string headerName)
    {
        route.Configure(r => r.WithTransformRequestHeaderRemove(headerName));
        return route;
    }

    /// <summary>
    /// Adds the transform which will only copy the allowed request headers. Other transforms
    /// that modify or append to existing headers may be affected if not included in the allow list.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformRequestHeadersAllowed", Description = "Exports WithTransformRequestHeadersAllowed for polyglot app hosts.")]
    public static YarpRoute WithTransformRequestHeadersAllowed(this YarpRoute route, params string[] allowedHeaders)
    {
        route.Configure(r => r.WithTransformRequestHeadersAllowed(allowedHeaders));
        return route;
    }
}
