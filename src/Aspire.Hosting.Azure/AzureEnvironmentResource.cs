// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the root Azure deployment target for an Aspire application.
/// Emits a <c>main.bicep</c> that aggregates all provisionable resources.
/// </summary>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class AzureEnvironmentResource : Resource
{
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

    internal AzurePublishingContext? PublishingContext { get; set; }

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
        Annotations.Add(new PublishingCallbackAnnotation(PublishAsync));
        Annotations.Add(new DeployingCallbackAnnotation(DeployAsync));

        Location = location;
        ResourceGroupName = resourceGroupName;
        PrincipalId = principalId;
    }

    private Task PublishAsync(PublishingContext context)
    {
        var azureProvisioningOptions = context.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();

        PublishingContext = new AzurePublishingContext(
            context.OutputPath,
            azureProvisioningOptions.Value,
            context.Logger,
            context.ActivityReporter);

        return PublishingContext.WriteModelAsync(context.Model, this);
    }

    private async Task DeployAsync(DeployingContext context)
    {
        var azureProvisionerOptions = context.Services.GetRequiredService<IOptions<AzureProvisionerOptions>>();
        var bicepProvisioner = context.Services.GetRequiredService<BicepProvisioner>();
        var progressReporter = context.ProgressReporter;

        Debug.Assert(PublishingContext != null, "PublishingContext should be initialized before deployment.");

        // Check for required Azure configuration and prompt if missing
        var subscriptionId = azureProvisionerOptions.Value.SubscriptionId;
        var resourceGroup = azureProvisionerOptions.Value.ResourceGroup;
        var location = azureProvisionerOptions.Value.Location;

        var interactionService = context.Services.GetService<IInteractionService>();
        if (interactionService?.IsAvailable == true)
        {
            var missingInputs = new List<InteractionInput>();

            if (string.IsNullOrEmpty(subscriptionId))
            {
                missingInputs.Add(new InteractionInput
                {
                    Label = "Azure Subscription ID",
                    InputType = InputType.Text,
                    Required = true,
                    Placeholder = "Enter your Azure subscription ID"
                });
            }

            if (string.IsNullOrEmpty(resourceGroup))
            {
                missingInputs.Add(new InteractionInput
                {
                    Label = "Resource Group Name",
                    InputType = InputType.Text,
                    Required = true,
                    Placeholder = "Enter the resource group name"
                });
            }

            if (string.IsNullOrEmpty(location))
            {
                missingInputs.Add(new InteractionInput
                {
                    Label = "Azure Location",
                    InputType = InputType.Text,
                    Required = true,
                    Placeholder = "e.g., eastus, westus2, centralus"
                });
            }

            if (missingInputs.Count > 0)
            {
                var inputResult = await interactionService.PromptInputsAsync(
                    "Azure Deployment Configuration",
                    "The following Azure configuration values are required for deployment:",
                    missingInputs,
                    cancellationToken: context.CancellationToken).ConfigureAwait(false);

                if (inputResult.Canceled || inputResult.Data == null)
                {
                    throw new InvalidOperationException("Azure deployment configuration is required but was not provided.");
                }

                // Extract the provided values
                var inputs = inputResult.Data.ToList();
                var inputIndex = 0;

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    subscriptionId = inputs[inputIndex++].Value;
                }

                if (string.IsNullOrEmpty(resourceGroup))
                {
                    resourceGroup = inputs[inputIndex++].Value;
                }

                if (string.IsNullOrEmpty(location))
                {
                    location = inputs[inputIndex].Value;
                }
            }
        }
        else if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(location))
        {
            throw new MissingConfigurationException("Azure subscription ID and location are required for deployment. Configure these values or ensure IInteractionService is available for prompting.");
        }

        // Create step for deployment process
        var deploymentStep = await progressReporter.CreateStepAsync(
            "Deploying Azure resources",
            context.CancellationToken).ConfigureAwait(false);

        try
        {
            // Create task for locating saved Bicep template
            var locateTemplateTask = await progressReporter.CreateTaskAsync(
                deploymentStep,
                "Locating saved Bicep template for deployment",
                context.CancellationToken).ConfigureAwait(false);

            // Locate the main.bicep file that was saved during publishing
            var mainBicepPath = PublishingContext.MainBicepPath;
            if (!File.Exists(mainBicepPath))
            {
                throw new FileNotFoundException($"Main Bicep template not found at {mainBicepPath}. Ensure the publishing step has completed successfully.");
            }

            await progressReporter.CompleteTaskAsync(
                locateTemplateTask,
                CompletionState.Completed,
                $"Bicep template located at {mainBicepPath}",
                context.CancellationToken).ConfigureAwait(false);

            // Create task for setting up deployment context
            var setupTask = await progressReporter.CreateTaskAsync(
                deploymentStep,
                "Setting up Azure deployment context",
                context.CancellationToken).ConfigureAwait(false);

            // Create provisioning context for deployment with the configured values
            var provisioningContextProvider = context.Services.GetRequiredService<IProvisioningContextProvider>();
            var userSecretsManager = context.Services.GetRequiredService<IUserSecretsManager>();
            var userSecrets = await userSecretsManager.LoadUserSecretsAsync(context.CancellationToken).ConfigureAwait(false);
            var provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(
                userSecrets,
                subscriptionId,
                resourceGroup,
                location,
                allowResourceGroupCreation: true,
                context.CancellationToken).ConfigureAwait(false);

            await progressReporter.CompleteTaskAsync(
                setupTask,
                CompletionState.Completed,
                "Deployment context configured",
                context.CancellationToken).ConfigureAwait(false);

            // Create task for deploying main template
            var deploymentTask = await progressReporter.CreateTaskAsync(
                deploymentStep,
                "Deploying main Bicep template to Azure",
                context.CancellationToken).ConfigureAwait(false);
            await using (deploymentTask.ConfigureAwait(false))
            {
                // Create a temporary AzureBicepResource to represent the main deployment
                var mainResource = new AzureBicepResource(this.Name + "-main");

                // Copy over parameters from the environment resource
                mainResource.Parameters[AzureBicepResource.KnownParameters.Location] = this.Location;

                // Map parameters from the AzurePublishingContext
                foreach (var (parameterResource, provisioningParameter) in PublishingContext.ParameterLookup)
                {
                    if (parameterResource == Location)
                    {
                        mainResource.Parameters[provisioningParameter.BicepIdentifier] = location;
                    }
                    else if (parameterResource == ResourceGroupName)
                    {
                        mainResource.Parameters[provisioningParameter.BicepIdentifier] = resourceGroup;
                    }
                    else if (parameterResource == PrincipalId)
                    {
                        mainResource.Parameters[provisioningParameter.BicepIdentifier] = provisioningContext.Principal.Id.ToString();
                    }
                    else
                    {
                        mainResource.Parameters[provisioningParameter.BicepIdentifier] = parameterResource;
                    }
                }

                // Deploy using the new BicepProvisioner API with the saved template file
                await bicepProvisioner.DeployWithBicepFileAsync(
                    mainResource,
                    mainBicepPath,
                    provisioningContext,
                    context.CancellationToken).ConfigureAwait(false);

                await progressReporter.CompleteTaskAsync(
                    deploymentTask,
                    CompletionState.Completed,
                    "Main Bicep template deployed successfully",
                    context.CancellationToken).ConfigureAwait(false);

                await progressReporter.CompleteStepAsync(
                    deploymentStep,
                    "Azure deployment completed successfully using consolidated Bicep template",
                    CompletionState.Completed,
                    context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Azure deployment failed");

            await progressReporter.CompleteStepAsync(
                deploymentStep,
                $"Azure deployment failed: {ex.Message}",
                CompletionState.CompletedWithError,
                context.CancellationToken).ConfigureAwait(false);

            throw;
        }
    }
}
