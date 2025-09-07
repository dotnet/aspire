// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Yarp.Transforms;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Transforms;

namespace Aspire.Hosting.Yarp.Transforms;

/// <summary>
/// Extensions for adding path transforms.
/// </summary>
public static class PathTransformExtensions
{
    /// <summary>
    /// Adds the transform which sets the request path with the given value.
    /// </summary>
    public static YarpRoute WithTransformPathSet(this YarpRoute route, PathString path)
    {
        if (path.Value is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        route.Configure(r => r.WithTransformPathSet(path));

        return route;
    }

    /// <summary>
    /// Adds the transform which will prefix the request path with the given value.
    /// </summary>
    public static YarpRoute WithTransformPathPrefix(this YarpRoute route, PathString prefix)
    {
        if (prefix.Value is null)
        {
            throw new ArgumentNullException(nameof(prefix));
        }

        route.Configure(r => r.WithTransformPathPrefix(prefix));

        return route;
    }

    /// <summary>
    /// Adds the transform which will remove the matching prefix from the request path.
    /// </summary>
    public static YarpRoute WithTransformPathRemovePrefix(this YarpRoute route, PathString prefix)
    {
        if (prefix.Value is null)
        {
            throw new ArgumentNullException(nameof(prefix));
        }

        route.Configure(r => r.WithTransformPathRemovePrefix(prefix));

        return route;
    }

    /// <summary>
    /// Adds the transform which will set the request path with the given value.
    /// </summary>
    public static YarpRoute WithTransformPathRouteValues(this YarpRoute route, [StringSyntax("Route")] PathString pattern)
    {
        if (pattern.Value is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        route.Configure(r => r.WithTransformPathRouteValues(pattern));
        
        return route;
    }
}
