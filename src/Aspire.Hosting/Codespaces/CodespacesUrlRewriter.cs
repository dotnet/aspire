// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Codespaces;

internal class CodespacesUrlRewriter(ILogger<CodespacesUrlRewriter> logger, IConfiguration configuration, ResourceNotificationService resourceNotificationService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("CODESPACES", false))
        {
            logger.LogTrace("Not running in Codespaces, skipping URL rewriting.");
            return;
        }

        var gitHubCodespacesPortForwardingDomain = configuration.GetValue<string>("GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN") ?? throw new DistributedApplicationException("Codespaces was detected but GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN environment missing.");
        var codespaceName = configuration.GetValue<string>("CODESPACE_NAME") ?? throw new DistributedApplicationException("Codespaces was detected but CODESPACE_NAME environment missing.");

        do
        {
            var resourceEvents = resourceNotificationService.WatchAsync(stoppingToken);

            await foreach (var resourceEvent in resourceEvents.ConfigureAwait(false))
            {
                Dictionary<UrlSnapshot, UrlSnapshot>? remappedUrls = null;

                foreach (var originalUrlSnapshot in resourceEvent.Snapshot.Urls)
                {
                    var uri = new Uri(originalUrlSnapshot.Url);

                    if (!originalUrlSnapshot.IsInternal && (uri.Scheme == "http" || uri.Scheme == "https") && uri.Host == "localhost")
                    {
                        if (remappedUrls is null)
                        {
                            remappedUrls = new();
                        }

                        var newUrlSnapshot = originalUrlSnapshot with
                        {
                            Url = $"{uri.Scheme}://{codespaceName}-{uri.Port}.{gitHubCodespacesPortForwardingDomain}{uri.AbsolutePath}"
                        };

                        remappedUrls.Add(originalUrlSnapshot, newUrlSnapshot);
                    }
                }

                if (remappedUrls is not null)
                {
                    var transformedUrls = from originalUrl in resourceEvent.Snapshot.Urls
                                          select remappedUrls.TryGetValue(originalUrl, out var remappedUrl) ? remappedUrl : originalUrl;

                    await resourceNotificationService.PublishUpdateAsync(resourceEvent.Resource, resourceEvent.ResourceId, s => s with
                    {
                        Urls = transformedUrls.ToImmutableArray()
                    }).ConfigureAwait(false);
                }
            }

            // Short delay if we crash just to avoid spinning CPU.
            await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
        } while (!stoppingToken.IsCancellationRequested);
    }
}
