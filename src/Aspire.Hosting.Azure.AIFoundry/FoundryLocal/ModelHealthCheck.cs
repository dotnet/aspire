// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.ApplicationModel;

internal class ModelHealthCheck(string model, FoundryManager manager) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!manager.IsServiceRunning)
        {
            return HealthCheckResult.Unhealthy("Foundry Local not running");
        }

        var loadedModels = await manager.ListLoadedModelsAsync(cancellationToken);

        if (!loadedModels.Any(lm => lm.Alias == model))
        {
            return HealthCheckResult.Unhealthy("Model has not been loaded.");
        }

        return HealthCheckResult.Healthy();
    }
}
