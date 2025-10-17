// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure App Service Environment resource.
/// </summary>
/// <param name="name">The name of the Azure App Service Environment.</param>
/// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
public class AzureAppServiceEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    IAzureComputeEnvironmentResource,
    IAzureContainerRegistry
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
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
        return ReferenceExpression.Create($"{resource.Name.ToLower()}-{WebSiteSuffix}.azurewebsites.net");
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
