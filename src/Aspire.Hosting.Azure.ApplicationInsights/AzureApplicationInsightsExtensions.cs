// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureApplicationInsights(name, null, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureApplicationInsights(name, logAnalyticsWorkspace, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Optional callback to configure the Application Insights resource.</param>
    /// <returns></returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource)
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
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureApplicationInsightsResource> AddAzureApplicationInsights(this IDistributedApplicationBuilder builder, [ResourceName] string name, IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalyticsWorkspace, Action<IResourceBuilder<AzureApplicationInsightsResource>, ResourceModuleConstruct, ApplicationInsightsComponent>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var appTypeParameter = new BicepParameter("applicationType", typeof(string))
            {
                Value = new StringLiteral("web")
            };
            construct.Add(appTypeParameter);

            var kindParameter = new BicepParameter("kind", typeof(string))
            {
                Value = new StringLiteral("web")
            };
            construct.Add(kindParameter);

            var appInsights = new ApplicationInsightsComponent(name)
            {
                ApplicationType = appTypeParameter,
                Kind = kindParameter,
                Tags = { { "aspire-resource-name", name } }
            };
            construct.Add(appInsights);

            if (logAnalyticsWorkspace != null)
            {
                // If someone provides a workspace via the extension method we should use it.
                appInsights.WorkspaceResourceId = logAnalyticsWorkspace.Resource.WorkspaceId.AsBicepParameter(construct, AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId);
            }
            else if (builder.ExecutionContext.IsRunMode)
            {
                // ... otherwise if we are in run mode, the provisioner expects us to create one ourselves.
                var autoInjectedLogAnalyticsWorkspaceName = $"law-{construct.Resource.Name}";
                var autoInjectedLogAnalyticsWorkspace = new OperationalInsightsWorkspace(autoInjectedLogAnalyticsWorkspaceName)
                {
                    Sku = new OperationalInsightsWorkspaceSku()
                    {
                        Name = OperationalInsightsWorkspaceSkuName.PerGB2018
                    },
                    Tags = { { "aspire-resource-name", autoInjectedLogAnalyticsWorkspaceName } }
                };
                construct.Add(autoInjectedLogAnalyticsWorkspace);

                // If the user does not supply a log analytics workspace of their own we still create a parameter on the Aspire
                // side and the CDK side so that AZD can fill the value in with the one it generates.
                appInsights.WorkspaceResourceId = autoInjectedLogAnalyticsWorkspace.Id;
            }
            else
            {
                // If the user does not supply a log analytics workspace of their own, and we are in publish mode
                // then we want AZD to provide one to us.
                construct.Resource.Parameters.TryAdd(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, null);
                var logAnalyticsWorkspaceParameter = new BicepParameter(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, typeof(string))
                {
                    Value = new StringLiteral("web")
                };
                construct.Add(kindParameter);
                appInsights.WorkspaceResourceId = logAnalyticsWorkspaceParameter;
            }

            construct.Add(new BicepOutput("appInsightsConnectionString", typeof(string)) { Value = appInsights.ConnectionString });

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
