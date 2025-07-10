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
        if (options.Value.OutputPath == null)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified."
            );
        }

        var outputPath = Path.GetFullPath(options.Value.OutputPath);

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

            var publishingResources = new List<IResource>();
            var deployingResources = new List<IResource>();

            foreach (var resource in model.Resources)
            {
                if (resource.HasAnnotationOfType<PublishingCallbackAnnotation>())
                {
                    publishingResources.Add(resource);
                }

                if (resource.HasAnnotationOfType<DeployingCallbackAnnotation>())
                {
                    deployingResources.Add(resource);
                }
            }

            (string Message, CompletionState State) taskInfo;

            if (options.Value.Deploy)
            {
                taskInfo = deployingResources.Count switch
                {
                    0 => ("No resources in the distributed application model support deployment.", CompletionState.CompletedWithError),
                    _ => ($"Found {deployingResources.Count} resources that support deployment. ({string.Join(", ", deployingResources.Select(r => r.GetType().Name))})", CompletionState.Completed)
                };
            }
            else
            {
                taskInfo = publishingResources.Count switch
                {
                    0 => ("No resources in the distributed application model support publishing.", CompletionState.CompletedWithError),
                    _ => ($"Found {publishingResources.Count} resources that support publishing. ({string.Join(", ", publishingResources.Select(r => r.GetType().Name))})", CompletionState.Completed)
                };
            }

            await task.CompleteAsync(
                        taskInfo.Message,
                        taskInfo.State,
                        cancellationToken)
                        .ConfigureAwait(false);

            if (taskInfo.State == CompletionState.CompletedWithError)
            {
                // If there are no resources to publish or deploy, we can exit early
                return;
            }
        }

        var publishingContext = new PublishingContext(model, executionContext, serviceProvider, logger, cancellationToken, outputPath);
        await publishingContext.WriteModelAsync(model).ConfigureAwait(false);

        // If deployment is enabled, run deploying callbacks after publishing
        if (options.Value.Deploy)
        {
            var deployingContext = new DeployingContext(model, executionContext, serviceProvider, logger, cancellationToken, outputPath);
            await deployingContext.WriteModelAsync(model).ConfigureAwait(false);
        }
    }
}
