// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

/// <summary>
/// Health check for EF migration resources that reports healthy only when migrations have completed.
/// </summary>
internal sealed class EFMigrationHealthCheck(
    string resourceId,
    ResourceNotificationService resourceNotificationService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Get the current resource state by trying to find the resource snapshot
        if (resourceNotificationService.TryGetCurrentState(resourceId, out var resourceEvent) && resourceEvent != null)
        {
            var stateText = resourceEvent.Snapshot.State?.Text;

            return stateText switch
            {
                "Active" => Task.FromResult(HealthCheckResult.Healthy("Migrations completed successfully.")),
                "Running" => Task.FromResult(HealthCheckResult.Unhealthy("Migrations are currently running.")),
                "FailedToStart" => Task.FromResult(HealthCheckResult.Unhealthy("Migrations failed.")),
                "Stopped" => Task.FromResult(HealthCheckResult.Unhealthy("Migrations were stopped.")),
                "Pending" => Task.FromResult(HealthCheckResult.Unhealthy("Migrations have not started yet.")),
                _ => Task.FromResult(HealthCheckResult.Unhealthy($"Unknown migration state: {stateText}"))
            };
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Migration resource not found."));
    }
}
