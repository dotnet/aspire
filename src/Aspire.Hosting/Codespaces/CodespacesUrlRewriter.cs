// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Codespaces;

internal sealed class CodespacesUrlRewriter(ILogger<CodespacesUrlRewriter> logger, IConfiguration configuration, ResourceNotificationService resourceNotificationService) : BackgroundService
{
    private const string CodespacesEnvironmentVariable = "CODESPACES";
    private const string CodespaceNameEnvironmentVariable = "CODESPACE_NAME";
    private const string GitHubCodespacesPortForwardingDomain = "GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN";

    private string GetRequiredCodespacesConfigurationValue(string key)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        return configuration.GetValue<string>(key) ?? throw new DistributedApplicationException($"Codespaces was detected but {key} environment missing.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>(CodespacesEnvironmentVariable, false))
        {
            logger.LogTrace("Not running in Codespaces, skipping URL rewriting.");
            return;
        }

        var gitHubCodespacesPortForwardingDomain = GetRequiredCodespacesConfigurationValue(GitHubCodespacesPortForwardingDomain);
        var codespaceName = GetRequiredCodespacesConfigurationValue(CodespaceNameEnvironmentVariable);

        do
        {
            try
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
                            remappedUrls ??= new();

                            var newUrlSnapshot = originalUrlSnapshot with
                            {
                                // The format of GitHub Codespaces URLs comprises the codespace
                                // name (from the CODESPACE_NAME environment variable, the port,
                                // and the port forwarding domain (via GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN
                                // which is typically ".app.github.dev". The VSCode instance is typically
                                // hosted at codespacename.github.dev whereas the forwarded ports
                                // would be at codespacename-port.app.github.dev.
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
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // When debugging sometimes we'll get cancelled here but we don't want
                // to tear down the loop. We only want to crash out when the service's
                // cancellation token is signaled.
                logger.LogTrace(ex, "Codespace URL rewriting loop threw an exception but was ignored.");
            }
        } while (!stoppingToken.IsCancellationRequested);
    }
}
