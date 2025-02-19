// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;

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
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.Api&gt;.PublishAsAzureContainerApp((infrastructure, app) =>
    /// {
    ///     // Configure the container app here
    /// });
    /// </code>
    /// </example>
    public static IResourceBuilder<T> PublishAsAzureContainerApp<T>(this IResourceBuilder<T> project, Action<AzureResourceInfrastructure, ContainerApp> configure)
        where T : ProjectResource
    {
        if (!project.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return project;
        }

        project.ApplicationBuilder.AddAzureContainerAppsInfrastructure();

        project.WithAnnotation(new AzureContainerAppCustomizationAnnotation(configure));

        return project;
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="project"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IResourceBuilder<T> PublishAsAzureContainerAppWithKind<T>(this IResourceBuilder<T> project, Action<AzureResourceInfrastructure, ContainerAppWithKind> configure)
        where T : ProjectResource
    {
        if (!project.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return project;
        }

        project.ApplicationBuilder.AddAzureContainerAppsInfrastructure();

        project.WithAnnotation(new AzureContainerAppWithKindCustomizationAnnotation(configure));

        return project;
    }
}

/// <summary>
///  Container app with kind.
/// </summary>
public class ContainerAppWithKind : ContainerApp
{
    /// <summary>
    /// The kind of the container app.
    /// </summary>
    public BicepValue<string> Kind
    {
        get { Initialize(); return _kind!; }
        set { Initialize(); _kind!.Assign(value); }
    }
    private BicepValue<string>? _kind;

    /// <summary>
    /// Creates a new instance of <see cref="ContainerAppWithKind"/>.
    /// </summary>
    /// <param name="bicepIdentifier"></param>
    /// <param name="resourceVersion"></param>
    public ContainerAppWithKind(string bicepIdentifier, string? resourceVersion = null) : base(bicepIdentifier, resourceVersion) { }

    /// <summary>
    /// Overrides provisionable properties.
    /// </summary>
    protected override void DefineProvisionableProperties()
    {
        base.DefineProvisionableProperties();

        _kind = DefineProperty<string>(nameof(Kind), ["kind"]);
    }
}

/// <summary>
/// Represents an annotation for customizing an Azure Container App.
/// </summary>
/// <param name="configure"></param>
public class AzureContainerAppWithKindCustomizationAnnotation(Action<AzureResourceInfrastructure, ContainerAppWithKind> configure) : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration action for customizing the Azure Container App.
    /// </summary>
    public Action<AzureResourceInfrastructure, ContainerAppWithKind> Configure { get; } = configure;
}
