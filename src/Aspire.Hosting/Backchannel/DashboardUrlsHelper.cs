// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// Helper class for retrieving dashboard URLs with login tokens.
/// </summary>
internal static class DashboardUrlsHelper
{
    /// <summary>
    /// Gets the dashboard URLs including the login token.
    /// Waits for the dashboard to become healthy before returning.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The Dashboard URLs state including health and login URLs.</returns>
    public static async Task<DashboardUrlsState> GetDashboardUrlsAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var resourceNotificationService = serviceProvider.GetRequiredService<ResourceNotificationService>();

        // Wait for the dashboard to be healthy before returning the URL. This is to ensure that the
        // endpoint for the resource is available and the dashboard is ready to be used. This helps
        // avoid some issues with port forwarding in devcontainer/codespaces scenarios.
        try
        {
            await resourceNotificationService.WaitForResourceHealthyAsync(
                KnownResourceNames.AspireDashboard,
                WaitBehavior.StopOnResourceUnavailable,
                cancellationToken).ConfigureAwait(false);
        }
        catch (DistributedApplicationException ex)
        {
            logger.LogWarning(ex, "An error occurred while waiting for the Aspire Dashboard to become healthy.");

            return new DashboardUrlsState
            {
                DashboardHealthy = false,
                BaseUrlWithLoginToken = null,
                CodespacesUrlWithLoginToken = null
            };
        }

        var dashboardOptions = serviceProvider.GetService<IOptions<DashboardOptions>>();

        if (dashboardOptions is null)
        {
            logger.LogWarning("Dashboard options not found.");
            throw new InvalidOperationException("Dashboard options not found.");
        }

        // Get the actual allocated URL from the dashboard resource endpoint
        var appModel = serviceProvider.GetService<DistributedApplicationModel>();
        string? dashboardUrl = null;

        if (appModel?.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is IResourceWithEndpoints dashboardResource)
        {
            // Try HTTPS first, then HTTP
            var httpsEndpoint = dashboardResource.GetEndpoint("https");
            var httpEndpoint = dashboardResource.GetEndpoint("http");

            var endpoint = httpsEndpoint.Exists ? httpsEndpoint : httpEndpoint;
            if (endpoint.Exists)
            {
                dashboardUrl = await endpoint.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // Fall back to configured URL if we couldn't get it from the resource
        if (string.IsNullOrEmpty(dashboardUrl))
        {
            if (!StringUtils.TryGetUriFromDelimitedString(dashboardOptions.Value.DashboardUrl, ";", out var dashboardUri))
            {
                logger.LogWarning("Dashboard URL could not be parsed from dashboard options.");
                throw new InvalidOperationException("Dashboard URL could not be parsed from dashboard options.");
            }
            dashboardUrl = dashboardUri.GetLeftPart(UriPartial.Authority);
        }

        var codespacesUrlRewriter = serviceProvider.GetService<CodespacesUrlRewriter>();

        var baseUrlWithLoginToken = $"{dashboardUrl.TrimEnd('/')}/login?t={dashboardOptions.Value.DashboardToken}";
        var codespacesUrlWithLoginToken = codespacesUrlRewriter?.RewriteUrl(baseUrlWithLoginToken);

        if (baseUrlWithLoginToken == codespacesUrlWithLoginToken)
        {
            return new DashboardUrlsState
            {
                DashboardHealthy = true,
                BaseUrlWithLoginToken = baseUrlWithLoginToken,
                CodespacesUrlWithLoginToken = null
            };
        }
        else
        {
            return new DashboardUrlsState
            {
                DashboardHealthy = true,
                BaseUrlWithLoginToken = baseUrlWithLoginToken,
                CodespacesUrlWithLoginToken = codespacesUrlWithLoginToken
            };
        }
    }
}
