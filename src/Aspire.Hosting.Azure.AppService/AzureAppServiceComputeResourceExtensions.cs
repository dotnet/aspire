// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.AppService;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for publishing compute resources as Azure App Service websites.
/// </summary>
public static class AzureAppServiceComputeResourceExtensions
{
    /// <summary>
    /// Publishes the specified compute resource as an Azure App Service.
    /// </summary>
    /// <typeparam name="T">The type of the compute resource.</typeparam>
    /// <param name="builder">The compute resource builder.</param>
    /// <param name="configure">The configuration action for the App Service WebSite resource.</param>
    /// <returns>The updated compute resource builder.</returns>
    /// <remarks>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.Api&gt;("name").PublishAsAzureAppServiceWebsite((infrastructure, app) =>
    /// {
    ///     // Configure the App Service WebSite resource here
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> PublishAsAzureAppServiceWebsite<T>(this IResourceBuilder<T> builder, Action<AzureResourceInfrastructure, WebSite> configure)
        where T : IComputeResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.ApplicationBuilder.AddAzureAppServiceInfrastructureCore();

        return builder.WithAnnotation(new AzureAppServiceWebsiteCustomizationAnnotation(configure));
    }

    /// <summary>
    /// Configures the resource to be published as an Azure App Service website slot during deployment.
    /// </summary>
    /// <remarks>This method only applies the configuration when the application is running in publish mode.
    /// In other modes, the builder is returned unchanged.</remarks>
    /// <typeparam name="T">The type of compute resource being configured.</typeparam>
    /// <param name="builder">The resource builder used to configure the compute resource. Cannot be null.</param>
    /// <param name="configure">A delegate that configures the Azure resource infrastructure and the website slot. Cannot be null.</param>
    /// <returns>The original resource builder with the Azure App Service website slot publishing configuration applied.</returns>
    public static IResourceBuilder<T> PublishAsAzureAppServiceWebsiteSlot<T>(this IResourceBuilder<T> builder, Action<AzureResourceInfrastructure, WebSiteSlot> configure)
        where T : IComputeResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.ApplicationBuilder.AddAzureAppServiceInfrastructureCore();

        return builder.WithAnnotation(new AzureAppServiceWebsiteSlotCustomizationAnnotation(configure));
    }
}
