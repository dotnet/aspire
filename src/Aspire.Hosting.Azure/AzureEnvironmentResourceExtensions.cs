// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

/// <summary>
/// Provides extension methods for adding Azure environment resources to the application model.
/// </summary>
public static class AzureEnvironmentResourceExtensions
{
    /// <summary>
    /// Adds an Azure environment resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{AzureEnvironmentResource}"/>.</returns>
    [Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEnvironmentResource> AddAzureEnvironment(this IDistributedApplicationBuilder builder)
    {
        if (builder.Resources.OfType<AzureEnvironmentResource>().SingleOrDefault() is { } existingResource)
        {
            // If the resource already exists, return the existing builder
            return builder.CreateResourceBuilder(existingResource);
        }
        var resourceName = builder.CreateDefaultAzureEnvironmentName();
        var resource = new AzureEnvironmentResource(resourceName);
        if (builder.ExecutionContext.IsRunMode)
        {
            // Return a builder that isn't added to the top-level application builder
            // so it doesn't surface as a resource.
            return builder.CreateResourceBuilder(resource);

        }
        return builder.AddResource(resource)
            .ExcludeFromManifest();
    }

    /// <summary>
    /// Applies additional configuration to the <see cref="AzureEnvironmentResource"/> represented
    /// by the given <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IResourceBuilder{TResource}"/> that wraps the <see cref="AzureEnvironmentResource"/>
    /// to configure.
    /// </param>
    /// <param name="configure">
    /// A delegate that receives the underlying <see cref="AzureEnvironmentResource"/> and performs
    /// the desired mutations, such as the location, resource group name, or tags.
    /// </param>
    /// <returns>
    /// The same <paramref name="builder"/> instance so that further configuration calls can be chained.
    /// </returns>
    [Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEnvironmentResource> WithProperties(
        this IResourceBuilder<AzureEnvironmentResource> builder,
        Action<AzureEnvironmentResource> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Resource);

        return builder;
    }

    /// <summary>
    /// Sets the location of the Azure environment resource.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{TResource}"/>.</param>
    /// <param name="location">The Azure location.</param>
    /// <returns>The <see cref="IResourceBuilder{AzureEnvironmentResource}"/>.</returns>
    /// <remarks>
    /// This method is used to set the location of the Azure environment resource.
    /// The location is used to determine where the resources will be deployed.
    /// </remarks>
    [Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEnvironmentResource> WithLocation(
        this IResourceBuilder<AzureEnvironmentResource> builder,
        object? location)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(location);

        builder.Resource.Location = location;

        return builder;
    }

    /// <summary>
    /// Sets the resource group name of the Azure environment resource.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{TResource}"/>.</param>
    /// <param name="resourceGroup">The Azure resource group name.</param>
    /// <returns>The <see cref="IResourceBuilder{AzureEnvironmentResource}"/>.</returns>
    /// <remarks>
    /// This method is used to set the resource group name of the Azure environment resource.
    /// The resource group name is used to determine where the resources will be deployed.
    /// </remarks>
    [Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEnvironmentResource> WithResourceGroup(
        this IResourceBuilder<AzureEnvironmentResource> builder,
        object? resourceGroup)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(resourceGroup);

        builder.Resource.ResourceGroupName = resourceGroup;

        return builder;
    }

    private static string CreateDefaultAzureEnvironmentName(this IDistributedApplicationBuilder builder)
    {
        var applicationHash = builder.Configuration["AppHost:Sha256"]![..5].ToLowerInvariant();
        return $"azure{applicationHash}";
    }
}
