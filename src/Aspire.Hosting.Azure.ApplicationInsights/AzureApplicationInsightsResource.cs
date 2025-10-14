// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Application Insights resource.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure Application Insights resource.</param>
public class AzureApplicationInsightsResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the "appInsightsConnectionString" output reference for the Azure Application Insights resource.
    /// </summary>
    public BicepOutputReference ConnectionString => new("appInsightsConnectionString", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Application Insights resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");

    // UseAzureMonitor is looks for this specific environment variable name.
    string IResourceWithConnectionString.ConnectionStringEnvironmentVariable => "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="infra"></param>
    /// <returns></returns>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();

        // Check if an ApplicationInsightsComponent with the same identifier already exists
        var existingAppInsights = resources.OfType<ApplicationInsightsComponent>().SingleOrDefault(plan => plan.BicepIdentifier == bicepIdentifier);

        if (existingAppInsights is not null)
        {
            return existingAppInsights;
        }

        // Create and add new resource if it doesn't exist
        var appInsights = ApplicationInsightsComponent.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceAnnotation(
            this,
            infra,
            appInsights))
        {
            appInsights.Name = NameOutputReference.AsProvisioningParameter(infra);
        }

        infra.Add(appInsights);
        return appInsights;
    }
}
