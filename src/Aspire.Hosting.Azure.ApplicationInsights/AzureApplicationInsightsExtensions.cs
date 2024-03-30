// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.ApplicationInsights;

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
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureApplicationInsights(name, null, null);
#pragma warning restore ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="logAnalyticsWorkspace">A resource builder for the log analytics workspace.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureApplicationInsightsResource}"/>.</returns>
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace)
    {
#pragma warning disable ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureApplicationInsights(name, logAnalyticsWorkspace, null);
#pragma warning restore ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Optional callback to configure the Application Insights resource.</param>
    /// <returns></returns>
    [Experimental("ASPIRE0001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource)
    {
        return builder.AddAzureApplicationInsights(name, null, configureResource);
    }

    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="logAnalyticsWorkspace">A resource builder for the log analytics workspace.</param>
    /// <param name="configureResource">Optional callback to configure the Application Insights resource.</param>
    /// <returns></returns>
    [Experimental("ASPIRE0001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var appInsights = new ApplicationInsightsComponent(construct, name: name);
            appInsights.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;
            appInsights.AssignProperty(p => p.ApplicationType, new Parameter("applicationType", defaultValue: "web"));
            appInsights.AssignProperty(p => p.Kind, new Parameter("kind", defaultValue: "web"));

            if (logAnalyticsWorkspace != null)
            {
                appInsights.AssignProperty(p => p.WorkspaceResourceId, logAnalyticsWorkspace.Resource.WorkspaceId, AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId);
            }
            else
            {
                // If the user does not supply a log analytics workspace of their own we still create a parameter on the Aspire
                // side and the CDK side so that AZD can fill the value in with the one it generates.
                construct.Resource.Parameters.Add(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, "");
                appInsights.AssignProperty(p => p.WorkspaceResourceId, new Parameter(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId));

            }

            appInsights.AddOutput("appInsightsConnectionString", p => p.ConnectionString);

            if (configureResource != null)
            {
                var resource = (AzureApplicationInsightsResource)construct.Resource;
                var resourceBuilder = builder.CreateResourceBuilder(resource);
                configureResource(resourceBuilder, construct, appInsights);
            }
        };

        var resource = new AzureApplicationInsightsResource(name, configureConstruct);

        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
