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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Backchannel;

internal class AppHostRpcTarget(
    ILogger<AppHostRpcTarget> logger,
    ResourceNotificationService resourceNotificationService,
    IServiceProvider serviceProvider,
    IDistributedApplicationEventing eventing,
    PublishingActivityProgressReporter activityReporter) 
{
    public async IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            var publishingActivity = await activityReporter.ActivitiyUpdated.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            yield return (
                publishingActivity.Id,
                publishingActivity.StatusMessage,
                publishingActivity.IsComplete,
                publishingActivity.IsError
            );

            if ( publishingActivity.IsPrimary &&(publishingActivity.IsComplete || publishingActivity.IsError))
            {
                // If the activity is complete or an error and it is the primary activity,
                // we can stop listening for updates.
                break;
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

    public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        logger.LogTrace("Received ping from CLI with timestamp: {Timestamp}", timestamp);
        return Task.FromResult(timestamp);
    }

    public Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync()
    {
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
            return Task.FromResult<(string, string?)>((baseUrlWithLoginToken, null));
        }
        else
        {
            return Task.FromResult((baseUrlWithLoginToken, codespacesUrlWithLoginToken));
        }
    }

    public async Task<string[]> GetPublishersAsync(CancellationToken cancellationToken)
    {
        var e = new PublisherAdvertisementEvent();
        await eventing.PublishAsync(e, cancellationToken).ConfigureAwait(false);

        var publishers = e.Advertisements.Select(x => x.Name);
        return [..publishers];
    }
}