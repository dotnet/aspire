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
    IPipelineActivityReporter pipelineActivityReporter) : BackgroundService
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

            var step = await pipelineActivityReporter.CreateStepAsync("pipeline execution", stoppingToken).ConfigureAwait(false);

            // Set the current step in the logger provider so that logs are associated with the correct pipeline step
            PipelineLoggerProvider.CurrentStep = step;

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

                await step.SucceedAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
                await activityReporter.CompletePublishAsync(completionMessage: null, completionState: null, cancellationToken: stoppingToken).ConfigureAwait(false);

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
                logger.LogError(ex, ex.Message);

                await step.FailAsync(cancellationToken: stoppingToken).ConfigureAwait(false);

                await activityReporter.CompletePublishAsync(completionMessage: ex.Message, completionState: CompletionState.CompletedWithError, cancellationToken: stoppingToken).ConfigureAwait(false);

                if (!backchannelService.IsBackchannelExpected)
                {
                    throw new DistributedApplicationException($"Pipeline execution failed: {ex.Message}", ex);
                }
            }
            finally
            {
                // Clear the current step from the logger provider
                PipelineLoggerProvider.CurrentStep = null;
            }
        }
    }

    public async Task ExecutePipelineAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var pipelineContext = new PipelineContext(model, executionContext, serviceProvider, logger, cancellationToken);

        var pipeline = serviceProvider.GetRequiredService<IDistributedApplicationPipeline>();
        await pipeline.ExecuteAsync(pipelineContext).ConfigureAwait(false);
    }
}
