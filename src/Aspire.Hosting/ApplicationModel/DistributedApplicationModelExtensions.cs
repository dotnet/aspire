// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods on the <see cref="DistributedApplicationModel"/> class.
/// </summary>
public static class DistributedApplicationModelExtensions
{
    /// <summary>
    /// Returns the compute resources from the <see cref="DistributedApplicationModel"/>.
    /// Compute resources are those that are either containers or project resources, and are not marked to be ignored by the manifest publishing callback annotation.
    /// </summary>
    /// <param name="model">The distributed application model to extract compute resources from.</param>
    /// <returns>An enumerable of compute <see cref="IResource"/> in the model.</returns>
    public static IEnumerable<IResource> GetComputeResources(this DistributedApplicationModel model)
    {
        foreach (var r in model.Resources)
        {
            if (r.IsExcludedFromPublish())
            {
                continue;
            }

            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            yield return r;
        }
    }
}
