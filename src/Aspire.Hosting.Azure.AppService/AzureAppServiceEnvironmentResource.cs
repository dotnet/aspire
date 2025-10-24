// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
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
        Annotations.Add(new PipelineStepAnnotation(async (factoryContext) =>
        {
            var model = factoryContext.PipelineContext.Model;
            var steps = new List<PipelineStep>();

            // Get all compute resources targeted to this environment
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Add default image tags step for this environment
            var addImageTagsStep = new PipelineStep
            {
                Name = $"default-image-tags-{name}",
                Action = ctx => DefaultImageTags(ctx),
                Tags = ["default-image-tags"]
            };
            steps.Add(addImageTagsStep);

            // Add registry login step (once per environment, not per push)
            var registryLoginStep = new PipelineStep
            {
                Name = $"login-registry-{name}",
                Action = async ctx =>
                {
                    var processRunner = ctx.Services.GetRequiredService<IProcessRunner>();
                    var configuration = ctx.Services.GetRequiredService<IConfiguration>();
                    await AzureEnvironmentResourceHelpers.LoginToRegistryAsync(this, ctx, processRunner, configuration).ConfigureAwait(false);
                },
                Tags = ["login-registry"]
            };
            steps.Add(registryLoginStep);

            // For each compute resource, expand its build steps or create a default one
            foreach (var computeResource in computeResources)
            {
                // Check if the compute resource has its own build steps
                var hasCustomBuildSteps = false;
                if (computeResource.TryGetAnnotationsOfType<PipelineStepAnnotation>(out var pipelineStepAnnotations))
                {
                    foreach (var annotation in pipelineStepAnnotations)
                    {
                        var resourceSteps = await annotation.CreateStepsAsync(factoryContext).ConfigureAwait(false);
                        var buildSteps = resourceSteps.Where(s => s.Tags.Contains(WellKnownPipelineTags.BuildCompute)).ToList();
                        
                        if (buildSteps.Any())
                        {
                            hasCustomBuildSteps = true;
                            foreach (var buildStep in buildSteps)
                            {
                                buildStep.DependsOn(addImageTagsStep);
                                steps.Add(buildStep);
                            }
                        }
                    }
                }

                // If no custom build steps, create a default build step
                if (!hasCustomBuildSteps)
                {
                    var buildStep = new PipelineStep
                    {
                        Name = $"build-{computeResource.Name}",
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
                        },
                        Tags = [WellKnownPipelineTags.BuildCompute, "build"]
                    };
                    buildStep.DependsOn(addImageTagsStep);
                    steps.Add(buildStep);
                }

                // Push step for this specific resource
                var pushStep = new PipelineStep
                {
                    Name = $"push-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        
                        // Push this specific resource (login happens once in the registryLoginStep)
                        await AzureEnvironmentResourceHelpers.PushImagesToRegistryAsync(this, [computeResource], ctx, containerImageBuilder).ConfigureAwait(false);
                    },
                    Tags = ["push"]
                };
                // Push depends on all build steps for this resource
                var resourceBuildSteps = steps.Where(s => s.Tags.Contains(WellKnownPipelineTags.BuildCompute) && 
                    (s.Name == $"build-{computeResource.Name}" || s.Name.Contains(computeResource.Name))).ToList();
                foreach (var buildStep in resourceBuildSteps)
                {
                    pushStep.DependsOn(buildStep);
                }
                // Push also depends on registry login
                pushStep.DependsOn(registryLoginStep);
                steps.Add(pushStep);

                // For deploy, get the deployment target and expand its provision steps
                if (computeResource.GetDeploymentTargetAnnotation(this) is { DeploymentTarget: var deploymentTarget })
                {
                    // Check if deployment target has provision steps
                    if (deploymentTarget.TryGetAnnotationsOfType<PipelineStepAnnotation>(out var deploymentAnnotations))
                    {
                        foreach (var annotation in deploymentAnnotations)
                        {
                            var deploymentSteps = await annotation.CreateStepsAsync(factoryContext).ConfigureAwait(false);
                            var provisionSteps = deploymentSteps.Where(s => s.Tags.Contains(WellKnownPipelineTags.ProvisionInfrastructure)).ToList();
                            
                            foreach (var provisionStep in provisionSteps)
                            {
                                provisionStep.DependsOn(pushStep);
                                steps.Add(provisionStep);
                            }
                        }
                    }
                }

                // Deploy step for this specific resource
                var deployStep = new PipelineStep
                {
                    Name = $"deploy-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var azureEnvironment = ctx.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
                        if (azureEnvironment == null)
                        {
                            throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
                        }
                        
                        var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);
                        await DeployComputeResourceAsync(ctx, computeResource, provisioningContext).ConfigureAwait(false);
                    },
                    Tags = [WellKnownPipelineTags.DeployCompute, "deploy"]
                };
                deployStep.DependsOn(pushStep);
                // Also depend on any provision steps we added for this resource's deployment target
                var resourceProvisionSteps = steps.Where(s => s.Tags.Contains(WellKnownPipelineTags.ProvisionInfrastructure) && 
                    s.Name.Contains(computeResource.Name)).ToList();
                foreach (var provisionStep in resourceProvisionSteps)
                {
                    deployStep.DependsOn(provisionStep);
                }
                // Make this deploy step required by the deploy-compute-marker in AzureEnvironmentResource
                deployStep.RequiredBy(AzureEnvironmentResource.DeployComputeMarkerStepName);
                steps.Add(deployStep);
            }

            // Add print-dashboard-url step that depends on all deploy steps
            var printDashboardUrlStep = new PipelineStep
            {
                Name = $"print-dashboard-url-{name}",
                Action = ctx => PrintDashboardUrlAsync(ctx),
            };
            // Make it depend on all deploy steps for this environment
            var deploySteps = steps.Where(s => s.Tags.Contains("deploy")).ToList();
            foreach (var deployStep in deploySteps)
            {
                printDashboardUrlStep.DependsOn(deployStep);
            }
            printDashboardUrlStep.RequiredBy("deploy");
            steps.Add(printDashboardUrlStep);

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            // Make all push steps for this environment depend on the registry being provisioned
            var pushSteps = context.GetSteps("push");
            var provisionSteps = context.GetSteps(this, WellKnownPipelineTags.ProvisionInfrastructure);
            foreach (var pushStep in pushSteps)
            {
                foreach (var provisionStep in provisionSteps)
                {
                    pushStep.DependsOn(provisionStep);
                }
            }

            // Make registry login depend on the registry being provisioned
            var loginSteps = context.GetSteps("login-registry");
            foreach (var loginStep in loginSteps)
            {
                foreach (var provisionStep in provisionSteps)
                {
                    loginStep.DependsOn(provisionStep);
                }
            }

            return Task.CompletedTask;
        }));
    }

    private Task DefaultImageTags(PipelineStepContext context)
    {
        var computeResources = context.Model.GetComputeResources()
            .Where(r => r.RequiresImageBuildAndPush())
            .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
            .ToList();

        var deploymentTag = $"aspire-deploy-{DateTime.UtcNow:yyyyMMddHHmmss}";
        foreach (var resource in computeResources)
        {
            if (resource.TryGetLastAnnotation<DeploymentImageTagCallbackAnnotation>(out _))
            {
                continue;
            }
            resource.Annotations.Add(
                new DeploymentImageTagCallbackAnnotation(_ => deploymentTag));
        }

        return Task.CompletedTask;
    }

    private async Task DeployComputeResourceAsync(PipelineStepContext context, IResource computeResource, ProvisioningContext provisioningContext)
    {
        var bicepProvisioner = context.Services.GetRequiredService<IBicepProvisioner>();

        var resourceTask = await context.ReportingStep
            .CreateTaskAsync($"Deploying **{computeResource.Name}**", context.CancellationToken)
            .ConfigureAwait(false);

        await using (resourceTask.ConfigureAwait(false))
        {
            try
            {
                if (computeResource.GetDeploymentTargetAnnotation(this) is { } deploymentTarget)
                {
                    if (deploymentTarget.DeploymentTarget is AzureBicepResource bicepResource)
                    {
                        await bicepProvisioner.GetOrCreateResourceAsync(
                            bicepResource, provisioningContext, context.CancellationToken)
                            .ConfigureAwait(false);

                        var completionMessage = $"Successfully deployed **{computeResource.Name}**";
                        completionMessage += TryGetComputeResourceEndpoint(computeResource);

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

    private string TryGetComputeResourceEndpoint(IResource computeResource)
    {
        // Only produce endpoints for resources that have external endpoints
        if (!computeResource.TryGetEndpoints(out var endpoints) || !endpoints.Any(e => e.IsExternal))
        {
            return string.Empty;
        }

        // Use the WebSiteSuffix BicepOutputReference to construct the URL
        if (WebSiteSuffix.Value is { } suffixValue)
        {
            var endpoint = $"https://{computeResource.Name.ToLowerInvariant()}-{suffixValue}.azurewebsites.net";
            return $" to [{endpoint}]({endpoint})";
        }

        return string.Empty;
    }

    private async Task PrintDashboardUrlAsync(PipelineStepContext context)
    {
        // Use the DashboardUriReference BicepOutputReference
        if (DashboardUriReference.Value is { } dashboardUri)
        {
            await context.ReportingStep.CompleteAsync(
                $"Dashboard available at [dashboard URL]({dashboardUri})",
                CompletionState.Completed,
                context.CancellationToken).ConfigureAwait(false);
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
