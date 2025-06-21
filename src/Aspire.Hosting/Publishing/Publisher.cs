// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal class Publisher(
    IPublishingActivityProgressReporter progressReporter,
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
            cancellationToken)
            .ConfigureAwait(false);

        var task = await progressReporter.CreateTaskAsync(
            step,
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

        (string Message, TaskCompletionState State) taskInfo;

        if (options.Value.Deploy)
        {
            taskInfo = deployingResources.Count switch
            {
                0 => ("No resources in the distributed application model support deployment.", TaskCompletionState.CompletedWithError),
                _ => ($"Found {deployingResources.Count} resources that support deployment. ({string.Join(", ", deployingResources.Select(r => r.GetType().Name))})", TaskCompletionState.Completed)
            };
        }
        else
        {
            taskInfo = publishingResources.Count switch
            {
                0 => ("No resources in the distributed application model support publishing.", TaskCompletionState.CompletedWithError),
                _ => ($"Found {publishingResources.Count} resources that support publishing. ({string.Join(", ", publishingResources.Select(r => r.GetType().Name))})", TaskCompletionState.Completed)
            };
        }

        await progressReporter.CompleteTaskAsync(
                    task,
                    taskInfo.State,
                    taskInfo.Message,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

        // This should be automagically handled by the progress reporter
        await progressReporter.CompleteStepAsync(
                    step,
                    "Model analysis completed.",
                    isError: taskInfo.State == TaskCompletionState.CompletedWithError,
                    cancellationToken)
                    .ConfigureAwait(false);

        if (taskInfo.State == TaskCompletionState.CompletedWithError)
        {
            // TOOD: This should be automatically handled (if any steps fail)
            await progressReporter.CompletePublishAsync(false, cancellationToken)
                .ConfigureAwait(false);

            // If there are no resources to publish or deploy, we can exit early
            return;
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
