// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.Expressions;
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
    {
        return builder.AddAzureApplicationInsights(name, logAnalyticsWorkspace: null);
    }

    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="logAnalyticsWorkspace">A resource builder for the log analytics workspace.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureApplicationInsightsResource}"/>.</returns>
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceName] string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var appTypeParameter = new ProvisioningParameter("applicationType", typeof(string))
            {
                Value = new StringLiteral("web")
            };
            infrastructure.Add(appTypeParameter);

            var kindParameter = new ProvisioningParameter("kind", typeof(string))
            {
                Value = new StringLiteral("web")
            };
            infrastructure.Add(kindParameter);

            var appInsights = new ApplicationInsightsComponent(infrastructure.AspireResource.GetBicepIdentifier())
            {
                ApplicationType = appTypeParameter,
                Kind = kindParameter,
                Tags = { { "aspire-resource-name", name } }
            };
            infrastructure.Add(appInsights);

            if (logAnalyticsWorkspace != null)
            {
                // If someone provides a workspace via the extension method we should use it.
                appInsights.WorkspaceResourceId = logAnalyticsWorkspace.Resource.WorkspaceId.AsProvisioningParameter(infrastructure, AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId);
            }
            else if (builder.ExecutionContext.IsRunMode)
            {
                // ... otherwise if we are in run mode, the provisioner expects us to create one ourselves.
                var autoInjectedLogAnalyticsWorkspaceName = $"law_{appInsights.IdentifierName}";
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
            else
            {
                // If the user does not supply a log analytics workspace of their own, and we are in publish mode
                // then we want AZD to provide one to us.
                infrastructure.AspireResource.Parameters.TryAdd(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, null);
                var logAnalyticsWorkspaceParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, typeof(string))
                {
                    Value = new StringLiteral("web")
                };
                infrastructure.Add(kindParameter);
                appInsights.WorkspaceResourceId = logAnalyticsWorkspaceParameter;
            }

            infrastructure.Add(new ProvisioningOutput("appInsightsConnectionString", typeof(string)) { Value = appInsights.ConnectionString });
        };

        var resource = new AzureApplicationInsightsResource(name, configureInfrastructure);

        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
