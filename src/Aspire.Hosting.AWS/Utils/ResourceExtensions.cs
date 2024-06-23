// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS;

internal static class ResourceExtensions
{
    public static T? TrySelectParentResource<T>(this IResource resource) where T : IResource
        => resource switch
        {
            T ar => ar,
            IResourceWithParent rp => TrySelectParentResource<T>(rp.Parent),
            _ => default
        };

    public static T SelectParentResource<T>(this IResource resource)
        where T : IResource
        => resource.TrySelectParentResource<T>()
            ?? throw new ArgumentException(
                $@"Resource with parent '{resource.GetType().FullName}' not found",
                nameof(resource));
}
