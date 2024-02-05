// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;

namespace Aspire.Hosting.Dcp;

internal sealed class HostDashboardEndpointProvider : IDashboardEndpointProvider
{
    private readonly DashboardServiceHost _dashboardServiceHost;

    public HostDashboardEndpointProvider(DashboardServiceHost dashboardServiceHost)
    {
        _dashboardServiceHost = dashboardServiceHost;
    }

    public async Task<string> GetResourceServiceUriAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dashboardServiceHost.GetResourceServiceUriAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new DistributedApplicationException("Error getting the resource service URL.", ex);
        }
    }
}
