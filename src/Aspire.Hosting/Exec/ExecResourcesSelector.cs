// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class ExecResourcesSelector : IResourcesSelector
{
    private readonly ExecOptions _execOptions;

    public ExecResourcesSelector(IOptions<ExecOptions> execOptions)
    {
        _execOptions = execOptions.Value;
    }

    public IResourceCollection Select(IResourceCollection resources)
    {
        if (_execOptions.Resource is null)
        {
            // execution in the context of AppHost is the only option
            return resources;
        }

        var targetResource = resources.FirstOrDefault(r => r.Name == _execOptions.Resource);
        if (targetResource is null)
        {
            // resource is not found - maybe show it in logs? Or throw exception?
            // i guess we can blindly return model resources as default option for now
            return resources;
        }

        var result = new ResourceCollection { targetResource };

        // we also need to find a tree of dependencies. The root is target resource.
        FindAndAddDependencies(targetResource);

        return result;

        void FindAndAddDependencies(IResource resource)
        {
            if (resource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships))
            {
                foreach (var relationship in relationships)
                {
                    if (relationship.Type == KnownRelationshipTypes.Reference)
                    {
                        result.Add(relationship.Resource);
                        FindAndAddDependencies(relationship.Resource);
                    }
                }
            }
        }
    }
}
