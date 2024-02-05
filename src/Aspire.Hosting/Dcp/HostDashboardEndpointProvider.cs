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

    public Task<string> GetResourceServiceUriAsync(CancellationToken cancellationToken = default)
    {
        return _dashboardServiceHost.GetResourceServiceUriAsync(cancellationToken);
    }
}
