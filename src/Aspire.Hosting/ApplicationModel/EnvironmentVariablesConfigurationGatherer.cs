// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers environment variables for resources.
/// </summary>
internal class EnvironmentVariablesExecutionConfigurationGatherer : IExecutionConfigurationGatherer
{
    /// <inheritdoc/>
    public async ValueTask GatherAsync(IExecutionConfigurationGathererContext context, IResource resource, ILogger resourceLogger, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        if (resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var callbackContext = new EnvironmentCallbackContext(executionContext, resource, cancellationToken: cancellationToken)
            {
                Logger = resourceLogger,
            };

            foreach (var callback in callbacks)
            {
                var envVars = await ((ICallbackResourceAnnotation<EnvironmentCallbackContext, Dictionary<string, object>>)callback)
                    .EvaluateOnceAsync(callbackContext).ConfigureAwait(false);

                foreach (var kvp in envVars)
                {
                    context.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}