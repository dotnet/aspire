// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Lifecycle;

internal sealed class LaunchEndpointUrlLifecycleHook(
    EndpointReference endpoint,
    string relativeUrl)
    : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo()
        {
            FileName = new Uri(new Uri(endpoint.Url), relativeUrl).ToString(),
            UseShellExecute = true
        };

        Process.Start(startInfo);
        return Task.CompletedTask;
    }
}
