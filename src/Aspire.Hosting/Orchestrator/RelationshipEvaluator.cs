// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Orchestrator;

internal static class RelationshipEvaluator
{
    public static ILookup<IResource, IResource> GetParentChildLookup(DistributedApplicationModel model)
    {
        static IResource? SelectParentContainerResource(IResource resource) => resource switch
        {
            IResourceWithParent rp => SelectParentContainerResource(rp.Parent),
            IResource r when r.IsContainer() => r,
            _ => null
        };

        // parent -> children lookup
        // Built from IResourceWithParent first, then from annotations.
        return model.Resources.OfType<IResourceWithParent>()
                              .Select(x => (Child: (IResource)x, Root: SelectParentContainerResource(x.Parent)))
                              .Where(x => x.Root is not null)
                              .Concat(GetParentChildRelationshipsFromAnnotations(model))
                              .ToLookup(x => x.Root!, x => x.Child);
    }

    private static IEnumerable<(IResource Child, IResource? Root)> GetParentChildRelationshipsFromAnnotations(DistributedApplicationModel model)
    {
        static bool TryGetParent(IResource resource, [NotNullWhen(true)] out IResource? parent)
        {
            if (resource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relations) &&
                   relations.LastOrDefault(r => r.Type == KnownRelationshipTypes.Parent) is { } parentRelationship)
            {
                parent = parentRelationship.Resource;
                return true;
            }

            parent = default;
            return false;
        }

        static IResource? SelectParentResource(IResource? resource) => resource switch
        {
            IResource r when TryGetParent(r, out var parent) => parent,
            _ => null
        };

        var result = model.Resources.Select(x => (Child: x, Parent: SelectParentResource(x)))
                                    .Where(x => x.Parent is not null)
                                    .ToArray();

        ValidateRelationships(result!);

        static IResource? SelectRootResource(IResource? resource) => resource switch
        {
            IResource r when TryGetParent(r, out var parent) => SelectRootResource(parent) ?? parent,
            _ => null
        };

        // translate the result to child -> root, which the dashboard expects
        return result.Select(x => (x.Child, Root: SelectRootResource(x.Child)));
    }

    private static void ValidateRelationships((IResource Child, IResource Parent)[] relationships)
    {
        if (relationships.Length == 0)
        {
            return;
        }

        var childToParentLookup = relationships.ToDictionary(x => x.Child, x => x.Parent);

        // ensure no circular dependencies
        var visited = new Stack<IResource>();
        foreach (var relation in relationships)
        {
            ValidateNoCircularDependencies(childToParentLookup, relation.Child, visited);
        }

        static void ValidateNoCircularDependencies(Dictionary<IResource, IResource> childToParentLookup, IResource child, Stack<IResource> visited)
        {
            visited.Push(child);
            if (childToParentLookup.TryGetValue(child, out var parent))
            {
                if (visited.Contains(parent))
                {
                    throw new InvalidOperationException($"Circular dependency detected: {string.Join(" -> ", visited)} -> {parent}");
                }
                ValidateNoCircularDependencies(childToParentLookup, parent, visited);
            }
            visited.Pop();
        }
    }
}
