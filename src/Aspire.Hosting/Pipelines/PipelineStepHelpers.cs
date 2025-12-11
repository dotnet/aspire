// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECONTAINERRUNTIME001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Helper methods for common pipeline step operations.
/// </summary>
internal static class PipelineStepHelpers
{
    /// <summary>
    /// Pushes a container image to a registry with proper logging and task reporting.
    /// </summary>
    /// <param name="resource">The resource whose image should be pushed.</param>
    /// <param name="context">The pipeline step context for logging and task reporting.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task PushImageToRegistryAsync(IResource resource, PipelineStepContext context)
    {
        var registry = resource.GetContainerRegistry();
        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Failed to retrieve container registry information.");

        IValueProvider cir = new ContainerImageReference(resource);
        var targetTag = await cir.GetValueAsync(context.CancellationToken).ConfigureAwait(false);

        var pushTask = await context.ReportingStep.CreateTaskAsync(
            $"Pushing **{resource.Name}** to **{registryName}**",
            context.CancellationToken).ConfigureAwait(false);

        await using (pushTask.ConfigureAwait(false))
        {
            try
            {
                if (targetTag is null)
                {
                    throw new InvalidOperationException($"Failed to get target tag for {resource.Name}");
                }

                var containerImageManager = context.Services.GetRequiredService<IResourceContainerImageManager>();
                await containerImageManager.PushImageAsync(resource, context.CancellationToken).ConfigureAwait(false);

                await pushTask.CompleteAsync(
                    $"Successfully pushed **{resource.Name}** to `{targetTag}`",
                    CompletionState.Completed,
                    context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pushTask.CompleteAsync(
                    $"Failed to push **{resource.Name}**: {ex.Message}",
                    CompletionState.CompletedWithError,
                    context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }
}
