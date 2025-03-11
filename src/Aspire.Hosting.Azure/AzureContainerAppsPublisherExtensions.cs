// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Extensions for adding an Azure Container Apps publisher to the application model.
/// </summary>
public static class AzureContainerAppsPublisherExtensions
{
    /// <summary>
    /// Adds an Azure Container Apps publisher to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="Aspire.Hosting.IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the publisher used when using the Aspire CLI.</param>
    /// <param name="configureOptions">Callback to configure Azure Container Apps publisher options.</param>
    public static void AddAzureContainerApps(this IDistributedApplicationBuilder builder, string name, Action<AzureContainerAppsPublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<AzureContainerAppsPublisher, AzureContainerAppsPublisherOptions>(name, configureOptions);
    }
}