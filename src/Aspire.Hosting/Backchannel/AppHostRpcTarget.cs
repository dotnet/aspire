// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Exec;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Backchannel;

internal class AppHostRpcTarget(
    ILogger<AppHostRpcTarget> logger,
    ResourceNotificationService resourceNotificationService,
    IServiceProvider serviceProvider,
    PublishingActivityReporter activityReporter,
    IHostApplicationLifetime lifetime,
    DistributedApplicationOptions options)
{
    private readonly TaskCompletionSource<Channel<BackchannelLogEntry>> _logChannelTcs = new();

    public void RegisterLogChannel(Channel<BackchannelLogEntry> channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _logChannelTcs.TrySetResult(channel);
    }

    public async IAsyncEnumerable<BackchannelLogEntry> GetAppHostLogEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = await _logChannelTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        var logEntries = channel.Reader.ReadAllAsync(cancellationToken);

        await foreach (var logEntry in logEntries.WithCancellation(cancellationToken))
        {
            // If the log entry is null, terminate the stream
            if (logEntry == null)
            {
                yield break;
            }

            yield return logEntry;
        }
    }

    public async IAsyncEnumerable<PublishingActivity> GetPublishingActivitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            var publishingActivity = await activityReporter.ActivityItemUpdated.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            // Terminate the stream if the publishing activity is null
            if (publishingActivity == null)
            {
                yield break;
            }

            yield return publishingActivity;
        }
    }

    public async IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
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

            // Compute health status
            var healthStatus = CustomResourceSnapshot.ComputeHealthStatus(resourceEvent.Snapshot.HealthReports, resourceEvent.Snapshot.State?.Text);

            yield return new RpcResourceState
            {
                Resource = resourceEvent.Resource.Name,
                Type = resourceEvent.Snapshot.ResourceType,
                State = resourceEvent.Snapshot.State?.Text ?? "Unknown",
                Endpoints = endpointUris,
                Health = healthStatus?.ToString()
            };
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

    public Task<DashboardUrlsState> GetDashboardUrlsAsync()
    {
        return GetDashboardUrlsAsync(CancellationToken.None);
    }

    public async Task<DashboardUrlsState> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        if (!options.DashboardEnabled)
        {
            logger.LogError("Dashboard URL requested but dashboard is disabled.");
            throw new InvalidOperationException("Dashboard URL requested but dashboard is disabled.");
        }

        // Wait for the dashboard to be healthy before returning the URL. This is to ensure that the
        // endpoint for the resource is available and the dashboard is ready to be used. This helps
        // avoid some issues with port forwarding in devcontainer/codespaces scenarios.
        await resourceNotificationService.WaitForResourceHealthyAsync(
            KnownResourceNames.AspireDashboard,
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
            return new DashboardUrlsState
            {
                BaseUrlWithLoginToken = baseUrlWithLoginToken
            };
        }
        else
        {
            return new DashboardUrlsState
            {
                BaseUrlWithLoginToken = baseUrlWithLoginToken,
                CodespacesUrlWithLoginToken = codespacesUrlWithLoginToken
            };
        }
    }

    public async IAsyncEnumerable<CommandOutput> ExecAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var execResourceManager = serviceProvider.GetRequiredService<ExecResourceManager>();
        var logsStream = execResourceManager.StreamExecResourceLogs(cancellationToken);
        await foreach (var commandOutput in logsStream.ConfigureAwait(false))
        {
            yield return commandOutput;
        }
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
            "baseline.v2"
            });
    }
#pragma warning restore CA1822

    public async Task CompletePromptResponseAsync(string promptId, string?[] answers, CancellationToken cancellationToken = default)
    {
        await activityReporter.CompleteInteractionAsync(promptId, answers, cancellationToken).ConfigureAwait(false);
    }
}
