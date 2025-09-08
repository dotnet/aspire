// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for publishing project resources as container apps in Azure.
/// </summary>
public static class AzureContainerAppProjectExtensions
{
    /// <summary>
    /// Allows configuring the specified project resource as a container app.
    /// </summary>
    /// <typeparam name="T">The type of the project resource.</typeparam>
    /// <param name="project">The project resource builder.</param>
    /// <param name="configure">The configuration action for the container app.</param>
    /// <returns>The updated project resource builder.</returns>
    /// <remarks>
    /// This method adds the necessary infrastructure for container apps to the application builder
    /// and applies the specified configuration to the container app.
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.Api&gt;.PublishAsAzureContainerApp((infrastructure, app) =>
    /// {
    ///     // Configure the container app here
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> PublishAsAzureContainerApp<T>(this IResourceBuilder<T> project, Action<AzureResourceInfrastructure, ContainerApp> configure)
        where T : ProjectResource
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(configure);

        if (!project.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return project;
        }

        project.ApplicationBuilder.AddAzureContainerAppsInfrastructureCore();

        project.WithAnnotation(new AzureContainerAppCustomizationAnnotation(configure));

        return project;
    }

    /// <summary>
    /// Allows configuring the specified project resource as an Azure Container App Job.
    /// </summary>
    /// <typeparam name="T">The type of the project resource.</typeparam>
    /// <param name="project">The project resource builder.</param>
    /// <param name="configure">The configuration action for the container app job.</param>
    /// <returns>The updated project resource builder.</returns>
    /// <remarks>
    /// This method adds the necessary infrastructure for container app jobs to the application builder
    /// and applies the specified configuration to the container app job.
    /// 
    /// Note that the default trigger type for the job is set to Manual, and the default replica timeout is set to 1800 seconds (30 minutes).
    /// 
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.Api&gt;.PublishAsAzureContainerAppJob((infrastructure, job) =>
    /// {
    ///     // Configure the container app job here
    ///     job.Configuration.TriggerType = ContainerAppJobTriggerType.Schedule;
    ///     job.Configuration.ScheduleTriggerConfig.CronExpression = "0 0 * * *"; // every day at midnight
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    [Experimental("ASPIREAZURE002", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static IResourceBuilder<T> PublishAsAzureContainerAppJob<T>(this IResourceBuilder<T> project, Action<AzureResourceInfrastructure, ContainerAppJob> configure)
        where T : ProjectResource
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(configure);

        if (!project.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return project;
        }

        project.ApplicationBuilder.AddAzureContainerAppsInfrastructureCore();

        project.WithAnnotation(new AzureContainerJobCustomizationAnnotation(configure));

        return project;
    }
}
