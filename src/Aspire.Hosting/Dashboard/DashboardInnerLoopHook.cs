// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

internal sealed class DashboardInnerLoopHook(ILogger<DashboardInnerLoopHook> logger, DashboardServiceHost dashboardHost) : IDistributedApplicationLifecycleHook
{
    private readonly DashboardServiceHost _dashboardHost = dashboardHost;
    private readonly ILogger<DashboardInnerLoopHook> _logger = logger;

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (appModel.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is not { } dashboardResource)
        {
            _logger.LogDebug("Dashboard resource not found in app model. Skipping dashboard hook.");
            return Task.CompletedTask;
        }

        var innerloopLaunchProfile = new LaunchProfileAnnotation("innerloop");
        dashboardResource.Annotations.Add(innerloopLaunchProfile);

        // Don't publish the resource to the manifest.
        dashboardResource.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

        dashboardResource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is not { } appHostApplicationUrl)
            {
                throw new DistributedApplicationException("Dashboard inner loop hook failed to configure resource because ASPNETCORE_URLS environment variable was not set.");
            }

            if (Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL") is not { } otlpEndpointUrl)
            {
                throw new DistributedApplicationException("Dashboard inner loop hook failed to configure resource because DOTNET_DASHBOARD_OTLP_ENDPOINT_URL environment variable was not set.");
            }

            // Grab the resource service URL. We need to inject this into the resource.
            string grpcEndpointUrl;
            try
            {
                grpcEndpointUrl = _dashboardHost.GetResourceServiceUriAsync(cancellationToken).Result;
            }
            catch (Exception ex)
            {
                throw new DistributedApplicationException("Error getting the resource service URL.", ex);
            }

            context.EnvironmentVariables["ASPNETCORE_URLS"] = appHostApplicationUrl;
            context.EnvironmentVariables["DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"] = grpcEndpointUrl;
            context.EnvironmentVariables["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = otlpEndpointUrl;
        }));

        return Task.CompletedTask;
    }
}
