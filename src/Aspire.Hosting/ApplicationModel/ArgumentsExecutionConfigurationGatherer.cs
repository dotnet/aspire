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
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argumentAnnotations))
        {
            var callbackContext = new CommandLineArgsCallbackContext([], resource, cancellationToken)
            {
                Logger = resourceLogger,
                ExecutionContext = executionContext
            };

            foreach (var ann in argumentAnnotations)
            {
                var args = await ann.AsCallbackAnnotation().EvaluateOnceAsync(callbackContext).ConfigureAwait(false);

                foreach (var arg in args)
                {
                    context.Arguments.Add(arg);
                }
            }
        }
    }
}