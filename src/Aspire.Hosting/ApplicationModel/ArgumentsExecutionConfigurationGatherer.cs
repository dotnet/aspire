// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers command line arguments for resources.
/// </summary>
internal class ArgumentsExecutionConfigurationGatherer : IExecutionConfigurationGatherer
{
    /// <inheritdoc/>
    public async ValueTask GatherAsync(IExecutionConfigurationGathererContext context, IResource resource, ILogger resourceLogger, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        // If cached unresolved values exist (from Phase 1 container configuration),
        // replay them into the context instead of re-invoking callbacks.
        if (resource.TryGetLastAnnotation<CachedExecutionConfigurationAnnotation>(out var cached))
        {
            context.Arguments.AddRange(cached.Arguments);
            return;
        }

        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var callbacks))
        {
            var callbackContext = new CommandLineArgsCallbackContext(context.Arguments, resource, cancellationToken)
            {
                Logger = resourceLogger,
                ExecutionContext = executionContext
            };

            foreach (var callback in callbacks)
            {
                await callback.Callback(callbackContext).ConfigureAwait(false);
            }
        }
    }
}