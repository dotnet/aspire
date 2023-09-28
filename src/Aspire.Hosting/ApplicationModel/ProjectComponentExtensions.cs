// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

public static class ProjectComponentExtensions
{
    public static IEnumerable<ProjectComponent> GetProjectComponents(this DistributedApplicationModel model)
    {
        return model.Components.OfType<ProjectComponent>();
    }

    public static bool TryGetProjectWithPath(this DistributedApplicationModel model, string path, [NotNullWhen(true)] out ProjectComponent? projectComponent)
    {
        projectComponent = model.GetProjectComponents().SingleOrDefault(p => p.Annotations.OfType<IServiceMetadata>().FirstOrDefault()?.ProjectPath == path);

        return projectComponent is not null;
    }

    public static IServiceMetadata GetServiceMetadata(this ProjectComponent projectComponent)
    {
        return projectComponent.Annotations.OfType<IServiceMetadata>().Single();
    }
}
