// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.OperationalInsights;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Log Analytics Workspace resources to the application model.
/// </summary>
public static class AzureLogAnalyticsWorkspaceExtensions
{
    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var workspace = new OperationalInsightsWorkspace(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Sku = new OperationalInsightsWorkspaceSku()
                {
                    Name = OperationalInsightsWorkspaceSkuName.PerGB2018
                },
                Tags = { { "aspire-resource-name", name } }
            };
            infrastructure.Add(workspace);

            infrastructure.Add(new ProvisioningOutput("logAnalyticsWorkspaceId", typeof(string))
            {
                Value = workspace.Id
            });
        };

        var resource = new AzureLogAnalyticsWorkspaceResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
