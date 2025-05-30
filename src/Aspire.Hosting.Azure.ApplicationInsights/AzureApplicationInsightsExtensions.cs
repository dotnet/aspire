// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.OperationalInsights;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure ApplicationInsights resources to the application model.
/// </summary>
public static class AzureApplicationInsightsExtensions
{
    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureApplicationInsightsResource}"/>.</returns>
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceName] string name)
        => AddAzureApplicationInsights(builder, name, logAnalyticsWorkspace: null);

    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="logAnalyticsWorkspace">A resource builder for the log analytics workspace.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureApplicationInsightsResource}"/>.</returns>
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var appInsights = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = ApplicationInsightsComponent.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) =>
                {
                    var appTypeParameter = new ProvisioningParameter("applicationType", typeof(string))
                    {
                        Value = "web"
                    };
                    infrastructure.Add(appTypeParameter);

                    var kindParameter = new ProvisioningParameter("kind", typeof(string))
                    {
                        Value = "web"
                    };
                    infrastructure.Add(kindParameter);

                    var appInsights = new ApplicationInsightsComponent(infrastructure.AspireResource.GetBicepIdentifier())
                    {
                        ApplicationType = appTypeParameter,
                        Kind = kindParameter,
                        Tags = { { "aspire-resource-name", name } }
                    };

                    if (logAnalyticsWorkspace != null)
                    {
                        // If someone provides a workspace via the extension method we should use it.
                        appInsights.WorkspaceResourceId = logAnalyticsWorkspace.Resource.WorkspaceId.AsProvisioningParameter(infrastructure);
                    }
                    else
                    {
                        // ... otherwise create one ourselves.
                        var autoInjectedLogAnalyticsWorkspaceName = $"law_{appInsights.BicepIdentifier}";
                        var autoInjectedLogAnalyticsWorkspace = new OperationalInsightsWorkspace(autoInjectedLogAnalyticsWorkspaceName)
                        {
                            Sku = new OperationalInsightsWorkspaceSku()
                            {
                                Name = OperationalInsightsWorkspaceSkuName.PerGB2018
                            },
                            Tags = { { "aspire-resource-name", autoInjectedLogAnalyticsWorkspaceName } }
                        };
                        infrastructure.Add(autoInjectedLogAnalyticsWorkspace);

                        // If the user does not supply a log analytics workspace of their own we still create a parameter on the Aspire
                        // side and the CDK side so that AZD can fill the value in with the one it generates.
                        appInsights.WorkspaceResourceId = autoInjectedLogAnalyticsWorkspace.Id;
                    }

                    return appInsights;
                });

            infrastructure.Add(new ProvisioningOutput("appInsightsConnectionString", typeof(string)) { Value = appInsights.ConnectionString });

            // Add name output for the resource to externalize role assignments
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = appInsights.Name });
        };

        var resource = new AzureApplicationInsightsResource(name, configureInfrastructure);

        return builder.AddResource(resource);
    }
}
