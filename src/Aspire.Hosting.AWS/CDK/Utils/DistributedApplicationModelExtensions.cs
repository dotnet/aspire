using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

internal static class DistributedApplicationModelExtensions
{
    public static T? TryFindParentOfType<T>(this IResourceWithParent resource)
        where T : IResource
    {
        var parentResource = resource.Parent;
        return parentResource switch
        {
            T resultResource => resultResource,
            IResourceWithParent parent => FindParentOfType<T>(parent),
            _ => default
        };
    }

    public static T FindParentOfType<T>(this IResourceWithParent resource)
        where T : IResource
    {
        return resource.TryFindParentOfType<T>() ??
               throw new ArgumentException($@"Resource with parent '{resource.GetType().FullName}' not found",
                   nameof(resource));
    }

    public static bool IsChildOfParent(this IResourceWithParent resource, IResource parent)
    {
        while (true)
        {
            if (resource.Parent == parent)
            {
                return true;
            }
            else if (resource.Parent is IResourceWithParent parentResource)
            {
                resource = parentResource;
                continue;
            }
            return false;
        }
    }

    public static IEnumerable<T> ListChildren<T>(this IResource parent, IEnumerable<T> resources)
        where T : IResourceWithParent
    {
        return resources.Where(resource => resource.IsChildOfParent(parent));
    }

    public static IEnumerable<T> ListParents<T>(this IResourceWithParent resource, IEnumerable<T> resources)
        where T : IResourceWithParent
    {
        return resources.Where(r => resource.IsChildOfParent(r));
    }

    public static IImmutableDictionary<TParent, IEnumerable<TChildren>> GetResourcesGroupedByParent<TParent, TChildren>(this IResourceCollection resources)
        where TParent : IResource
        where TChildren : IResourceWithParent
    {
        return resources.OfType<TChildren>().GroupBy(resource => resource.FindParentOfType<TParent>()).ToImmutableDictionary(group => group.Key, group => group.AsEnumerable());
    }
}
