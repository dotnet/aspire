// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Model;

[DebuggerDisplay("Resource = {Resource}, Children = {Children.Count}, Depth = {Depth}, IsHidden = {IsHidden}")]
public sealed class ResourceGridViewModel
{
    public required ResourceViewModel Resource { get; init; }

    public List<ResourceGridViewModel> Children { get; } = [];
    public int Depth { get; set; }
    public bool IsHidden { get; set; }

    public bool IsCollapsed
    {
        get;
        set
        {
            field = value;
            UpdateHidden();
        }
    }

    private void UpdateHidden(bool isParentCollapsed = false)
    {
        IsHidden = isParentCollapsed;
        foreach (var child in Children)
        {
            child.UpdateHidden(isParentCollapsed || IsCollapsed);
        }
    }

    public static List<ResourceGridViewModel> OrderNestedResources(List<ResourceGridViewModel> initialGridVMs, Func<ResourceViewModel, bool> isCollapsed)
    {
        // This method loops over the list of grid view models to build the nested list.
        // Apps shouldn't have a huge number of resources so this shouldn't impact performance,
        // but if that changes then this method will need to be improved.

        var gridViewModels = new List<ResourceGridViewModel>();
        var depth = 0;

        foreach (var gridVM in initialGridVMs.Where(r => !HasParent(r)))
        {
            gridVM.Depth = depth;
            gridVM.IsCollapsed = isCollapsed(gridVM.Resource);
            gridVM.IsHidden = false;

            gridViewModels.Add(gridVM);

            AddChildViewModel(gridVM.Resource, gridVM, depth + 1, hidden: gridVM.IsCollapsed);
        }

        return gridViewModels;

        void AddChildViewModel(ResourceViewModel resource, ResourceGridViewModel parent, int depth, bool hidden)
        {
            foreach (var childGridVM in initialGridVMs.Where(r => r.Resource.GetResourcePropertyValue(KnownProperties.Resource.ParentName) == resource.Name))
            {
                childGridVM.Depth = depth;
                childGridVM.IsCollapsed = isCollapsed(childGridVM.Resource);
                childGridVM.IsHidden = hidden;

                parent.Children.Add(childGridVM);
                gridViewModels.Add(childGridVM);

                AddChildViewModel(childGridVM.Resource, childGridVM, depth + 1, hidden: childGridVM.IsHidden || childGridVM.IsCollapsed);
            }
        }

        bool HasParent(ResourceGridViewModel gridViewModel)
        {
            var parentName = gridViewModel.Resource.GetResourcePropertyValue(KnownProperties.Resource.ParentName);
            if (string.IsNullOrEmpty(parentName))
            {
                return false;
            }

            // Check that the name matches to a resource. Handle the situation where the parent resource is filtered out.
            return initialGridVMs.Any(r => r.Resource.Name == parentName);
        }
    }
}
