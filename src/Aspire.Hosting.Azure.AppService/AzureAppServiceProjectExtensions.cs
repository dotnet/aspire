// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring Azure App Service project resources.
/// </summary>
public static class AzureAppServiceProjectExtensions
{
    /// <summary>
    /// Enables Azure Playwright Testing for the project when deployed to Azure App Service.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    /// <remarks>
    /// This method adds an annotation to the project that signals the Azure App Service deployment
    /// to inject the Playwright workspace environment variables
    /// when the project is deployed to an environment that has Azure Playwright Workspace enabled via
    /// <see cref="AzureAppServiceEnvironmentExtensions.WithAzurePlaywrightWorkspace"/>.
    /// </remarks>
    public static IResourceBuilder<T> EnablePlaywrightTesting<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.Annotations.Add(new Azure.EnablePlaywrightTestingAnnotation());

        return builder;
    }
}
