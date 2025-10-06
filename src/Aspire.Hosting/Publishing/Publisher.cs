// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal class Publisher(
    IPublishingActivityReporter progressReporter,
    ILogger<Publisher> logger,
    IOptions<PublishingOptions> options,
    DistributedApplicationExecutionContext executionContext,
    IServiceProvider serviceProvider,
    IInteractionService interactionService) : IDistributedApplicationPublisher
{
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        if (options.Value.OutputPath == null && !options.Value.Deploy)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified."
            );
        }

        // Prompt user about saving deployment state to user secrets before deployment
        // Only prompt if NoCache is false and Deploy is true and SaveToUserSecrets hasn't been set yet
        if (options.Value.Deploy && !options.Value.NoCache && !options.Value.SaveToUserSecrets.HasValue)
        {
            await PromptSaveToUserSecretsAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task PromptSaveToUserSecretsAsync(CancellationToken cancellationToken)
    {
        if (!interactionService.IsAvailable)
        {
            // If interaction service is not available, default to saving
            options.Value.SaveToUserSecrets = true;
            logger.LogDebug("Interaction service not available. Defaulting to saving deployment state to user secrets.");
            return;
        }

        try
        {
            var message = "Would you like to save your deployment configuration for and state to user secrets for future deployments?\n\n" +
                          "This allows you to skip re-entering these values in future deployments. " +
                          "To skip using saved configuration, use the --no-cache flag.";

            var result = await interactionService.PromptNotificationAsync(
                "Save deployment configuration",
                message,
                new NotificationInteractionOptions
                {
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    ShowSecondaryButton = true
                },
                cancellationToken).ConfigureAwait(false);

            options.Value.SaveToUserSecrets = !result.Canceled && result.Data;
            logger.LogDebug("User {Decision} to save deployment state to user secrets.",
                options.Value.SaveToUserSecrets == true ? "chose" : "declined");
        }
        catch (Exception ex)
        {
            // If prompting fails, default to saving and log the error
            logger.LogWarning(ex, "Failed to prompt user about saving deployment state. Defaulting to saving to user secrets.");
            options.Value.SaveToUserSecrets = true;
        }
    }
}
