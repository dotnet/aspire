#pragma warning disable AZPROVISION001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting.Azure;

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
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.Api&gt;.PublishAsAzureContainerApp((module, app) =>
    /// {
    ///     // Configure the container app here
    /// });
    /// </code>
    /// </example>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<T> PublishAsAzureContainerApp<T>(this IResourceBuilder<T> project, Action<ResourceModuleConstruct, ContainerApp> configure)
        where T : ProjectResource
    {
        if (!project.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return project;
        }

        project.ApplicationBuilder.AddContainerAppsInfrastructure();

        project.WithAnnotation(new ContainerAppCustomizationAnnotation(configure));

        return project;
    }
}
