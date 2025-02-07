// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

public static class ResourceBuilderExtensions
{
    public static IResourceBuilder<T> WithModifiedEndpoints<T>(this IResourceBuilder<T> builder, Action<EndpointAnnotation> callback) where T : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints))
        {
            return builder;
        }

        foreach (var endpoint in endpoints)
        {
            callback(endpoint);
        }

        return builder;
    }
}
