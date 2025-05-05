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
    /// <code>
    /// builder.AddProject&lt;Projects.Api&gt;("name").PublishAsAzureAppServiceWebsite((infrastructure, app) =>
    /// {
    ///     // Configure the App Service WebSite resource here
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> PublishAsAzureAppServiceWebsite<T>(this IResourceBuilder<T> builder, Action<AzureResourceInfrastructure, WebSite> configure)
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        where T : IComputeResource
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        return builder.WithAnnotation(new AzureAppServiceWebsiteCustomizationAnnotation(configure));
    }
}
