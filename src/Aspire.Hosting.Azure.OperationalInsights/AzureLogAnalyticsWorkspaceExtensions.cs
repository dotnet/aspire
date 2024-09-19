// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureLogAnalyticsWorkspace(name, (_, _, _) => { });
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure Log Analytics Workspace resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Optional callback to configure the Azure Log Analytics Workspace resource.</param>
    /// <returns></returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<IResourceBuilder<AzureLogAnalyticsWorkspaceResource>, ResourceModuleConstruct, OperationalInsightsWorkspace>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var workspace = new OperationalInsightsWorkspace(name)
            {
                Sku = new OperationalInsightsWorkspaceSku()
                {
                    Name = OperationalInsightsWorkspaceSkuName.PerGB2018
                },
                Tags = { { "aspire-resource-name", name } }
            };
            construct.Add(workspace);

            construct.Add(new BicepOutput("logAnalyticsWorkspaceId", typeof(string))
            {
                Value = workspace.Id
            });

            if (configureResource != null)
            {
                var resource = (AzureLogAnalyticsWorkspaceResource)construct.Resource;
                var resourceBuilder = builder.CreateResourceBuilder(resource);
                configureResource(resourceBuilder, construct, workspace);
            }
        };
        var resource = new AzureLogAnalyticsWorkspaceResource(name, configureConstruct);

        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
