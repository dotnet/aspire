// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli;

internal class CliRpcTarget(ILogger<CliRpcTarget> logger, ConsoleAppModel appModel)
{
    public Task<long> PingAsync(long timestamp)
    {
        logger.LogTrace("Received ping from AppHost with timestamp: {Timestamp}", timestamp);
        appModel.LastPing = DateTimeOffset.Now;
        return Task.FromResult(timestamp);
    }

    public async Task UpdateDashboardUrlsAsync(string baseUrl, string loginUrl)
    {
        logger.LogDebug("Received dashboard base URL: {Url}", baseUrl);
        logger.LogDebug("Received dashboard login URL: {Url}", loginUrl);
        
        await UpdateResourceAsync(
            "aspire-dashboard",
            "Built-in",
            "Running",
            new[] { baseUrl }).ConfigureAwait(false);

        appModel.DashboardLoginUrl = loginUrl;
    }

    public Task UpdateResourceAsync(string resourceName, string resourceType, string resourceStatus, string[]? resourceUris)
    {
        logger.LogTrace(
            "Received resource update: {ResourceName}, {ResourceType}, {ResourceStatus}",
            resourceName,
            resourceType,
            resourceStatus
            );

        appModel.UpdateResource(resourceName, resourceType, resourceStatus, resourceUris);
        return Task.CompletedTask;
    }
}