// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Configuration;

internal sealed class ConfigureForwardedHeadersOptions(IOptionsMonitor<DashboardOptions> dashboardOptionsMonitor) : IConfigureOptions<ForwardedHeadersOptions>
{
    public void Configure(ForwardedHeadersOptions options)
    {
        var reverseProxyOptions = dashboardOptionsMonitor.CurrentValue.ReverseProxy;

        if (!reverseProxyOptions.ForwardHeaders)
        {
            return;
        }

        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
        options.AllowedHosts = reverseProxyOptions.AllowedHosts;
        options.ForwardLimit = reverseProxyOptions.ForwardLimit;
        options.KnownNetworks.Clear();
        foreach (var network in reverseProxyOptions.KnownNetworks)
        {
            options.KnownNetworks.Add(network);
        }
        options.KnownProxies.Clear();
        foreach (var proxy in reverseProxyOptions.KnownProxies)
        {
            options.KnownProxies.Add(proxy);
        }
    }
}
