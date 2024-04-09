// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.OperationalInsights;
using Azure.ResourceManager.OperationalInsights.Models;

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
    public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, string name)
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
    public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureLogAnalyticsWorkspaceResource>, ResourceModuleConstruct, OperationalInsightsWorkspace>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var workspace = new OperationalInsightsWorkspace(construct, name: name, sku: new OperationalInsightsWorkspaceSku(OperationalInsightsWorkspaceSkuName.PerGB2018));
            workspace.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            workspace.AddOutput("logAnalyticsWorkspaceId", p => p.Id);

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
