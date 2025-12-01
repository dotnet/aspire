// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers command line arguments for resources.
/// </summary>
internal class ResourceArgumentsConfigurationGatherer : IResourceConfigurationGatherer
{
    /// <inheritdoc/>
    public async ValueTask GatherAsync(IResourceConfigurationGathererContext context, CancellationToken cancellationToken = default)
    {
        if (context.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var callbacks))
        {
            var callbackContext = new CommandLineArgsCallbackContext(context.Arguments, context.Resource, cancellationToken)
            {
                Logger = context.ResourceLogger,
                ExecutionContext = context.ExecutionContext
            };

            foreach (var callback in callbacks)
            {
                await callback.Callback(callbackContext).ConfigureAwait(false);
            }
        }
    }
}