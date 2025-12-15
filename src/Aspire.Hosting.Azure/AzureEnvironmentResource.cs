// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Pipelines;
using Azure;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the root Azure deployment target for an Aspire application.
/// Manages deployment parameters and context for Azure resources.
/// </summary>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class AzureEnvironmentResource : Resource
{
    /// <summary>
    /// The name of the step that creates the provisioning context.
    /// </summary>
    internal const string CreateProvisioningContextStepName = "create-provisioning-context";

    /// <summary>
    /// The name of the step that provisions Azure infrastructure resources.
    /// </summary>
    public const string ProvisionInfrastructureStepName = "provision-azure-bicep-resources";

    /// <summary>
    /// Gets or sets the Azure location that the resources will be deployed to.
    /// </summary>
    public ParameterResource Location { get; set; }

    /// <summary>
    /// Gets or sets the Azure resource group name that the resources will be deployed to.
    /// </summary>
    public ParameterResource ResourceGroupName { get; set; }

    /// <summary>
    /// Gets or sets the Azure principal ID that will be used to deploy the resources.
    /// </summary>
    public ParameterResource PrincipalId { get; set; }

    /// <summary>
    /// Gets the task completion source for the provisioning context.
    /// Consumers should await ProvisioningContextTask.Task to get the provisioning context.
    /// </summary>
    internal TaskCompletionSource<ProvisioningContext> ProvisioningContextTask { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Azure environment resource.</param>
    /// <param name="location">The Azure location that the resources will be deployed to.</param>
    /// <param name="resourceGroupName">The Azure resource group name that the resources will be deployed to.</param>
    /// <param name="principalId">The Azure principal ID that will be used to deploy the resources.</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is invalid.</exception>
    public AzureEnvironmentResource(string name, ParameterResource location, ParameterResource resourceGroupName, ParameterResource principalId) : base(name)
    {
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            var publishStep = new PipelineStep
            {
                Name = $"publish-{Name}",
                Description = $"Publishes the Azure environment configuration for {Name}.",
                Action = ctx => PublishAsync(ctx),
                RequiredBySteps = [WellKnownPipelineSteps.Publish],
                DependsOnSteps = [WellKnownPipelineSteps.PublishPrereq]
            };

            var validateStep = new PipelineStep
            {
                Name = "validate-azure-login",
                Description = "Validates Azure CLI authentication before deployment.",
                Action = ctx => ValidateAzureLoginAsync(ctx),
                RequiredBySteps = [WellKnownPipelineSteps.Deploy],
                DependsOnSteps = [WellKnownPipelineSteps.DeployPrereq]
            };

            var createContextStep = new PipelineStep
            {
                Name = CreateProvisioningContextStepName,
                Description = "Creates the Azure provisioning context for infrastructure deployment.",
                Action = async ctx =>
                {
                    var provisioningContextProvider = ctx.Services.GetRequiredService<IProvisioningContextProvider>();
                    var provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(ctx.CancellationToken).ConfigureAwait(false);
                    ProvisioningContextTask.TrySetResult(provisioningContext);
                },
                RequiredBySteps = [WellKnownPipelineSteps.Deploy],
                DependsOnSteps = [WellKnownPipelineSteps.DeployPrereq]
            };
            createContextStep.DependsOn(validateStep);

            var provisionStep = new PipelineStep
            {
                Name = ProvisionInfrastructureStepName,
                Description = "Aggregation step for all Azure infrastructure provisioning operations.",
                Action = _ => Task.CompletedTask,
                Tags = [WellKnownPipelineTags.ProvisionInfrastructure],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy],
                DependsOnSteps = [WellKnownPipelineSteps.DeployPrereq]
            };

            provisionStep.DependsOn(createContextStep);

            var deprovisionStep = new PipelineStep
            {
                Name = $"deprovision-{Name}",
                Description = "Deprovisions the Azure environment by deleting the resource group.",
                Action = ctx => DeprovisionAsync(ctx),
                Tags = ["azure-deprovision"]
            };

            return [publishStep, validateStep, createContextStep, provisionStep, deprovisionStep];
        }));

        Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

        Location = location;
        ResourceGroupName = resourceGroupName;
        PrincipalId = principalId;
    }

    private Task PublishAsync(PipelineStepContext context)
    {
        var azureProvisioningOptions = context.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
        var outputService = context.Services.GetRequiredService<IPipelineOutputService>();
        var publishingContext = new AzurePublishingContext(
            outputService.GetOutputDirectory(),
            azureProvisioningOptions.Value,
            context.Services,
            context.Logger,
            context.ReportingStep);

        return publishingContext.WriteModelAsync(context.Model, this);
    }

    private static async Task ValidateAzureLoginAsync(PipelineStepContext context)
    {
        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();

        try
        {
            var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
            await tokenCredentialProvider.TokenCredential.GetTokenAsync(tokenRequest, context.CancellationToken)
                .ConfigureAwait(false);

            await context.ReportingStep.CompleteAsync(
                "Azure CLI authentication validated successfully",
                CompletionState.Completed,
                context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await context.ReportingStep.CompleteAsync(
                "Azure CLI authentication failed. Please run `az login` to authenticate before deploying. Learn more at [Azure CLI documentation](https://learn.microsoft.com/cli/azure/authenticate-azure-cli).",
                CompletionState.CompletedWithError,
                context.CancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task DeprovisionAsync(PipelineStepContext context)
    {
        var interactionService = context.Services.GetService<IInteractionService>();
        
        // If interaction service is not available, fail immediately
        if (interactionService == null || !interactionService.IsAvailable)
        {
            await context.ReportingStep.CompleteAsync(
                "Cannot deprovision: interaction service not available for confirmation prompt.",
                CompletionState.CompletedWithError,
                context.CancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("Interaction service is required for deprovisioning operations.");
        }

        // Get the resource group name from the parameter
        var resourceGroupName = await ResourceGroupName.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
        
        if (string.IsNullOrWhiteSpace(resourceGroupName))
        {
            await context.ReportingStep.CompleteAsync(
                "Cannot deprovision: resource group name is not available.",
                CompletionState.CompletedWithError,
                context.CancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("Resource group name is required for deprovisioning.");
        }

        // Prompt for confirmation
        var confirmationResult = await interactionService.PromptConfirmationAsync(
            "Confirm Deprovision",
            $"Are you sure you want to delete the entire resource group '{resourceGroupName}'? This action cannot be undone and will delete all resources in the group.",
            new MessageBoxInteractionOptions
            {
                Intent = MessageIntent.Warning,
                PrimaryButtonText = "Delete Resource Group",
                SecondaryButtonText = "Cancel"
            },
            context.CancellationToken).ConfigureAwait(false);

        // If user canceled or declined
        if (confirmationResult.Canceled || !confirmationResult.Data)
        {
            await context.ReportingStep.CompleteAsync(
                "Deprovision operation canceled by user.",
                CompletionState.Completed,
                context.CancellationToken).ConfigureAwait(false);
            return;
        }

        // User confirmed, proceed with deletion
        try
        {
            // Get the provisioning context
            var provisioningContext = await ProvisioningContextTask.Task.ConfigureAwait(false);
            
            var resourceGroup = provisioningContext.ResourceGroup;
            
            context.Logger.LogInformation("Starting deletion of resource group '{ResourceGroupName}'...", resourceGroupName);

            // Start the delete operation
            var deleteOperation = await resourceGroup.DeleteAsync(
                WaitUntil.Started,
                context.CancellationToken).ConfigureAwait(false);

            context.Logger.LogInformation("Resource group deletion initiated. Waiting for completion...");

            // Wait for the operation to complete
            await deleteOperation.WaitForCompletionResponseAsync(context.CancellationToken).ConfigureAwait(false);

            await context.ReportingStep.CompleteAsync(
                $"Resource group '{resourceGroupName}' has been successfully deleted.",
                CompletionState.Completed,
                context.CancellationToken).ConfigureAwait(false);

            context.Logger.LogInformation("Resource group '{ResourceGroupName}' deleted successfully.", resourceGroupName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            context.Logger.LogError(ex, "Failed to delete resource group '{ResourceGroupName}'.", resourceGroupName);
            await context.ReportingStep.CompleteAsync(
                $"Failed to delete resource group: {ex.Message}",
                CompletionState.CompletedWithError,
                context.CancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
