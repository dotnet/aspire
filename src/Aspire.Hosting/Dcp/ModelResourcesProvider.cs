// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dcp;

internal class ModelResourcesProvider
{
    private readonly DistributedApplicationModel _model;
    private readonly IEnumerable<IResourcesSelector> _resourcesSelectors;

    private bool _isFiltered;
    private IResourceCollection? _filteredResources;

    public IResourceCollection ModelResources => _model.Resources;

    public ModelResourcesProvider(
        DistributedApplicationModel model,
        IEnumerable<IResourcesSelector> resourcesSelectors)
    {
        _model = model;
        _resourcesSelectors = resourcesSelectors;
    }

    public IResourceCollection GetAllResources()
    {
        Filter();
        return _filteredResources!;
    }

    public IEnumerable<ExecutableResource> GetExecutableResources()
    {
        Filter();
        return _filteredResources!.OfType<ExecutableResource>();
    }

    public IEnumerable<ProjectResource> GetProjectResources()
    {
        Filter();
        return _filteredResources!.OfType<ProjectResource>();
    }

    public IEnumerable<IResource> GetContainerResources()
    {
        Filter();
        return _filteredResources!.GetContainerResources();
    }

    private void Filter()
    {
        if (_isFiltered)
        {
            return;
        }

        if (_resourcesSelectors is null || !_resourcesSelectors.Any())
        {
            _filteredResources = _model.Resources;
            _isFiltered = true;
            return;
        }

        IResourceCollection resources = new ResourceCollection(_model.Resources);
        foreach (var selector in _resourcesSelectors)
        {
            resources = selector.Select(resources);
        }
        _filteredResources = resources;
        _isFiltered = true;
    }
}
