// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;

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
        // Add pipeline step annotation to create per-resource push and deploy steps
        // Build steps are now created by the resources themselves (ProjectResource and ContainerResource)
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
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

            // Add print-dashboard-url step that depends on the deploy-compute-marker
            var printDashboardUrlStep = new PipelineStep
            {
                Name = $"print-dashboard-url-{name}",
                Action = ctx => PrintDashboardUrlAsync(ctx),
            };
            printDashboardUrlStep.DependsOn(AzureEnvironmentResource.DeployComputeMarkerStepName);
            printDashboardUrlStep.RequiredBy("deploy");
            steps.Add(printDashboardUrlStep);

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        // This is where we wire up the build steps created by the resources
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            var model = context.Model;

            // Get all compute resources targeted to this environment
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Wire up build step dependencies
            // Build steps are created by ProjectResource and ContainerResource
            foreach (var computeResource in computeResources)
            {
                // Find build steps for this resource (created by the resource itself)
                var buildSteps = context.GetSteps(computeResource, WellKnownPipelineTags.BuildCompute)
                    .ToList();

                // Find the default-image-tags step for this environment
                var imageTagsSteps = context.GetSteps(computeResource, "default-image-tags")
                    .ToList();

                // Make build steps depend on default-image-tags
                foreach (var buildStep in buildSteps)
                {
                    foreach (var imageTagsStep in imageTagsSteps)
                    {
                        buildStep.DependsOn(imageTagsStep);
                    }
                }

                // Find push steps for this resource (created by deployment targets)
                var pushSteps = context.GetSteps("push")
                    .Where(s => s.Name == $"push-{computeResource.Name}")
                    .ToList();

                // Make push steps depend on build steps
                foreach (var pushStep in pushSteps)
                {
                    foreach (var buildStep in buildSteps)
                    {
                        pushStep.DependsOn(buildStep);
                    }
                }

                // Make push steps depend on the registry being provisioned
                var provisionSteps = context.GetSteps(this, WellKnownPipelineTags.ProvisionInfrastructure);
                foreach (var pushStep in pushSteps)
                {
                    foreach (var provisionStep in provisionSteps)
                    {
                        pushStep.DependsOn(provisionStep);
                    }
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
