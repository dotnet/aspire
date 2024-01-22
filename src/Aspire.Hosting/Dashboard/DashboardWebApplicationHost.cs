// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Hosts an instance of the Dashboard web application. Starts and stops it along with the parent application.
/// </summary>
internal sealed class DashboardWebApplicationHost : IHostedService
{
    // TODO move or remove this whole class when the app host stops hosting the dashboard web app

    private readonly DashboardWebApplication? _dashboardWebApp;

    public DashboardWebApplicationHost(
        ILoggerFactory loggerFactory,
        DistributedApplicationOptions options,
        IOptions<PublishingOptions> publishingOptions)
    {
        if (!options.DashboardEnabled)
        {
            // Dashboard was explicitly disabled
        }
        else if (publishingOptions.Value.Publisher == "manifest")
        {
            // HACK: Manifest publisher check is temporary until DcpHostService is integrated with DcpPublisher.
        }
        else
        {
            var dashboardLogger = loggerFactory.CreateLogger<DashboardWebApplication>();

            _dashboardWebApp = new DashboardWebApplication(dashboardLogger);
        }
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        if (_dashboardWebApp is not null)
        {
            await _dashboardWebApp.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        if (_dashboardWebApp is not null)
        {
            await _dashboardWebApp.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
