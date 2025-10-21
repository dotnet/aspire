// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal class Publisher(
    IPipelineActivityReporter progressReporter,
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

        // Add a step to display the target environment when deploying
        if (options.Value.Deploy)
        {
            var environmentStep = await progressReporter.CreateStepAsync(
                "display-environment",
                cancellationToken).ConfigureAwait(false);

            await using (environmentStep.ConfigureAwait(false))
            {
                var hostEnvironment = serviceProvider.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
                var environmentName = hostEnvironment?.EnvironmentName ?? "Production";

                var environmentTask = await environmentStep.CreateTaskAsync(
                    $"Discovering target environment",
                    cancellationToken)
                    .ConfigureAwait(false);

                await environmentTask.CompleteAsync(
                    $"Deploying to environment: {environmentName.ToLowerInvariant()}",
                    CompletionState.Completed,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // Check if --clear-cache flag is set and prompt user before deleting deployment state
        if (options.Value.Deploy && options.Value.ClearCache)
        {
            var deploymentStateManager = serviceProvider.GetService<IDeploymentStateManager>();
            if (deploymentStateManager?.StateFilePath is not null && File.Exists(deploymentStateManager.StateFilePath))
            {
                var interactionService = serviceProvider.GetService<IInteractionService>();
                if (interactionService?.IsAvailable == true)
                {
                    var hostEnvironment = serviceProvider.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
                    var environmentName = hostEnvironment?.EnvironmentName ?? "Production";
                    var result = await interactionService.PromptNotificationAsync(
                        "Clear Deployment State",
                        $"The deployment state for the '{environmentName}' environment will be deleted. All Azure resources will be re-provisioned. Do you want to continue?",
                        new NotificationInteractionOptions
                        {
                            Intent = MessageIntent.Confirmation,
                            ShowSecondaryButton = true,
                            ShowDismiss = false,
                            PrimaryButtonText = "Yes",
                            SecondaryButtonText = "No"
                        },
                        cancellationToken).ConfigureAwait(false);

                    if (result.Canceled || !result.Data)
                    {
                        // User declined or canceled - exit the deployment
                        logger.LogInformation("User declined to clear deployment state. Canceling deployment.");
                        return;
                    }

                    // User confirmed - delete the deployment state file
                    logger.LogInformation("Deleting deployment state file at {Path} due to --clear-cache flag", deploymentStateManager.StateFilePath);
                    File.Delete(deploymentStateManager.StateFilePath);
                }
            }
        }

        // Add a step to do model analysis before publishing/deploying
        var step = await progressReporter.CreateStepAsync(
            "analyze-model",
            cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {

            var task = await step.CreateTaskAsync(
                "Analyzing the distributed application model for publishing and deployment capabilities.",
                cancellationToken)
                .ConfigureAwait(false);

            string message;
            CompletionState state;

            if (options.Value.Deploy)
            {
                var hasResourcesWithSteps = model.Resources.Any(r => r.HasAnnotationOfType<PipelineStepAnnotation>());
                var pipeline = serviceProvider.GetRequiredService<IDistributedApplicationPipeline>();
                var hasDirectlyRegisteredSteps = pipeline is DistributedApplicationPipeline concretePipeline && concretePipeline.HasSteps;

                if (!hasResourcesWithSteps && !hasDirectlyRegisteredSteps)
                {
                    message = "No deployment steps found in the application pipeline.";
                    state = CompletionState.CompletedWithError;
                }
                else
                {
                    message = "Found deployment steps in the application pipeline.";
                    state = CompletionState.Completed;
                }
            }
            else
            {
                var targetResources = model.Resources.Where(r => r.HasAnnotationOfType<PublishingCallbackAnnotation>()).ToList();

                if (targetResources.Count == 0)
                {
                    message = "No resources in the distributed application model support publishing.";
                    state = CompletionState.CompletedWithError;
                }
                else
                {
                    message = $"Found {targetResources.Count} resources that support publishing. ({string.Join(", ", targetResources.Select(r => r.GetType().Name))})";
                    state = CompletionState.Completed;
                }
            }

            await task.CompleteAsync(
                        message,
                        state,
                        cancellationToken)
                        .ConfigureAwait(false);

            // Add a task to show the deployment state file path if available
            if (options.Value.Deploy && !options.Value.ClearCache)
            {
                var deploymentStateManager = serviceProvider.GetService<IDeploymentStateManager>();
                if (deploymentStateManager?.StateFilePath is not null && File.Exists(deploymentStateManager.StateFilePath))
                {
                    var statePathTask = await step.CreateTaskAsync(
                        "Checking deployment state configuration.",
                        cancellationToken)
                        .ConfigureAwait(false);

                    await statePathTask.CompleteAsync(
                        $"Deployment state will be loaded from: {deploymentStateManager.StateFilePath}",
                        CompletionState.Completed,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            if (state == CompletionState.CompletedWithError)
            {
                // If there are no resources to publish or deploy, we can exit early
                return;
            }
        }

        // If deployment is enabled, execute the pipeline with steps from PipelineStepAnnotation
        if (options.Value.Deploy)
        {
            // Initialize parameters as a pre-requisite for deployment
            var parameterProcessor = serviceProvider.GetRequiredService<ParameterProcessor>();
            await parameterProcessor.InitializeParametersAsync(model, waitForResolution: true, cancellationToken).ConfigureAwait(false);

            var deployingContext = new PipelineContext(model, executionContext, serviceProvider, logger, cancellationToken, options.Value.OutputPath is not null ?
                Path.GetFullPath(options.Value.OutputPath) : null);

            // Execute the pipeline - it will collect steps from PipelineStepAnnotation on resources
            var pipeline = serviceProvider.GetRequiredService<IDistributedApplicationPipeline>();
            await pipeline.ExecuteAsync(deployingContext).ConfigureAwait(false);
        }
        else
        {
            var outputPath = Path.GetFullPath(options.Value.OutputPath!);
            var publishingContext = new PublishingContext(model, executionContext, serviceProvider, logger, cancellationToken, outputPath);
            await publishingContext.WriteModelAsync(model).ConfigureAwait(false);
        }
    }
}
