// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.OpenAI;

/// <summary>
/// Health check for OpenAI resources.
/// </summary>
internal sealed class OpenAIHealthCheck : IHealthCheck
{
    /// <summary>
    /// Checks the health of the OpenAI resource.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
