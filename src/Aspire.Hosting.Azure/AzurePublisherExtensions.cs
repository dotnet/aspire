// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Extensions for adding the Azure publisher to the application model.
/// </summary>
public static class AzurePublisherExtensions
{
    /// <summary>
    /// Adds an Azure Container Apps publisher to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="Aspire.Hosting.IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the publisher used when using the Aspire CLI.</param>
    /// <param name="configureOptions">Callback to configure Azure Container Apps publisher options.</param>
    [Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static void AddAzurePublisher(this IDistributedApplicationBuilder builder, string name, Action<AzurePublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<AzurePublisher, AzurePublisherOptions>(name, configureOptions);
    }

    /// <summary>
    /// Adds an Azure Container Apps publisher to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="Aspire.Hosting.IDistributedApplicationBuilder"/>.</param>
    /// <param name="configureOptions">Callback to configure Azure Container Apps publisher options.</param>
    [Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static void AddAzurePublisher(this IDistributedApplicationBuilder builder, Action<AzurePublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<AzurePublisher, AzurePublisherOptions>("azure", configureOptions);
    }
}