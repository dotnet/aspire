// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal class Publisher(
    IPublishingActivityReporter progressReporter,
    ILogger<Publisher> logger,
    IOptions<PublishingOptions> options,
    DistributedApplicationExecutionContext executionContext,
    IServiceProvider serviceProvider) : IDistributedApplicationPublisher
{
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        if (options.Value.OutputPath == null && !options.Value.Deploy)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified."
            );
        }

        // Add a step to do model analysis before publishing/deploying
        var step = await progressReporter.CreateStepAsync(
            "Analyzing model.",
            cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {

            var task = await step.CreateTaskAsync(
                "Analyzing the distributed application model for publishing and deployment capabilities.",
                cancellationToken)
                .ConfigureAwait(false);

            var targetResources = new List<IResource>();

            foreach (var resource in model.Resources)
            {
                if (options.Value.Deploy)
                {
                    if (resource.HasAnnotationOfType<DeployingCallbackAnnotation>())
                    {
                        targetResources.Add(resource);
                    }
                }
                else
                {
                    if (resource.HasAnnotationOfType<PublishingCallbackAnnotation>())
                    {
                        targetResources.Add(resource);
                    }
                }

            }

            var (message, state) = GetTaskInfo(targetResources, options.Value.Deploy);

            await task.CompleteAsync(
                        message,
                        state,
                        cancellationToken)
                        .ConfigureAwait(false);

            if (state == CompletionState.CompletedWithError)
            {
                // If there are no resources to publish or deploy, we can exit early
                return;
            }
        }

        // If deployment is enabled, run deploying callbacks after publishing
        if (options.Value.Deploy)
        {
            var deployingContext = new DeployingContext(model, executionContext, serviceProvider, logger, cancellationToken, options.Value.OutputPath is not null ?
                Path.GetFullPath(options.Value.OutputPath) : null);
            await deployingContext.WriteModelAsync(model).ConfigureAwait(false);
        }
        else
        {
            var outputPath = Path.GetFullPath(options.Value.OutputPath!);
            var publishingContext = new PublishingContext(model, executionContext, serviceProvider, logger, cancellationToken, outputPath);
            await publishingContext.WriteModelAsync(model).ConfigureAwait(false);
        }
    }

    private static (string Message, CompletionState State) GetTaskInfo(List<IResource> targetResources, bool isDeploy)
    {
        var operation = isDeploy ? "deployment" : "publishing";
        return targetResources.Count switch
        {
            0 => ($"No resources in the distributed application model support {operation}.", CompletionState.CompletedWithError),
            _ => ($"Found {targetResources.Count} resources that support {operation}. ({string.Join(", ", targetResources.Select(r => r.GetType().Name))})", CompletionState.Completed)
        };
    }
}
