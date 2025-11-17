// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppService;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an annotation for customizing an Azure Web App.
/// </summary>
public sealed class AzureAppServiceWebsiteCustomizationAnnotation(Action<AzureResourceInfrastructure, WebSite> configure)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration action for customizing the Azure Web App.
    /// </summary>
    public Action<AzureResourceInfrastructure, WebSite> Configure { get; } = configure ?? throw new ArgumentNullException(nameof(configure));
}

/// <summary>
/// Represents an annotation that dynamically specifies a network location by its host name.
/// </summary>
/// <remarks>This annotation is typically used to associate a resource with a network location that is identified
/// by a host name.</remarks>
public class DynamicNetworkLocationAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Host name of the dynamic network location.
    /// </summary>
    public string HostName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicNetworkLocationAnnotation"/> class with the specified host
    /// name.
    /// </summary>
    /// <remarks>The <paramref name="hostName"/> parameter is used to identify the network location
    /// dynamically. Ensure that the provided host name is valid and properly formatted.</remarks>
    /// <param name="hostName">The host name associated with the network location. Cannot be null or empty.</param>
    public DynamicNetworkLocationAnnotation(string hostName)
    {
        HostName = hostName;
    }
}

/// <summary>
/// Host name parameter annotation.
/// </summary>
/// <param name="parameter"></param>
public sealed class HostNameParameterAnnotation(ParameterResource parameter) : IResourceAnnotation
{
    /// <summary>
    /// Host name parameter resource.
    /// </summary>
    public ParameterResource Parameter { get; internal set; } = parameter;
}

/// <summary>
/// 
/// </summary>
/// <param name="parameter"></param>
public sealed class HostNamePlaceholderAnnotation(string? parameter) : IResourceAnnotation
{
    /// <summary>
    /// Host name parameter resource.
    /// </summary>
    public string? Value { get; internal set; } = parameter;
}
