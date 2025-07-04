// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Dcp.Process;
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
        var imageBuilder = context.Services.GetRequiredService<IResourceContainerImageBuilder>();
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
        var mainDeploymentStep = await progressReporter.CreateStepAsync(
            "Deploying Azure resources",
            context.CancellationToken).ConfigureAwait(false);

        try
        {
            // Create task for locating saved Bicep template
            var locateTemplateTask = await mainDeploymentStep.CreateTaskAsync(
                "Locating saved Bicep template for deployment",
                context.CancellationToken).ConfigureAwait(false);

            // Locate the main.bicep file that was saved during publishing
            var mainBicepPath = PublishingContext.MainBicepPath;
            if (!File.Exists(mainBicepPath))
            {
                throw new FileNotFoundException($"Main Bicep template not found at {mainBicepPath}. Ensure the publishing step has completed successfully.");
            }

            await locateTemplateTask.SucceedAsync(
                $"Bicep template located at {mainBicepPath}",
                context.CancellationToken).ConfigureAwait(false);

            // Create task for setting up deployment context
            var setupTask = await mainDeploymentStep.CreateTaskAsync(
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

            await setupTask.SucceedAsync(
                "Deployment context configured",
                context.CancellationToken).ConfigureAwait(false);

            // Create task for deploying main template
            var deploymentTask = await mainDeploymentStep.CreateTaskAsync(
                "Deploying main Bicep template to Azure",
                context.CancellationToken).ConfigureAwait(false);
            // Create a temporary AzureBicepResource to represent the main deployment
            var mainResource = new AzureBicepResource(this.Name + "-main");
            await using (deploymentTask.ConfigureAwait(false))
            {
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
                    useResourceScope: false,
                    context.CancellationToken).ConfigureAwait(false);

                await deploymentTask.SucceedAsync(
                    "Main Bicep template deployed successfully",
                    context.CancellationToken).ConfigureAwait(false);

                // Create task for propagating outputs to OutputLookup
                var outputPropagationTask = await mainDeploymentStep.CreateTaskAsync(
                    "Propagating deployment outputs to OutputLookup",
                    context.CancellationToken).ConfigureAwait(false);

                await using (outputPropagationTask.ConfigureAwait(false))
                {
                    var propagatedOutputsCount = 0;

                    // Propagate outputs from the main resource to the OutputLookup
                    foreach (var outputEntry in mainResource.Outputs)
                    {
                        var outputName = outputEntry.Key;
                        var outputValue = outputEntry.Value;

                        // Find any BicepOutputReference in the OutputLookup that references this output
                        foreach (var kvp in PublishingContext.OutputLookup)
                        {
                            var outputRef = kvp.Key;
                            var provisioningOutput = kvp.Value;

                            // Check if this output reference matches our deployed output
                            // The output could be from any of the deployed modules that contributed to the main template
                            if ($"{outputRef.Resource.Name.Replace("-", "_")}_{outputRef.Name}" == outputName)
                            {
                                // Update the underlying resource's Outputs dictionary so the BicepOutputReference can access it
                                outputRef.Resource.Outputs[outputRef.Name] = outputValue;
                                propagatedOutputsCount++;
                            }
                        }
                    }

                    await outputPropagationTask.SucceedAsync(
                        $"Propagated {propagatedOutputsCount} output(s) to OutputLookup",
                        context.CancellationToken).ConfigureAwait(false);
                }

                // Create task for propagating outputs to deployment targets
                var deploymentTargetPropagationTask = await mainDeploymentStep.CreateTaskAsync(
                    "Propagating deployment outputs to compute resource targets",
                    context.CancellationToken).ConfigureAwait(false);

                await using (deploymentTargetPropagationTask.ConfigureAwait(false))
                {
                    var propagatedTargetsCount = 0;
                    var totalOutputsPropagated = 0;

                    // Propagate outputs from main resource to deployment targets in ComputeResources
                    foreach (var (computeResourcePath, computeResource) in PublishingContext.ComputeResources)
                    {
                        var deploymentTarget = computeResource.GetDeploymentTargetAnnotation();
                        if (deploymentTarget?.DeploymentTarget is AzureBicepResource targetResource)
                        {
                            // Propagate all outputs from main resource to the target resource
                            foreach (var outputEntry in mainResource.Outputs)
                            {
                                var outputName = outputEntry.Key;
                                var outputValue = outputEntry.Value;

                                // Update the deployment target resource's Outputs dictionary
                                targetResource.Outputs[outputName] = outputValue;
                                totalOutputsPropagated++;
                            }
                            propagatedTargetsCount++;
                        }
                    }

                    await deploymentTargetPropagationTask.SucceedAsync(
                        $"Propagated {totalOutputsPropagated} output(s) to {propagatedTargetsCount} deployment target(s)",
                        context.CancellationToken).ConfigureAwait(false);
                }

                await mainDeploymentStep.SucceedAsync(
                    "Azure deployment completed successfully using consolidated Bicep template",
                    context.CancellationToken).ConfigureAwait(false);
            }

            // Build and push container images for compute resources
            if (PublishingContext.ComputeResources.Count > 0)
            {
                var computeResources = PublishingContext.ComputeResources.Values.ToList();
                var containerRegistryEndpoint = mainResource.Outputs["infra_AZURE_CONTAINER_REGISTRY_ENDPOINT"];

                // Build container images first
                await imageBuilder.BuildImagesAsync(computeResources, context.CancellationToken).ConfigureAwait(false);

                var imagePushStep = await progressReporter.CreateStepAsync(
                    "Pushing images to Azure Container Registry",
                    context.CancellationToken).ConfigureAwait(false);

                await using (imagePushStep.ConfigureAwait(false))
                {
                    try
                    {
                        // Sign in to ACR using managed identity
                        var acrLoginTask = await imagePushStep.CreateTaskAsync(
                            "Authenticating to Azure Container Registry",
                            context.CancellationToken).ConfigureAwait(false);

                        await using (acrLoginTask.ConfigureAwait(false))
                        {
                            var acrLoginProcess = new ProcessSpec("az")
                            {
                                Arguments = $"acr login --name {containerRegistryEndpoint}"
                            };

                            var (acrLoginResultTask, acrLoginDisposable) = ProcessUtil.Run(acrLoginProcess);
                            await using (acrLoginDisposable)
                            {
                                var acrLoginResult = await acrLoginResultTask.WaitAsync(context.CancellationToken).ConfigureAwait(false);
                                if (acrLoginResult.ExitCode != 0)
                                {
                                    throw new InvalidOperationException($"Failed to authenticate to Azure Container Registry");
                                }
                            }

                            await acrLoginTask.SucceedAsync(
                                "Successfully authenticated to Azure Container Registry",
                                context.CancellationToken).ConfigureAwait(false);
                        }

                        foreach (var computeResource in computeResources)
                        {
                            if (computeResource is not ProjectResource && (!computeResource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation) || !computeResource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out var dockerfileBuildAnnotation)))
                            {
                                continue;
                            }
                            var resourceName = computeResource.Name.ToLowerInvariant();
                            var localImageName = resourceName;
                            var remoteImageName = $"{containerRegistryEndpoint}/{resourceName}:latest";

                            var pushTask = await imagePushStep.CreateTaskAsync(
                                $"Pushing image for {computeResource.Name}",
                                context.CancellationToken).ConfigureAwait(false);

                            await using (pushTask.ConfigureAwait(false))
                            {
                                // Tag the local image for ACR
                                var tagProcess = new ProcessSpec("docker")
                                {
                                    Arguments = $"tag {localImageName} {remoteImageName}"
                                };

                                var (tagResultTask, tagDisposable) = ProcessUtil.Run(tagProcess);
                                await using (tagDisposable)
                                {
                                    var tagResult = await tagResultTask.WaitAsync(context.CancellationToken).ConfigureAwait(false);
                                    if (tagResult.ExitCode != 0)
                                    {
                                        throw new InvalidOperationException($"Failed to tag image {localImageName}");
                                    }
                                }

                                // Push the image to ACR
                                var pushProcess = new ProcessSpec("docker")
                                {
                                    Arguments = $"push {remoteImageName}"
                                };

                                var (pushResultTask, pushDisposable) = ProcessUtil.Run(pushProcess);
                                await using (pushDisposable)
                                {
                                    var pushResult = await pushResultTask.WaitAsync(context.CancellationToken).ConfigureAwait(false);
                                    if (pushResult.ExitCode != 0)
                                    {
                                        throw new InvalidOperationException($"Failed to push image {remoteImageName}");
                                    }
                                }

                                await pushTask.SucceedAsync(
                                    $"Successfully pushed {remoteImageName}",
                                    context.CancellationToken).ConfigureAwait(false);
                            }
                        }

                        await imagePushStep.SucceedAsync(
                            $"Successfully pushed {computeResources.Count} image(s) to Azure Container Registry",
                            context.CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError(ex, "Container image pushing failed");

                        await imagePushStep.FailAsync(
                            $"Container image pushing failed: {ex.Message}",
                            context.CancellationToken).ConfigureAwait(false);

                        throw;
                    }
                }
            }

            // Deploy compute resources
            if (PublishingContext.ComputeResources.Count > 0)
            {
                var computeDeploymentStep = await progressReporter.CreateStepAsync(
                    "Deploying compute resources",
                    context.CancellationToken).ConfigureAwait(false);

                await using (computeDeploymentStep.ConfigureAwait(false))
                {
                    try
                    {
                        foreach (var (computeResourcePath, computeResource) in PublishingContext.ComputeResources)
                        {
                            var computeResourceName = computeResource.Name;

                            var computeDeploymentTask = await computeDeploymentStep.CreateTaskAsync(
                                $"Deploying compute resource: {computeResourceName}",
                                context.CancellationToken).ConfigureAwait(false);

                            await using (computeDeploymentTask.ConfigureAwait(false))
                            {
                                // Deploy the compute resource using its Bicep file
                                var deploymentTarget = computeResource.GetDeploymentTargetAnnotation();
                                var targetResource = (AzureBicepResource)deploymentTarget!.DeploymentTarget;
                                await bicepProvisioner.DeployWithBicepFileAsync(
                                            targetResource,
                                            computeResourcePath,
                                            provisioningContext,
                                            useResourceScope: true,
                                            context.CancellationToken).ConfigureAwait(false);

                                await computeDeploymentTask.SucceedAsync(
                                    $"Compute resource {computeResourceName} deployed successfully",
                                    context.CancellationToken).ConfigureAwait(false);
                            }
                        }

                        await computeDeploymentStep.SucceedAsync(
                            $"Successfully deployed {PublishingContext.ComputeResources.Count} compute resource(s)",
                            context.CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError(ex, "Compute resource deployment failed");

                        await computeDeploymentStep.FailAsync(
                            $"Compute resource deployment failed: {ex.Message}",
                            context.CancellationToken).ConfigureAwait(false);

                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Azure deployment failed");

            await mainDeploymentStep.FailAsync(
                $"Azure deployment failed: {ex.Message}",
                context.CancellationToken).ConfigureAwait(false);

            throw;
        }
    }
}
