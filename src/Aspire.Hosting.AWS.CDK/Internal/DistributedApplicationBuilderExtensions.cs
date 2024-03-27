// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

internal static class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<TDestination> AddResource<TSource, TDestination>(this IResourceBuilder<TSource> builder, Func<TSource, TDestination> resource)
        where TSource : IResource
        where TDestination : IResourceWithConstruct
    {
        return builder.ApplicationBuilder.AddResource(resource(builder.Resource));
    }

    public static IResourceBuilder<T>? FindResourceBuilder<T>(this IResourceBuilder<IResourceWithParent> builder)
        where T : IResource
    {
        var parentResource = builder.Resource.Parent;
        return parentResource switch
        {
            T resultResource => builder.ApplicationBuilder.CreateResourceBuilder(resultResource),
            IResourceWithParent parent => FindResourceBuilder<T>(builder.ApplicationBuilder.CreateResourceBuilder(parent)),
            _ => default
        };
    }
}
