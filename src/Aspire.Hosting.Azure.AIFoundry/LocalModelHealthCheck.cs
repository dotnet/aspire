// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.AIFoundry;

internal sealed class LocalModelHealthCheck(string? modelId) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var manager = FoundryLocalManager.Instance;
        if (manager is null)
        {
            return HealthCheckResult.Unhealthy("Foundry Local has not been initialised successfully.");
        }

        if (string.IsNullOrEmpty(modelId))
        {
            return HealthCheckResult.Unhealthy("Model has not been loaded.");
        }

        var catalog = await manager.GetCatalogAsync(cancellationToken).ConfigureAwait(false);

        var loadedModels = await catalog.GetLoadedModelsAsync(cancellationToken).ConfigureAwait(false);

        if (!loadedModels.Any(lm => lm.Id.Equals(modelId, StringComparison.InvariantCultureIgnoreCase)))
        {
            return HealthCheckResult.Unhealthy("Model has not been loaded.");
        }

        return HealthCheckResult.Healthy();
    }
}
