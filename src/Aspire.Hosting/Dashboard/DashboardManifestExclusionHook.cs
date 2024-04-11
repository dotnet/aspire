// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Dashboard;

internal sealed class DashboardManifestExclusionHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        // When developing locally, exclude the dashboard from the manifest. This only affects our playground projects in practice.
        if (model.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is { } dashboardResource)
        {
            dashboardResource.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);
        }

        return Task.CompletedTask;
    }
}
