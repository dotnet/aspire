// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting;

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
    /// <example>
    /// <code>
    /// builder.AddContainer("name", "image").PublishAsAzureContainerApp((infrastructure, app) =>
    /// {
    ///     // Configure the container app here
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> PublishAsAzureContainerApp<T>(this IResourceBuilder<T> container, Action<AzureResourceInfrastructure, ContainerApp> configure)
        where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(configure);

        if (!container.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return container;
        }

        container.ApplicationBuilder.AddAzureContainerAppsInfrastructureCore();

        container.WithAnnotation(new AzureContainerAppCustomizationAnnotation(configure));

        return container;
    }

    /// <summary>
    /// Publishes the specified container resource as an Azure Container App Job.
    /// </summary>
    /// <typeparam name="T">The type of the container resource.</typeparam>
    /// <param name="container">The container resource builder.</param>
    /// <param name="configure">The configuration action for the container app job.</param>
    /// <returns>The updated container resource builder.</returns>
    /// <remarks>
    /// This method adds the necessary infrastructure for container app jobs to the application builder
    /// and applies the specified configuration to the container app job.
    /// 
    /// Note that the default trigger type for the job is set to Manual, and the default replica timeout is set to 1800 seconds (30 minutes).
    /// 
    /// <example>
    /// <code>
    /// builder.AddContainer("name", "image").PublishAsAzureContainerAppJob((infrastructure, job) =>
    /// {
    ///     // Configure the container app job here
    ///     job.Configuration.TriggerType = ContainerAppJobTriggerType.Schedule;
    ///     job.Configuration.ScheduleTriggerConfig.CronExpression = "0 0 * * *"; // every day at midnight
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    [Experimental("ASPIREAZURE002", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static IResourceBuilder<T> PublishAsAzureContainerAppJob<T>(this IResourceBuilder<T> container, Action<AzureResourceInfrastructure, ContainerAppJob> configure)
        where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(configure);

        if (!container.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return container;
        }

        container.ApplicationBuilder.AddAzureContainerAppsInfrastructureCore();

        container.WithAnnotation(new AzureContainerJobCustomizationAnnotation(configure));

        return container;
    }
}
