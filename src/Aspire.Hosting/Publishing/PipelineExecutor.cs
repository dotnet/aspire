// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES002

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Cli;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal sealed class PipelineExecutor(
    ILogger<PipelineExecutor> logger,
    IHostApplicationLifetime lifetime,
    DistributedApplicationExecutionContext executionContext,
    DistributedApplicationModel model,
    IServiceProvider serviceProvider,
    IPipelineActivityReporter activityReporter,
    IDistributedApplicationEventing eventing,
    BackchannelService backchannelService,
    IOptions<PipelineOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (executionContext.IsPublishMode)
        {
            // If we are running in publish mode and are being driven by the
            // CLI we need to wait for the backchannel from the CLI to the
            // apphost to be connected so we can stream back publishing progress.
            // This code detects that a backchannel is expected - and if so
            // we block until the backchannel is connected and bound to the RPC target.
            if (backchannelService.IsBackchannelExpected)
            {
                logger.LogDebug("Waiting for backchannel connection before publishing.");
                await backchannelService.BackchannelConnected.ConfigureAwait(false);
            }

            try
            {
                await eventing.PublishAsync<BeforePublishEvent>(
                    new BeforePublishEvent(serviceProvider, model), stoppingToken
                    ).ConfigureAwait(false);

                await ExecutePipelineAsync(model, stoppingToken).ConfigureAwait(false);

                await eventing.PublishAsync<AfterPublishEvent>(
                    new AfterPublishEvent(serviceProvider, model), stoppingToken
                    ).ConfigureAwait(false);

                // We pass null here so the aggregate state can be calculated based on the state of
                // each of the pipeline steps that have been enumerated.
                await activityReporter.CompletePublishAsync(completionMessage: null, completionState: null, isDeploy: true, cancellationToken: stoppingToken).ConfigureAwait(false);

                // If we are running in publish mode and a backchannel is being
                // used then we don't want to stop the app host. Instead the
                // CLI will tell the app host to stop when it is done - and
                // if the CLI crashes then the orphan detector will kick in
                // and stop the app host.
                if (!backchannelService.IsBackchannelExpected)
                {
                    lifetime.StopApplication();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute the pipeline.");
                await activityReporter.CompletePublishAsync(completionMessage: ex.Message, completionState: CompletionState.CompletedWithError, isDeploy: true, cancellationToken: stoppingToken).ConfigureAwait(false);

                if (!backchannelService.IsBackchannelExpected)
                {
                    throw new DistributedApplicationException($"Pipeline execution failed: {ex.Message}", ex);
                }
            }
        }
    }

    public async Task ExecutePipelineAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        // Add a step to display the target environment
        var environmentStep = await activityReporter.CreateStepAsync(
            "display-environment",
            cancellationToken).ConfigureAwait(false);

        await using (environmentStep.ConfigureAwait(false))
        {
            var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
            var environmentName = hostEnvironment?.EnvironmentName ?? "Production";

            var environmentTask = await environmentStep.CreateTaskAsync(
                $"Discovering target environment",
                cancellationToken)
                .ConfigureAwait(false);

            await environmentTask.CompleteAsync(
                $"Target environment: {environmentName.ToLowerInvariant()}",
                CompletionState.Completed,
                cancellationToken)
                .ConfigureAwait(false);
        }

        // Check if --clear-cache flag is set and prompt user before deleting deployment state
        if (options.Value.ClearCache)
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
                        logger.LogInformation("User declined to clear deployment state. Canceling pipeline execution.");
                        return;
                    }

                    // User confirmed - delete the deployment state file
                    logger.LogInformation("Deleting deployment state file at {Path} due to --clear-cache flag", deploymentStateManager.StateFilePath);
                    File.Delete(deploymentStateManager.StateFilePath);
                }
            }
        }

        // Add a step to do model analysis before publishing/deploying
        var step = await activityReporter.CreateStepAsync(
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

            var hasResourcesWithSteps = model.Resources.Any(r => r.HasAnnotationOfType<PipelineStepAnnotation>());
            var pipeline = serviceProvider.GetRequiredService<IDistributedApplicationPipeline>();
            var hasDirectlyRegisteredSteps = pipeline is DistributedApplicationPipeline concretePipeline && concretePipeline.HasSteps;

            if (!hasResourcesWithSteps && !hasDirectlyRegisteredSteps)
            {
                message = "No pipeline steps found in the application.";
                state = CompletionState.CompletedWithError;
            }
            else
            {
                message = "Found pipeline steps in the application.";
                state = CompletionState.Completed;
            }

            await task.CompleteAsync(
                        message,
                        state,
                        cancellationToken)
                        .ConfigureAwait(false);

            // Add a task to show the deployment state file path if available
            if (!options.Value.ClearCache)
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
                // If there are no pipeline steps, we can exit early
                return;
            }
        }

        var pipelineContext = new PipelineContext(model, executionContext, serviceProvider, logger, cancellationToken, options.Value.OutputPath is not null ?
            Path.GetFullPath(options.Value.OutputPath) : null);

        try
        {
            var pipeline = serviceProvider.GetRequiredService<IDistributedApplicationPipeline>();
            await pipeline.ExecuteAsync(pipelineContext).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            var errorStep = await activityReporter.CreateStepAsync(
                "pipeline-validation",
                cancellationToken).ConfigureAwait(false);

            await using (errorStep.ConfigureAwait(false))
            {
                var errorTask = await errorStep.CreateTaskAsync(
                    "Validating pipeline configuration",
                    cancellationToken)
                    .ConfigureAwait(false);

                await errorTask.CompleteAsync(
                    ex.Message,
                    CompletionState.CompletedWithError,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }
}
