// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers environment variables for resources.
/// </summary>
public class ResourceEnvironmentVariablesConfigurationGatherer : IResourceConfigurationGatherer
{
    /// <inheritdoc/>
    public async ValueTask GatherAsync(IResourceConfigurationGathererContext context, CancellationToken cancellationToken = default)
    {
        if (context.Resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var callbackContext = new EnvironmentCallbackContext(context.ExecutionContext, context.Resource, context.EnvironmentVariables, cancellationToken)
            {
                Logger = context.ResourceLogger,
            };

            foreach (var callback in callbacks)
            {
                await callback.Callback(callbackContext).ConfigureAwait(false);
            }
        }
    }
}