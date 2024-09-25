// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Provides extension methods for publishing container resources as container apps in Azure.
/// </summary>
public static class AzureContainerAppContainerExtensions
{
    /// <summary>
    /// Publishes the specified container resource as a container app.
    /// </summary>
    /// <typeparam name="T">The type of the container resource.</typeparam>
    /// <param name="container">The container resource builder.</param>
    /// <param name="configure">The configuration action for the container app.</param>
    /// <returns>The updated container resource builder.</returns>
    /// <remarks>
    /// This method checks if the application is in publish mode. If it is, it adds the necessary infrastructure
    /// for container apps and applies the provided configuration action to the container app.
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = new ResourceBuilder&lt;MyContainerResource&gt;();
    /// builder.PublishAsContainerApp((module, app) =>
    /// {
    ///     // Configure the container app here
    /// });
    /// </code>
    /// </example>
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public static IResourceBuilder<T> PublishAsContainerApp<T>(this IResourceBuilder<T> container, Action<ResourceModuleConstruct, ContainerApp> configure) where T : ContainerResource
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        if (!container.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return container;
        }

        container.ApplicationBuilder.AddContainerAppsInfrastructure();

        container.WithAnnotation(new ContainerAppCustomizationAnnotation(configure));

        return container;
    }
}
