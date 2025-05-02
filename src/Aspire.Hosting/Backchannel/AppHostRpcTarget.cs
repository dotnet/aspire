// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Backchannel;

internal class AppHostRpcTarget(
    ILogger<AppHostRpcTarget> logger,
    ResourceNotificationService resourceNotificationService,
    IServiceProvider serviceProvider,
    IDistributedApplicationEventing eventing,
    PublishingActivityProgressReporter activityReporter,
    IHostApplicationLifetime lifetime,
    DistributedApplicationOptions options
    ) 
{
    public async IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            var publishingActivityStatus = await activityReporter.ActivityStatusUpdated.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (publishingActivityStatus == null)
            {
                // If the publishing activity is null, it means that the activity has been removed.
                // This can happen if the activity is complete or an error occurred.
                yield break;
            }

            yield return (
                publishingActivityStatus.Activity.Id,
                publishingActivityStatus.StatusText,
                publishingActivityStatus.IsComplete,
                publishingActivityStatus.IsError
            );

            if ( publishingActivityStatus.Activity.IsPrimary &&(publishingActivityStatus.IsComplete || publishingActivityStatus.IsError))
            {
                // If the activity is complete or an error and it is the primary activity,
                // we can stop listening for updates.
                yield break;
            }
        }
    }

    public async IAsyncEnumerable<(string Resource, string Type, string State, string[] Endpoints)> GetResourceStatesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        var resourceEvents = resourceNotificationService.WatchAsync(cancellationToken);

        await foreach (var resourceEvent in resourceEvents.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (resourceEvent.Resource.Name == "aspire-dashboard")
            {
                // Skip the dashboard resource, as it is handled separately.
                continue;
            }

            if (!resourceEvent.Resource.TryGetEndpoints(out var endpoints))
            {
                logger.LogTrace("Resource {Resource} does not have endpoints.", resourceEvent.Resource.Name);
                endpoints = Enumerable.Empty<EndpointAnnotation>();
            }
    
            var endpointUris = endpoints
                .Where(e => e.AllocatedEndpoint != null)
                .Select(e => e.AllocatedEndpoint!.UriString)
                .ToArray();
            // TODO: Decide on whether we want to define a type and share it between codebases for this.
            yield return (
                resourceEvent.Resource.Name,
                resourceEvent.Snapshot.ResourceType,
                resourceEvent.Snapshot.State?.Text ?? "Unknown",
                endpointUris
                );
        }
    }

    public Task RequestStopAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        lifetime.StopApplication();
        return Task.CompletedTask;
    }

    public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        logger.LogTrace("Received ping from CLI with timestamp: {Timestamp}", timestamp);
        return Task.FromResult(timestamp);
    }

    public Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync()
    {
        return GetDashboardUrlsAsync(CancellationToken.None);
    }

    public async Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        if (!options.DashboardEnabled)
        {
            logger.LogError("Dashboard URL requested but dashboard is disabled.");
            throw new InvalidOperationException("Dashboard URL requested but dashboard is disabled.");
        }

        // Wait for the dashboard to be healthy before returning the URL. This next statement has several
        // layers of hacks. Some to work around devcontainer/codespaces port forwarding behavior, and one to
        // temporarily work around the fact that resource events abuse the state to mark the resource as
        // hidden instead of having another field. There is a corresponding modification in the ResourceHealthService
        // which allows the dashboard resource to trigger health reports even though it never enters
        // the Running state. This is a hack. The reason we can't just check HealthStatus is because
        // the current implementation of HealthStatus depends on the state of the resource as well.
        await resourceNotificationService.WaitForResourceAsync(
            KnownResourceNames.AspireDashboard,
            re => re.Snapshot.HealthReports.All(h => h.Status == HealthStatus.Healthy),
            cancellationToken).ConfigureAwait(false);

        var dashboardOptions = serviceProvider.GetService<IOptions<DashboardOptions>>();

        if (dashboardOptions is null)
        {
            logger.LogWarning("Dashboard options not found.");
            throw new InvalidOperationException("Dashboard options not found.");
        }

        if (!StringUtils.TryGetUriFromDelimitedString(dashboardOptions.Value.DashboardUrl, ";", out var dashboardUri))
        {
            logger.LogWarning("Dashboard URL could not be parsed from dashboard options.");
            throw new InvalidOperationException("Dashboard URL could not be parsed from dashboard options.");            
        }

        var codespacesUrlRewriter = serviceProvider.GetService<CodespacesUrlRewriter>();

        var baseUrlWithLoginToken = $"{dashboardUri.GetLeftPart(UriPartial.Authority)}/login?t={dashboardOptions.Value.DashboardToken}";
        var codespacesUrlWithLoginToken = codespacesUrlRewriter?.RewriteUrl(baseUrlWithLoginToken);

        if (baseUrlWithLoginToken == codespacesUrlWithLoginToken)
        {
            return (baseUrlWithLoginToken, null);
        }
        else
        {
            return (baseUrlWithLoginToken, codespacesUrlWithLoginToken);
        }
    }

    public async Task<string[]> GetPublishersAsync(CancellationToken cancellationToken)
    {
        var e = new PublisherAdvertisementEvent();
        await eventing.PublishAsync(e, cancellationToken).ConfigureAwait(false);

        var publishers = e.Advertisements.Select(x => x.Name);
        return [..publishers];
    }

#pragma warning disable CA1822
    public Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        // The purpose of this API is to allow the CLI to determine what API surfaces
        // the AppHost supports. In 9.2 we'll be saying that you need a 9.2 apphost,
        // but the 9.3 CLI might actually support working with 9.2 apphosts. The idea
        // is that when the backchannel is established the CLI will call this API
        // and store the results. The "baseline.v0" capability is the bare minimum
        // that we need as of CLI version 9.2-preview*.
        //
        // Some capabilties will be opt in. For example in 9.3 we might refine the
        // publishing activities API to return more information, or add log streaming
        // features. So that would add a new capability that the apphsot can report
        // on initial backchannel negotiation and the CLI can adapt its behavior around
        // that. There may be scenarios where we need to break compataiblity at which
        // point we might increase the baseline version that the apphost reports.
        //
        // The ability to support a back channel at all is determined by the CLI by
        // making sure that the apphost version is at least > 9.2.

        _ = cancellationToken;
        return Task.FromResult(new string[] {
            "baseline.v0"
            });
    }
#pragma warning restore CA1822
}