// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure App Service Environment resource.
/// </summary>
public class AzureAppServiceEnvironmentResource :
    AzureProvisioningResource,
    IAzureComputeEnvironmentResource,
    IAzureContainerRegistry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAppServiceEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Azure App Service Environment.</param>
    /// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
    public AzureAppServiceEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(name, configureInfrastructure)
    {
        // Add pipeline step annotation to create per-resource build, push, and deploy steps
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            var model = factoryContext.PipelineContext.Model;
            var steps = new List<PipelineStep>();

            // Get all compute resources targeted to this environment
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation()?.ComputeEnvironment == this)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Create top-level coordination steps (no-op)
            var buildAllStep = new PipelineStep
            {
                Name = $"build-images-{name}",
                Action = _ => Task.CompletedTask
            };
            steps.Add(buildAllStep);

            var pushAllStep = new PipelineStep
            {
                Name = $"push-images-{name}",
                Action = _ => Task.CompletedTask
            };
            steps.Add(pushAllStep);

            var deployAllStep = new PipelineStep
            {
                Name = $"deploy-compute-{name}",
                Action = _ => Task.CompletedTask
            };
            steps.Add(deployAllStep);

            // Create per-resource steps
            foreach (var computeResource in computeResources)
            {
                // Build step for this resource
                var buildStep = new PipelineStep
                {
                    Name = $"build-image-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        await containerImageBuilder.BuildImagesAsync(
                            [computeResource],
                            new ContainerBuildOptions
                            {
                                TargetPlatform = ContainerTargetPlatform.LinuxAmd64
                            },
                            ctx.CancellationToken).ConfigureAwait(false);
                    }
                };
                buildStep.RequiredBy(buildAllStep);
                steps.Add(buildStep);

                // Push step for this resource
                var pushStep = new PipelineStep
                {
                    Name = $"push-image-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        await PushImageForResourceAsync(ctx, computeResource, containerImageBuilder).ConfigureAwait(false);
                    }
                };
                pushStep.DependsOn(buildStep);
                pushStep.RequiredBy(pushAllStep);
                steps.Add(pushStep);

                // Deploy step for this resource
                var deployStep = new PipelineStep
                {
                    Name = $"deploy-compute-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        await DeployComputeResourceAsync(ctx, computeResource).ConfigureAwait(false);
                    }
                };
                deployStep.DependsOn(pushStep);
                deployStep.RequiredBy(deployAllStep);
                steps.Add(deployStep);
            }

            return steps;
        }));

        // Add pipeline configuration annotation to create dependencies with container registry provision steps
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            // Get all push-image steps for this environment
            var model = context.Model;
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation()?.ComputeEnvironment == this)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Find the container registry provision step
            var registryResource = model.Resources.OfType<IAzureResource>()
                .FirstOrDefault(r => r is IContainerRegistry && r == this);

            if (registryResource != null)
            {
                var provisionSteps = context.GetSteps(registryResource, WellKnownPipelineTags.ProvisionInfrastructure);
                foreach (var provisionStep in provisionSteps)
                {
                    // Make all push steps depend on the registry being provisioned
                    foreach (var computeResource in computeResources)
                    {
                        var pushSteps = context.Pipeline.Steps
                            .Where(s => s.Name == $"push-image-{computeResource.Name}")
                            .ToList();
                        
                        foreach (var pushStep in pushSteps)
                        {
                            pushStep.DependsOn(provisionStep);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }));
    }

    private static async Task PushImageForResourceAsync(PipelineStepContext context, IResource computeResource, IResourceContainerImageBuilder containerImageBuilder)
    {
        var processRunner = context.Services.GetRequiredService<IProcessRunner>();
        var configuration = context.Services.GetRequiredService<IConfiguration>();

        // Get the container registry for this resource
        var deploymentTarget = computeResource.GetDeploymentTargetAnnotation();
        if (deploymentTarget?.ContainerRegistry is not { } registry)
        {
            return;
        }

        // Login to the registry
        var registryEndpoint = await registry.Endpoint.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(registryEndpoint))
        {
            throw new InvalidOperationException($"Container registry endpoint is not available for resource {computeResource.Name}");
        }

        if (registry is IAzureContainerRegistry azureRegistry)
        {
            var managedIdentityId = await azureRegistry.ManagedIdentityId.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(managedIdentityId))
            {
                var loginArgs = new[]
                {
                    "acr",
                    "login",
                    "--name",
                    registryEndpoint.Replace(".azurecr.io", ""),
                    "--identity",
                    managedIdentityId
                };

                var loginResult = await processRunner.RunAsync("az", loginArgs, cancellationToken: context.CancellationToken).ConfigureAwait(false);
                if (loginResult.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to login to Azure Container Registry: {loginResult.StandardError}");
                }
            }
        }

        // Push the image
        await containerImageBuilder.PushImagesAsync([computeResource], context.CancellationToken).ConfigureAwait(false);
    }

    private static async Task DeployComputeResourceAsync(PipelineStepContext context, IResource computeResource)
    {
        var bicepProvisioner = context.Services.GetRequiredService<IBicepProvisioner>();
        
        // Find the AzureEnvironmentResource to get the provisioning context
        var azureEnvironment = context.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
        if (azureEnvironment == null)
        {
            throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
        }
        
        var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);

        var resourceTask = await context.ReportingStep
            .CreateTaskAsync($"Deploying **{computeResource.Name}**", context.CancellationToken)
            .ConfigureAwait(false);

        await using (resourceTask.ConfigureAwait(false))
        {
            try
            {
                if (computeResource.GetDeploymentTargetAnnotation() is { } deploymentTarget)
                {
                    if (deploymentTarget.DeploymentTarget is AzureBicepResource bicepResource)
                    {
                        await bicepProvisioner.GetOrCreateResourceAsync(
                            bicepResource, provisioningContext, context.CancellationToken)
                            .ConfigureAwait(false);

                        var completionMessage = $"Successfully deployed **{computeResource.Name}**";

                        if (deploymentTarget.ComputeEnvironment is IAzureComputeEnvironmentResource azureComputeEnv)
                        {
                            // Try to get endpoint information if available
                            // This would require access to the endpoint, which we can add later if needed
                        }

                        await resourceTask.CompleteAsync(
                            completionMessage,
                            CompletionState.Completed,
                            context.CancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await resourceTask.CompleteAsync(
                            $"Skipped **{computeResource.Name}** - no Bicep deployment target",
                            CompletionState.CompletedWithWarning,
                            context.CancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    await resourceTask.CompleteAsync(
                        $"Skipped **{computeResource.Name}** - no deployment target annotation",
                        CompletionState.Completed,
                        context.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await resourceTask.CompleteAsync(
                    $"Failed to deploy {computeResource.Name}: {ex.Message}",
                    CompletionState.CompletedWithError,
                    context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    // We don't want these to be public if we end up with an app service
    // per compute resource.
    internal BicepOutputReference PlanIdOutputReference => new("planId", this);
    internal BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);
    internal BicepOutputReference ContainerRegistryName => new("AZURE_CONTAINER_REGISTRY_NAME", this);
    internal BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);
    internal BicepOutputReference ContainerRegistryClientId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID", this);
    internal BicepOutputReference WebsiteContributorManagedIdentityId => new("AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_ID", this);
    internal BicepOutputReference WebsiteContributorManagedIdentityPrincipalId => new("AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_PRINCIPAL_ID", this);

    /// <summary>
    /// Gets the suffix added to each web app created in this App Service Environment.
    /// </summary>
    private BicepOutputReference WebSiteSuffix => new("webSiteSuffix", this);

    /// <summary>
    /// Gets or sets a value indicating whether the Aspire dashboard should be included in the container app environment.
    /// Default is true.
    /// </summary>
    internal bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// Gets the name of the App Service Plan.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the URI of the App Service Environment dashboard.
    /// </summary>
    public BicepOutputReference DashboardUriReference => new("AZURE_APP_SERVICE_DASHBOARD_URI", this);

    internal static BicepValue<string> GetWebSiteSuffixBicep() =>
        BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id);

    ReferenceExpression IAzureContainerRegistry.ManagedIdentityId => 
        ReferenceExpression.Create($"{ContainerRegistryManagedIdentityId}");

    ReferenceExpression IContainerRegistry.Name => 
        ReferenceExpression.Create($"{ContainerRegistryName}");

    ReferenceExpression IContainerRegistry.Endpoint => 
        ReferenceExpression.Create($"{ContainerRegistryUrl}");

    ReferenceExpression IComputeEnvironmentResource.GetHostAddressExpression(EndpointReference endpointReference)
    {
        var resource = endpointReference.Resource;
        return ReferenceExpression.Create($"{resource.Name.ToLowerInvariant()}-{WebSiteSuffix}.azurewebsites.net");
    }

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();
        
        // Check if an AppServicePlan with the same identifier already exists
        var existingPlan = resources.OfType<AppServicePlan>().SingleOrDefault(plan => plan.BicepIdentifier == bicepIdentifier);
        
        if (existingPlan is not null)
        {
            return existingPlan;
        }
        
        // Create and add new resource if it doesn't exist
        var plan = AppServicePlan.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceAnnotation(
            this,
            infra,
            plan))
        {
            plan.Name = NameOutputReference.AsProvisioningParameter(infra);
        }

        infra.Add(plan);
        return plan;
    }
}
