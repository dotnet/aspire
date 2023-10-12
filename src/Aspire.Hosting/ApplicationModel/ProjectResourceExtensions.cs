// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

public static class ProjectResourceExtensions
{
    public static IEnumerable<ProjectResource> GetProjectResources(this DistributedApplicationModel model)
    {
        return model.Resources.OfType<ProjectResource>();
    }

    public static bool TryGetProjectWithPath(this DistributedApplicationModel model, string path, [NotNullWhen(true)] out ProjectResource? projectResource)
    {
        projectResource = model.GetProjectResources().SingleOrDefault(p => p.Annotations.OfType<IServiceMetadata>().FirstOrDefault()?.ProjectPath == path);

        return projectResource is not null;
    }

    public static IServiceMetadata GetServiceMetadata(this ProjectResource projectResource)
    {
        return projectResource.Annotations.OfType<IServiceMetadata>().Single();
    }
}
