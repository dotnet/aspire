// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yarp.Transforms;
using Yarp.ReverseProxy.Transforms;

namespace Aspire.Hosting.Yarp.Transforms;

/// <summary>
/// Extensions for adding query transforms.
/// </summary>
public static class QueryTransformExtensions
{
    /// <summary>
    /// Adds the transform that will append or set the query parameter from the given value.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformQueryValue", Description = "Exports WithTransformQueryValue for polyglot app hosts.")]
    public static YarpRoute WithTransformQueryValue(this YarpRoute route, string queryKey, string value, bool append = true)
    {
        route.Configure(r => r.WithTransformQueryValue(queryKey, value, append));
        return route;
    }

    /// <summary>
    /// Adds the transform that will append or set the query parameter from a route value.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformQueryRouteValue", Description = "Exports WithTransformQueryRouteValue for polyglot app hosts.")]
    public static YarpRoute WithTransformQueryRouteValue(this YarpRoute route, string queryKey, string routeValueKey, bool append = true)
    {
        route.Configure(r => r.WithTransformQueryRouteValue(queryKey, routeValueKey, append));
        return route;
    }

    /// <summary>
    /// Adds the transform that will remove the given query key.
    /// </summary>
    [global::Aspire.Hosting.AspireExport("withTransformQueryRemoveKey", Description = "Exports WithTransformQueryRemoveKey for polyglot app hosts.")]
    public static YarpRoute WithTransformQueryRemoveKey(this YarpRoute route, string queryKey)
    {
        route.Configure(r => r.WithTransformQueryRemoveKey(queryKey));
        return route;
    }
}
